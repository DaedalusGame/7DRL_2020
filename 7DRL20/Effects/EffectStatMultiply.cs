using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectStatMultiply : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public virtual double Multiplier
        {
            get;
            set;
        }

        public override double VisualPriority => Stat.Priority + 0.2;

        public EffectStatMultiply(IEffectHolder holder, Stat stat, double multiplier)
        {
            Holder = holder;
            Stat = stat;
            Multiplier = multiplier;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            //EffectManager.RemoveEffect(Holder, this);
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Stat} x{Multiplier} ({Holder})";
        }
    }
}
