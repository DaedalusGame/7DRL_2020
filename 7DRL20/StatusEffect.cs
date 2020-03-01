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
        public int Stacks = 1;

        public StatusEffect()
        {
            ObjectID = EffectManager.NewID();
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
            return new[] { this };
        }

        public abstract void OnAdd();

        public abstract void OnRemove();
    }

    class DefenseDown : StatusEffect
    {
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

        public override StatusEffect[] Combine(StatusEffect other)
        {
            if (other is DefenseDown defenseDown)
                Stacks += defenseDown.Stacks;
            return new[] { this };
        }

        public override string ToString()
        {
            return $"{base.ToString()} x{Stacks}";
        }
    }

    class Poison : StatusEffect
    {
        public Poison() : base()
        {
            
        }

        public override void OnAdd()
        {
            throw new NotImplementedException();
        }

        public override void OnRemove()
        {
            throw new NotImplementedException();
        }

        public override bool CanCombine(StatusEffect other)
        {
            return base.CanCombine(other);
        }

        public override StatusEffect[] Combine(StatusEffect other)
        {
            return base.Combine(other);
        }
    }
}
