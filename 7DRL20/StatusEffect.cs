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

        public abstract string Name
        {
            get;
        }
        public abstract string Description
        {
            get;
        }
        public string BuildupText => ((int)(BuildupRound * 100)).ToString("+0;-#")+"%";
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
                builder.Remove(0, 1);
                return builder.ToString();
            }
        }

        public int LastChange = 0;

        public StatusEffect()
        {
            ObjectID = EffectManager.NewID(this);
        }

        public void Apply()
        {
            Effect.Apply(new EffectStatusEffect(this, Creature));
        }

        public void Remove()
        {
            foreach (var effect in EffectManager.GetEffects<Effects.EffectStatusEffect>(this).Where(stat => stat.StatusEffect == this))
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
            AddBuildup(other.Buildup);
            return new[] { this };
        }

        private void AddBuildup(double buildup)
        {
            int lastStacks = Stacks;
            Buildup += buildup;
            if (Stacks > lastStacks)
                OnStackChange(Stacks - lastStacks);
        }

        public abstract void OnAdd();

        public abstract void OnRemove();

        public virtual void OnStackChange(int delta)
        {
            LastChange += delta;
        }
    }

    class DefenseDown : StatusEffect
    {
        public override string Name => $"Defense Down";
        public override string Description => $"Defense is reduced by {Stacks * 10}%";

        public DefenseDown() : base()
        {
            Effect.Apply(new EffectStatPercent.Stackable(this, Stat.Defense, -0.1));
        }

        public override void OnAdd()
        {
            //Your defense decreases!
        }

        public override void OnRemove()
        {
            //Your defense returns to normal
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
}
