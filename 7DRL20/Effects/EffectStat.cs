using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectStat : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public virtual double Amount
        {
            get;
            set;
        }

        public EffectStat(IEffectHolder holder, Stat stat, double amount)
        {
            Holder = holder;
            Stat = stat;
            Amount = amount;
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
            return $"{Stat} {Amount:+0;-#} ({Holder})";
        }
    }
}
