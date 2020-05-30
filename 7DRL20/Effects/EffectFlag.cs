using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectFlag : Effect
    {
        public IEffectHolder Holder;

        public Stat Flag;
        public bool Value;
        public double Priority;

        public EffectFlag(IEffectHolder holder, Stat flag, bool value, double priority)
        {
            Holder = holder;
            Flag = flag;
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
            return $"{Flag} {Value} (Priority {Priority})";
        }
    }
}
