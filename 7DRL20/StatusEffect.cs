using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoguelikeEngine.Effects;

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

        protected void AddBuildup(double buildup)
        {
            int lastStacks = Stacks;
            Buildup += buildup;
            Buildup = Math.Max(0, Buildup);
            if (!Hidden)
                PopupManager.Add(new MessageStatusBuildup(Creature, this, buildup));
            if (Stacks > lastStacks)
                OnStackChange(Stacks - lastStacks);
        }

        public virtual void OnAdd()
        {
            if(!Hidden)
                PopupManager.Add(new MessageStatusBuildup(Creature, this, Buildup));
        }

        public virtual void OnRemove()
        {
            if (!Hidden)
                PopupManager.Add(new MessageStatusEffect(Creature, this));
        }

        public virtual void OnStackChange(int delta)
        {
            LastChange += delta;
            if (!Hidden)
                PopupManager.Add(new MessageStatusEffect(Creature, this));
        }
    }

    class BleedLesser : StatusEffect
    { 
         public static Element Element = new Element("Bleed", SpriteLoader.Instance.AddSprite("content/element_blood"));

        public override string Name => $"Lesser Bleed";
        public override string Description => $"Lose HP over time.";

        public override int MaxStacks => 3;

        public BleedLesser() : base()
        {
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is DefenseDown;
        }

        public override void OnStackChange(int delta)
        {
            base.OnStackChange(delta);
            if(Stacks >= MaxStacks)
            {
                Remove();
            }
        }

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
                Creature.TakeDamage(1, Element);
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

        public double BloodLoss;

        public BleedGreater() : base()
        {
        }

        public override void OnStackChange(int delta)
        {
            if (delta > 0)
            {
                Creature.TakeDamage(delta * 30, BleedLesser.Element);
                BloodLoss += delta * 30;
            }
            if (BloodLoss >= Creature.GetStat(Stat.HP))
            {
                //Proc Anemia
            }
            base.OnStackChange(delta);
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is BleedGreater;
        }

        public override void Update()
        {
            base.Update();
            if (Stacks >= 1)
            {
                Creature.TakeDamage(5, BleedLesser.Element);
                BloodLoss += 5;
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks} ({BloodLoss} Blood Loss)";
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

        public override bool CanCombine(StatusEffect other)
        {
            return other is DefenseDownPoison;
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
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, -3));
        }

        public override bool CanCombine(StatusEffect other)
        {
            return other is DefenseDown;
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
            Effect.Apply(new EffectStat.Stackable(this, Stat.Defense, 3));
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

        public static Element Element = new Element("Poison", SpriteLoader.Instance.AddSprite("content/element_poison"));

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
                Creature.TakeDamage(Math.Pow(2, Stacks - 1), Element);
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

        public override int MaxStacks => 5;

        Creature Master;
        static Random Random = new Random();

        public Slimed(Creature master) : base()
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
            return other is Slimed slimed && slimed.Master == Master;
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
}
