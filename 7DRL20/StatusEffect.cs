using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;

namespace RoguelikeEngine
{
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
            ObjectID = EffectManager.NewID(this);
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
    }

    class BleedLesser : StatusEffect
    { 
        public override string Name => $"Lesser Bleed";
        public override string Description => $"Lose HP over time.";

        public override int MaxStacks => 3;

        public BleedLesser() : base()
        {
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
        public override string Description => $"Sudden HP damage on each buildup.";

        public BleedGreater() : base()
        {
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
        public override string Description => $"Defense is reduced by {Stacks * 10}% + {Stacks * 3}";

        public override int MaxStacks => 15;

        public DefenseDown() : base()
        {
            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, -0.1));
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, -3) { Base = false });
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class DefenseUp : StatusEffect
    {
        public override string Name => $"Defense Up";
        public override string Description => $"Defense is increased by {Stacks * 10}% + {Stacks * 3}";

        public override int MaxStacks => 10;

        public DefenseUp() : base()
        {
            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, 0.1));
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, 3) { Base = false });
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
        public override string Description => $"Periodic damage based on buildup.";

        public Poison() : base()
        {
            
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is Poison;
        }

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
                Creature.TakeDamage(Math.Pow(2, Stacks - 1), Element.Poison);
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

        public HealSlimed(Creature master) : base()
        {
            Master = master;
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
        public override string Description => $"Slime sometimes restores health of the user.";

        public override int MaxStacks => 1;

        Creature Master;

        public Slimed(Creature master) : base()
        {
            Master = master;
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
                slime.MakeAggressive(world.Player);
                world.ActionQueue.Add(slime);
                var neighbors = tile.GetAdjacentNeighbors().Shuffle();
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
        public override string Description => $"Subject is undead. Healing causes damage.";

        public override int MaxStacks => 1;

        public Undead() : base()
        {
            Hidden = true;
            Effect.Apply(new EffectStatMultiply(this, Element.Healing.DamageRate, -1));
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
    }

    class Chirality : StatusEffect
    {
        public override string Name => $"Chirality";
        public override string Description => $"Increases chiral attack's damage.";

        public override bool CanCombine(StatusEffect other)
        {
            return other is Chirality;
        }
    }

    class DeltaMark : StatusEffect
    {
        public override string Name => $"Delta Mark";
        public override string Description => $"Increases earth damage by 20% and triples Chirality buildup.";

        public override int MaxStacks => 1;

        public DeltaMark()
        {
            Effect.Apply(new EffectStatPercent(this, Element.Earth.DamageRate, 0.2));
        }
    }

    class Geomancy : StatusEffect, ITurnTaker
    {
        public override string Name => $"Geomancy";
        public override string Description => GetDescription();

        public double TurnSpeed => 1;
        public double TurnBuildup { get; set; }
        public bool TurnReady => TurnBuildup >= 1;
        public bool RemoveFromQueue { get; set; }

        public override int MaxStacks => 1;

        Creature Master;

        public Geomancy(Creature master)
        {
            Master = master;
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

            if (Master.Dead)
                this.Remove();
        }

        public override void OnAdd()
        {
            base.OnAdd();
            if(Creature is Creature creature)
            {
                creature.ActionQueue.Add(this);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            RemoveFromQueue = true;
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

        public Wait TakeTurn(ActionQueue queue)
        {
            UpdateStats();
            this.ResetTurn();
            return Wait.NoWait;
        }
    }

    class Wedlock : StatusEffect
    {
        public override string Name => $"Wedlock";
        public override string Description => $"Blocks Unequipping and Offhand Swapping.";

        public override int MaxStacks => 1;

        Creature Master;

        public Wedlock(Creature master)
        {
            Master = master;

            Effect.Apply(new EffectFlag(this, Stat.SwapItem, false, 10));
            Effect.Apply(new EffectFlag(this, Stat.UnequipItem, false, 10));
        }

        public override void Update()
        {
            base.Update();

            if (Master.Dead)
                this.Remove();
        }
    }
}
