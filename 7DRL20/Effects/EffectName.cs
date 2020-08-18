using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectName : Effect
    {
        public IEffectHolder Holder;

        public string Name;
        public double Priority;

        public EffectName(IEffectHolder holder, string name, double priority)
        {
            Holder = holder;
            Name = name;
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
            return $"Named \"{Name}\" (Priority {Priority})";
        }
    }
}
