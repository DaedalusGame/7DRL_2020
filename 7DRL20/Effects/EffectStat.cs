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

        public class Stackable : EffectStat
        {
            double PerStack;

            public Stackable(StatusEffect holder, Stat stat, double perStack) : base(holder, stat, perStack)
            {
                PerStack = perStack;
            }

            public override double Amount
            {
                get
                {
                    return PerStack * ((StatusEffect)Holder).Stacks;
                }
                set
                {
                    //NOOP
                }
            }
        }

        public class Randomized : EffectStat
        {
            Random Random = new Random();
            double Lower;
            double Upper;

            public Randomized(IEffectHolder holder, Stat stat, double lower, double upper) : base(holder, stat, 0)
            {
                Lower = lower;
                Upper = upper;
            }

            public override double Amount
            {
                get
                {
                    return Lower + Random.NextDouble() * (Upper - Lower);
                }
                set
                {
                    //NOOP
                }
            }
        }
    }
}
