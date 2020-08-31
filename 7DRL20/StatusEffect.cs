using System;
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

        public void Apply()
        {
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
                PopupManager.Add(new MessageStatusBuildup(Creature, this, Stacks));
        }

        public virtual void OnRemove()
        {
            if (!Hidden && Stacks > 0)
                PopupManager.Add(new MessageStatusBuildup(Creature, this, -Stacks));
        }

        public virtual void OnStackChange(int delta)
        {
            LastChange += delta;
            if (!Hidden)
                PopupManager.Add(new MessageStatusBuildup(Creature, this, delta));
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

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
            {
                double damage = 2.5 * Math.Pow(2, Stacks - 1);
                Creature.TakeDamage(damage, Element.Bleed);
                Creature.TakeStatDamage(damage, Stat.Blood);
            }
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

        [Construct("bleed_greater")]
        public static BleedGreater Construct(Context context)
        {
            return new BleedGreater();
        }

        public override void OnStackChange(int delta)
        {
            if (delta > 0)
            {
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

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
            {
                double damage = 5;
                Creature.TakeDamage(damage, Element.Bleed);
                Creature.TakeStatDamage(damage, Stat.Blood);
            }
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
            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, -0.1));
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, -3) { Base = false });
        }

        [Construct("defense_down")]
        public static DefenseDown Construct(Context context)
        {
            return new DefenseDown();
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
            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, 0.1));
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, 3) { Base = false });
        }

        [Construct("defense_up")]
        public static DefenseUp Construct(Context context)
        {
            return new DefenseUp();
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is DefenseUp;
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

        public override bool CanCombine(StatusEffect other)
        {
            return other is Poison;
        }

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
                Creature.TakeDamage(Math.Pow(2, Stacks - 1) * 5, Element.Poison);
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

        public override bool CanCombine(StatusEffect other)
        {
            return other is Aflame;
        }

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
                Creature.TakeDamage(Math.Pow(2, Stacks - 1) * 5, Element.Fire);
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
            Effect.Apply(new EffectStatPercent(this, Element.Thunder.DamageRate, 0.5));
            Effect.Apply(new EffectStatPercent(this, Element.Fire.DamageRate, -0.25));
        }

        [Construct("wet")]
        public static Wet Construct(Context context)
        {
            return new Wet();
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Wet;
        }

        public override void Update()
        {
            base.Update();
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
            Effect.Apply(new EffectTrait(this, Trait.Undead));
        }

        [Construct("undead")]
        public static Undead Construct(Context context)
        {
            return new Undead();
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
            Effect.Apply(new EffectStatPercent(this, Element.Earth.DamageRate, 0.2));
        }

        [Construct("mark_delta")]
        public static DeltaMark Construct(Context context)
        {
            return new DeltaMark();
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
            var statGroups = effects.OfType<IStat>().GroupBy(stat => stat.Stat, stat => (Effect)stat).OrderBy(group => group.Key.Priority);
            foreach (var stat in statGroups)
            {
                statBlock += stat.GetStatBonus(stat.Key);
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
    }

    class Wedlock : StatusEffect
    {
        public override string Name => $"Wedlock";
        public override string Description => $"Blocks Unequipping and Offhand Swapping.";

        public override int MaxStacks => 1;

        Creature Master;

        public Wedlock()
        {
            Effect.Apply(new EffectFlag(this, Stat.SwapItem, false, 10));
            Effect.Apply(new EffectFlag(this, Stat.UnequipItem, false, 10));
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

        public override void Update()
        {
            base.Update();

            if (Master.Dead)
                this.Remove();
        }
    }
}
