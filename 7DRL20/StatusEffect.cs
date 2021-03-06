﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.Events;
using RoguelikeEngine.Traits;
using RoguelikeEngine.VisualEffects;

namespace RoguelikeEngine
{
    [SerializeInfo]
    abstract class StatusEffect : IEffectHolder
    {
        public IEffectHolder Creature;
        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public bool HasStacks => MaxStacks > 1;
        public int Stacks => (int)Math.Min(BuildupRound, MaxStacks);
        public virtual int MaxStacks => int.MaxValue;
        public double Buildup = 0;
        public double BuildupRound => Math.Round(Buildup, 2);

        public Slider Duration = new Slider(float.PositiveInfinity);
        public string DurationText => float.IsInfinity(Duration.EndTime) ? string.Empty : $"({Duration.EndTime - Duration.Time} Turns)";

        public bool Hidden;

        public abstract string Name
        {
            get;
        }
        public abstract string Description
        {
            get;
        }
        public string BuildupText(double buildup) => ((int)(Math.Round(buildup, 2) * 100)).ToString("+0;-#")+"%";
        public string StackText => $"x{Stacks}";
        public string BuildupTooltip
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                if (Stacks > 0)
                    builder.Append($" {StackText}");
                if (Math.Round(BuildupRound - Stacks,2) > 0)
                    builder.Append($" ({(int)(Math.Round(BuildupRound - Stacks,2) * 100)}%)");
                if(builder.Length > 0)
                    builder.Remove(0, 1);
                return builder.ToString();
            }
        }

        public int LastChange = 0;

        public StatusEffect()
        {
            ObjectID = EffectManager.SetID(this);
        }

        public virtual void Update()
        {
            Duration += 1;
            if (Duration.Done)
                Remove();
        }

        public virtual void SetupEffects()
        {
            //NOOP
        }

        public void Apply()
        {
            SetupEffects();
            Effect.Apply(new EffectStatusEffect(this, Creature));
        }

        public void Remove()
        {
            foreach (var effect in EffectManager.GetEffects<Effects.EffectStatusEffect>(Creature).Where(stat => stat.StatusEffect == this))
                effect.Remove();
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            var list = new List<T>();
            list.AddRange(EffectManager.GetEffects<T>(this));
            return list;
        }

        public virtual bool CanCombine(StatusEffect other)
        {
            return GetType() == other.GetType();
        }

        public virtual StatusEffect[] Combine(StatusEffect other)
        {
            AddDuration(other.Duration);
            AddBuildup(other.Buildup);
            return new[] { this };
        }

        private void AddDuration(Slider duration)
        {
            if(Duration.EndTime - Duration.Time < duration.EndTime - duration.Time)
                Duration = duration;
        }

        public void AddBuildup(double buildup)
        {
            int lastStacks = Stacks;
            Buildup += buildup;
            Buildup = Math.Max(0, Math.Min(Buildup, MaxStacks));
            if (Stacks > lastStacks)
                OnStackChange(Stacks - lastStacks);
        }

        public virtual void OnAdd()
        {
            if(!Hidden && Stacks > 0)
                PopupHelper.Global.Add(new MessageStatusBuildup(Creature, this, Stacks));
        }

        public virtual void OnRemove()
        {
            if (!Hidden && Stacks > 0)
                PopupHelper.Global.Add(new MessageStatusBuildup(Creature, this, -Stacks));
        }

        public virtual void OnStackChange(int delta)
        {
            LastChange += delta;
            if (!Hidden)
                PopupHelper.Global.Add(new MessageStatusBuildup(Creature, this, delta));
        }

        public virtual JToken WriteJson()
        {
            JObject json = new JObject();

            json["id"] = Serializer.GetID(this);
            json["creature"] = Serializer.GetHolderID(Creature);
            json["buildup"] = Buildup;
            json["duration"] = Duration.WriteJson();

            return json;
        }

        public virtual void ReadJson(JToken json, Context context)
        {
            Creature = Serializer.GetHolder(json["creature"], context);
            Buildup = json["buildup"].Value<double>();
            Duration = new Slider(json["duration"]);
        }
    }

    class BleedLesser : StatusEffect
    { 
        public override string Name => $"Lesser Bleed";
        public override string Description => $"Lose HP over time.";

        public override int MaxStacks => 3;

        public BleedLesser() : base()
        {
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new OnTurn(this, OnTick));
        }

        [Construct("bleed_lesser")]
        public static BleedLesser Construct(Context context)
        {
            return new BleedLesser();
        }

        public override void OnStackChange(int delta)
        {
            base.OnStackChange(delta);
            if(Stacks >= MaxStacks)
            {
                //Remove();
            }
        }

        private IEnumerable<Wait> OnTick(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            if (Stacks >= 1)
                yield return creature.AttackSelf(BleedAttack);
        }

        private Attack BleedAttack(Creature attacker, IEffectHolder defender)
        {
            var damage = 2.5 * Math.Pow(2, Stacks - 1);

            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(damage, 0, 0);
            attack.Elements.Add(Element.Bleed, 1);
            attack.ExtraEffects.Add(new AttackDamageStat(Stat.Blood, 1.0));
            attack.DamageEffect = null;
            attack.Unblockable = true;
            return attack;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class BleedGreater : StatusEffect
    {
        public override string Name => $"Greater Bleed";
        public override string Description => $"Sudden {Element.Bleed.FormatString} damage on each buildup.";

        public BleedGreater() : base()
        {
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new OnTurn(this, OnTick));
        }

        [Construct("bleed_greater")]
        public static BleedGreater Construct(Context context)
        {
            return new BleedGreater();
        }

        public override void OnStackChange(int delta)
        {
            if (delta > 0)
            {
                //TODO: Rewrite this so it uses an Attack
                double damage = delta * 30;
                Creature.TakeDamage(damage, Element.Bleed);
                Creature.TakeStatDamage(damage, Stat.Blood);
            }
            if (Creature.GetStatDamage(Stat.Blood) >= Creature.GetStat(Stat.HP))
            {
                Creature.AddStatusEffect(new Anemia()
                {
                    Buildup = 0.1,
                });
            }
            base.OnStackChange(delta);
        }

        private IEnumerable<Wait> OnTick(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            if (Stacks >= 1)
                yield return creature.AttackSelf(BleedAttack);
        }

        private Attack BleedAttack(Creature attacker, IEffectHolder defender)
        {
            var damage = 5;

            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(damage, 0, 0);
            attack.Elements.Add(Element.Bleed, 1);
            attack.ExtraEffects.Add(new AttackDamageStat(Stat.Blood, 1.0));
            attack.DamageEffect = null;
            attack.Unblockable = true;
            return attack;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Anemia : StatusEffect
    {
        public override string Name => $"Anemia";
        public override string Description => $"";

        public override int MaxStacks => 1;

        [Construct("anemia")]
        public static Anemia Construct(Context context)
        {
            return new Anemia();
        }

        public override void Update()
        {
            base.Update();
            double bloodloss = Creature.GetStatDamage(Stat.Blood);
            if (bloodloss < Creature.GetStat(Stat.HP))
            {
                AddBuildup(-0.1);
            }
        }
    }

    class DefenseDownPoison : StatusEffect
    {
        public override string Name => $"Defense Poison";
        public override string Description => "Escalating Defense Down until cured";

        public override int MaxStacks => 1;

        public DefenseDownPoison() : base()
        {
        }

        [Construct("poison_defense_down")]
        public static DefenseDownPoison Construct(Context context)
        {
            return new DefenseDownPoison();
        }

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
                Creature.AddStatusEffect(new DefenseDown()
                {
                    Buildup = 0.1,
                    Duration = new Slider(20),
                });
        }
    }

    class DefenseDown : StatusEffect
    {
        public override string Name => $"Defense Down";
        public override string Description => $"{Stat.Defense.FormatString} is reduced by {Stacks * 10}% + {Stacks * 3}";

        public override int MaxStacks => 15;

        public DefenseDown() : base()
        {
        }

        [Construct("defense_down")]
        public static DefenseDown Construct(Context context)
        {
            return new DefenseDown();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, -0.1));
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, -3) { Base = false });
        }

        public override void OnStackChange(int delta)
        {
            base.OnStackChange(delta);
            if(delta > 0 && Creature is Creature creature)
            {
                var cloak = new Cloak(creature, 30);
                cloak.OnUpdate += c => Cloak.PowerDown(c, 5, ColorMatrix.Greyscale() * ColorMatrix.Tint(Color.SteelBlue), LerpHelper.QuadraticOut, LerpHelper.QuadraticOut, 20);
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class DefenseUp : StatusEffect
    {
        public override string Name => $"Defense Up";
        public override string Description => $"{Stat.Defense.FormatString} is increased by {Stacks * 10}% + {Stacks * 3}";

        public override int MaxStacks => 10;

        public DefenseUp() : base()
        {      
        }

        [Construct("defense_up")]
        public static DefenseUp Construct(Context context)
        {
            return new DefenseUp();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, 0.1));
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, 3) { Base = false });
        }

        public override void OnStackChange(int delta)
        {
            base.OnStackChange(delta);
            if (delta > 0 && Creature is Creature creature)
            {
                var cloak = new Cloak(creature, 30);
                cloak.OnUpdate += c => Cloak.PowerUp(c, 5, ColorMatrix.Greyscale() * ColorMatrix.Tint(Color.SteelBlue), LerpHelper.QuadraticOut, LerpHelper.QuadraticOut, 20);
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class MagicPower : StatusEffect
    {
        public override string Name => $"Magic Power";
        public override string Description => $"Spell Damage is increased by 100%";

        public override int MaxStacks => 1;

        public MagicPower() : base()
        {
            Effect.Apply(new OnStartAttack(this, OnSpellAttack));
        }

        private IEnumerable<Wait> OnSpellAttack(Attack attack)
        {
            if(attack.IsSpell())
            {
                attack.Damage *= 2;
            }
            return Enumerable.Empty<Wait>();
        }

        [Construct("magic_power")]
        public static MagicPower Construct(Context context)
        {
            return new MagicPower();
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Poison : StatusEffect
    {
        public override string Name => $"Poison";
        public override string Description => $"Periodic {Element.Poison.FormatString} damage based on buildup.";

        public Poison() : base()
        {
        }

        [Construct("poison")]
        public static Poison Construct(Context context)
        {
            return new Poison();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new OnTurn(this, OnTick));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Poison;
        }

        private IEnumerable<Wait> OnTick(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            if (Stacks >= 1)
                yield return creature.AttackSelf(BurnAttack);
        }

        private Attack BurnAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(Math.Pow(2, Stacks - 1) * 5, 0, 1);
            attack.Elements.Add(Element.Poison, 1);
            attack.DamageEffect = null;
            attack.Unblockable = true;
            return attack;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Stun : StatusEffect
    {
        public override string Name => $"Stun";
        public override string Description => $"Reduces speed by 100%.";

        public override int MaxStacks => 1;

        public Stun() : base()
        {
        }

        [Construct("stun")]
        public static Stun Construct(Context context)
        {
            return new Stun();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatPercent(this, Stat.Speed, -1));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Stun;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Paralyze : StatusEffect
    {
        public override string Name => $"Paralyze";
        public override string Description => $"Reduces speed by 95%.";

        public override int MaxStacks => 1;

        public Paralyze() : base()
        {
        }

        [Construct("paralyze")]
        public static Paralyze Construct(Context context)
        {
            return new Paralyze();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatPercent(this, Stat.Speed, -0.95));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Paralyze;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Freeze : StatusEffect
    {
        public override string Name => $"Freeze";
        public override string Description => $"Reduces speed to 0. Frozen targets take double damage from non-{Element.Fire.FormatString} attacks.";

        public override int MaxStacks => 1;

        public Freeze() : base()
        {
        }

        [Construct("freeze")]
        public static Freeze Construct(Context context)
        {
            return new Freeze();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatMultiply(this, Stat.Speed, 0));
            Effect.Apply(new EffectStatMultiply(this, Stat.DamageRate, 2));
            Effect.Apply(new EffectStatMultiply(this, Element.Fire.DamageRate, 0.5));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Freeze;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Aflame : StatusEffect
    {
        public override string Name => $"Aflame";
        public override string Description => $"Take {Element.Fire.FormatString} damage every turn.";

        public override int MaxStacks => 1;

        public Aflame() : base()
        {
        }

        [Construct("aflame")]
        public static Aflame Construct(Context context)
        {
            return new Aflame();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new OnTurn(this, OnBurn));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Aflame;
        }

        private IEnumerable<Wait> OnBurn(TurnEvent turn)
        {
            Creature creature = turn.Creature;

            if (Stacks >= 1)
                yield return creature.AttackSelf(BurnAttack);
        }

        private Attack BurnAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(Math.Pow(2, Stacks - 1) * 5, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            attack.DamageEffect = null;
            attack.Unblockable = true;
            return attack;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Incinerate : StatusEffect
    {
        public override string Name => $"Incinerate";
        public override string Description => $"Instant Death at {MaxStacks} stacks.";

        public override int MaxStacks => 5;

        public Incinerate() : base()
        {
        }

        [Construct("incinerate")]
        public static Incinerate Construct(Context context)
        {
            return new Incinerate();
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Incinerate;
        }

        //TODO: Instant Death

        public override void Update()
        {
            base.Update();
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Wet : StatusEffect
    {
        public override string Name => $"Wet";
        public override string Description => $"Extra damage from {Element.Thunder.FormatString} attacks. Reduces {Element.Fire.FormatString} damage.";

        public override int MaxStacks => 1;

        public Wet() : base()
        {
        }

        [Construct("wet")]
        public static Wet Construct(Context context)
        {
            return new Wet();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatPercent(this, Element.Thunder.DamageRate, 0.5));
            Effect.Apply(new EffectStatPercent(this, Element.Fire.DamageRate, -0.25));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Wet;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Muddy : StatusEffect
    {
        public override string Name => $"Muddy";
        public override string Description => $"Reduces speed by 10%. Reduces {Element.Water.FormatString} damage.";

        public override int MaxStacks => 1;

        public Muddy() : base()
        {
        }

        [Construct("muddy")]
        public static Muddy Construct(Context context)
        {
            return new Muddy();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatMultiply(this, Stat.Speed, 0.9));
            Effect.Apply(new EffectStatPercent(this, Element.Water.DamageRate, -0.25));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Muddy;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Oiled : StatusEffect
    {
        public override string Name => $"Oiled";
        public override string Description => $"Triples {Element.Fire.FormatString} damage.";

        public override int MaxStacks => 1;

        public Oiled() : base()
        {
        }

        [Construct("oiled")]
        public static Oiled Construct(Context context)
        {
            return new Oiled();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatMultiply(this, Element.Fire.DamageRate, 3.0));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Oiled;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class HealSlimed : StatusEffect
    {
        public override string Name => $"Slimed";
        public override string Description => $"Slime sometimes restores health of the user.";

        public override int MaxStacks => 5;

        Creature Master;
        static Random Random = new Random();

        public HealSlimed() : base()
        {
        }

        public HealSlimed(Creature master) : base()
        {
            Master = master;
        }

        [Construct("slimed_heal")]
        public static HealSlimed Construct(Context context)
        {
            return new HealSlimed();
        }

        public override void Update()
        {
            base.Update();
            if (Random.NextDouble() < 0.2 && Stacks >= 1) {
                AddBuildup(-0.5);
                Master.Heal(40);
            }
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is HealSlimed slimed && slimed.Master == Master;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }

        public override JToken WriteJson()
        {
            JToken json = base.WriteJson();
            json["master"] = Serializer.GetHolderID(Master);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Master = Serializer.GetHolder<Creature>(json["master"], context);
        }
    }

    class Slimed : StatusEffect
    {
        public override string Name => $"Slimed";
        public override string Description => $"On buildup, enemy {Stat.HP.FormatString} turns into green slime.";

        public override int MaxStacks => 1;

        Random Random = new Random();

        public Slimed() : base()
        {
        }

        [Construct("slimed")]
        public static Slimed Construct(Context context)
        {
            return new Slimed();
        }

        public override void OnStackChange(int delta)
        {
            base.OnStackChange(delta);
            if (delta > 0)
            {
                if(Creature is Creature creature)
                {
                    SceneGame world = creature.World;
                    world.Wait.Add(Scheduler.Instance.RunAndWait(RoutineSpawn(creature)));
                }
                Remove();
            }
        }

        private IEnumerable<Wait> RoutineSpawn(Creature creature)
        {
            SceneGame world = creature.World;
            Tile tile = creature.Tile;
            double slimeDamage = 100;
            double slimeHP = Math.Min(slimeDamage, creature.CurrentHP);
            creature.TakeStatDamage(slimeDamage, Stat.HP);
            if (slimeHP > 0)
            {
                var slime = new GreenBlob(world, slimeHP);
                slime.MoveTo(tile, 0);
                //slime.MakeAggressive(world.Player);
                slime.AddControlTurn();
                var neighbors = tile.GetAdjacentNeighbors().Shuffle(Random);
                var pick = neighbors.Where(x => !x.Solid && x.Creatures.Empty()).FirstOrDefault();
                if (pick == null)
                    pick = neighbors.FirstOrDefault();
                slime.MoveTo(pick, 20);
                yield return slime.WaitSome(20);
                if (pick.Solid || pick.Creatures.Any(x => x != slime))
                {
                    new GreenBlobPop(slime.World, slime.VisualTarget, Vector2.Zero, 0, 10);
                    slime.Destroy();
                }
            }
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Slimed slimed;
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Undead : StatusEffect
    {
        public override string Name => $"Undead";
        public override string Description => $"Subject is undead. {Element.Healing.FormatString} causes damage.";

        public override int MaxStacks => 1;

        public Undead() : base()
        {
            Hidden = true;
        }

        [Construct("undead")]
        public static Undead Construct(Context context)
        {
            return new Undead();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectTrait(this, Trait.Undead));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Undead;
        }

        public override string ToString()
        {
            return $"{base.ToString()}";
        }
    }

    class PoweredUp : StatusEffect
    {
        public override string Name => $"Powered Up";
        public override string Description => $"GROOOOOAAAAR";

        public override int MaxStacks => 1;

        public PoweredUp() : base()
        {
            Hidden = true;
        }

        [Construct("powered_up")]
        public static PoweredUp Construct(Context context)
        {
            return new PoweredUp();
        }
    }

    class Chirality : StatusEffect
    {
        public override string Name => $"Chirality";
        public override string Description => $"Increases chiral attack's damage.";

        public override bool CanCombine(StatusEffect other)
        {
            return other is Chirality;
        }

        [Construct("chirality")]
        public static Chirality Construct(Context context)
        {
            return new Chirality();
        }
    }

    class DeltaMark : StatusEffect
    {
        public override string Name => $"Delta Mark";
        public override string Description => $"Increases {Element.Earth.FormatString} damage by 20% and triples Chirality buildup.";

        public override int MaxStacks => 1;

        public DeltaMark()
        {
        }

        [Construct("mark_delta")]
        public static DeltaMark Construct(Context context)
        {
            return new DeltaMark();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectStatPercent(this, Element.Earth.DamageRate, 0.2));
        }
    }

    class Geomancy : StatusEffect
    {
        public override string Name => $"Geomancy";
        public override string Description => GetDescription();

        public override int MaxStacks => 1;

        CloudGeomancy Master;

        public Geomancy()
        {
        }

        public Geomancy(CloudGeomancy master)
        {
            Master = master;
        }

        [Construct("geomancy")]
        public static Geomancy Construct(Context context)
        {
            return new Geomancy();
        }

        private string GetDescription()
        {
            string statBlock = String.Empty;
            var effects = GetEffects<Effect>();
            var effectGroups = effects.GroupBy(effect => effect, Effect.StatEquality);

            foreach (var group in effectGroups.OrderBy(group => group.Key.VisualPriority))
            {
                group.Key.AddStatBlock(ref statBlock, group);
            }
            return statBlock.TrimEnd('\n');
        }

        public override void Update()
        {
            base.Update();

            if (Master != null && Master.Destroyed) //TODO: also remove when leaving the floor
                this.Remove();
        }

        public override void OnAdd()
        {
            base.OnAdd();
            if(Creature is Creature creature)
            {
                EventBus.Register(this);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            EventBus.Unregister(this);
        }

        public void UpdateStats()
        {
            this.ClearEffects();
            if(Creature is Creature creature) {
                foreach (Tile tile in creature.Tiles){
                    int geoFunction = tile.X * 37 + tile.Y * 142;
                    switch(geoFunction % 10)
                    {
                        case (0):
                            Effect.Apply(new EffectStatPercent(this, Stat.Defense, 0.1));
                            break;
                        case (1):
                            Effect.Apply(new EffectStatPercent(this, Stat.Attack, 0.25));
                            break;
                        case (2):
                            Effect.Apply(new EffectStatPercent(this, Stat.Attack, 0.1));
                            break;
                        case (3):
                            Effect.Apply(new EffectStatPercent(this, Stat.Attack, -0.5));
                            Effect.Apply(new EffectStatPercent(this, Stat.Defense, 0.5));
                            break;
                        case (4):
                            Effect.Apply(new EffectStatPercent(this, Stat.Attack, 0.25));
                            Effect.Apply(new EffectStatPercent(this, Stat.Defense, -0.25));
                            break;
                    }
                    
                }
            }
        }

        [EventSubscribe]
        public void OnMove(EventMove.Finish evt)
        {
            if(evt.Creature == Creature)
                UpdateStats();
        }

        public override JToken WriteJson()
        {
            JToken json = base.WriteJson();
            json["master"] = Serializer.GetHolderID(Master);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Master = Serializer.GetHolder<CloudGeomancy>(json["master"], context);
        }
    }

    class Wedlock : StatusEffect
    {
        public override string Name => $"Wedlock";
        public override string Description => $"Blocks Unequipping and Offhand Swapping.";

        public override int MaxStacks => 1;

        Creature Master;

        public Wedlock()
        {
        }

        public Wedlock(Creature master) : this()
        {
            Master = master;
        }

        [Construct("wedlock")]
        public static Wedlock Construct(Context context)
        {
            return new Wedlock();
        }

        public override void SetupEffects()
        {
            base.SetupEffects();

            Effect.Apply(new EffectFlag(this, Stat.SwapItem, false, 10));
            Effect.Apply(new EffectFlag(this, Stat.UnequipItem, false, 10));
        }

        public override void Update()
        {
            base.Update();

            if (Master.Dead)
                this.Remove();
        }

        public override JToken WriteJson()
        {
            JToken json = base.WriteJson();
            json["master"] = Serializer.GetHolderID(Master);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            Master = Serializer.GetHolder<Creature>(json["master"], context);
        }
    }

    class HagsFlesh : StatusEffect
    {
        public override string Name => $"Stolen Flesh";
        public override string Description => $"Required for other Skills.";

        public override int MaxStacks => 5;

        public HagsFlesh()
        {
        }

        [Construct("hags_flesh")]
        public static HagsFlesh Construct(Context context)
        {
            return new HagsFlesh();
        }
    }

    class Satiated : StatusEffect
    {
        public override string Name => $"Satiated";
        public override string Description => $"Increases HP regeneration.";

        public override int MaxStacks => 1;

        public Satiated()
        {
        }

        //TODO: Increase HP regeneration

        [Construct("satiated")]
        public static Satiated Construct(Context context)
        {
            return new Satiated();
        }
    }

    abstract class Boiling : StatusEffect
    {
        public abstract void Broil();
    }

    class BoilingFlesh : Boiling
    {
        public override string Name => $"Boiling Flesh";
        public override string Description => $"Boiling: Gains 1 stack every turn.";

        public BoilingFlesh()
        {
        }

        [Construct("boiling_flesh")]
        public static BoilingFlesh Construct(Context context)
        {
            return new BoilingFlesh();
        }

        public override void Update()
        {
            base.Update();

            Broil();
        }

        public override void Broil()
        {
            Buildup += 1;
        }
    }

    class Forcefield : StatusEffect
    {
        public override string Name => $"Forcefield";
        public override string Description => GetDescription();

        public override int MaxStacks => 1;

        public Element WeakElement;

        public Forcefield()
        {
        }

        public Forcefield SetElement(Element element)
        {
            WeakElement = element;
            this.ClearEffects();
            Effect.Apply(new EffectStatPercent(this, WeakElement.DamageRate, 1.0));
            foreach (var otherElement in Element.MagicalElements.Where(e => SkillUtil.IsElement(WeakElement, element)))
            {
                Effect.Apply(new EffectStatPercent(this, otherElement.DamageRate, -1.0));
            }
            return this;
        }

        [Construct("forcefield")]
        public static Forcefield Construct(Context context)
        {
            return new Forcefield();
        }

        private string GetDescription()
        {
            string statBlock = String.Empty;
            statBlock += $"Weak to {WeakElement.FormatString}\n";
            statBlock += $"Resistant to all other elements\n";

            return statBlock.Trim('\n');
        }

        public override JToken WriteJson()
        {
            JToken json = base.WriteJson();
            json["weakElement"] = WeakElement.ID;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            base.ReadJson(json, context);
            SetElement(Element.GetElement(json["weakElement"].Value<string>()));
        }
    }
}
