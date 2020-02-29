using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectStatLock : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public double MaxValue;
        public double MinValue;

        public EffectStatLock(IEffectHolder holder, Stat stat, double min, double max)
        {
            Holder = holder;
            Stat = stat;
            MinValue = min;
            MaxValue = max;
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
            return $"{Stat} locked between {MinValue} and {MaxValue} ({Holder})";
        }
    }
}
