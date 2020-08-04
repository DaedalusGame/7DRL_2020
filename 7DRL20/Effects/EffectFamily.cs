using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectFamily : Effect
    {
        public IEffectHolder Holder;

        public Family Family;
        public bool Value;
        public double Priority;

        public EffectFamily(IEffectHolder holder, Family family) : this(holder, family, true, 0)
        {
            
        }

        public EffectFamily(IEffectHolder holder, Family family, bool value, double priority)
        {
            Holder = holder;
            Family = family;
            Value = value;
            Priority = priority;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Family} {Value} (Priority {Priority})";
        }
    }
}
