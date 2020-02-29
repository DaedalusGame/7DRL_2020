using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectStatPercent : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public virtual double Percentage
        {
            get;
            set;
        }

        public EffectStatPercent(IEffectHolder holder, Stat stat, double percentage)
        {
            Holder = holder;
            Stat = stat;
            Percentage = percentage;
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
            return $"{Stat} {Percentage*100:+0;-#}% ({Holder})";
        }

        public class Stackable : EffectStatPercent
        {
            double PerStack;

            public Stackable(StatusEffect holder, Stat stat, double perStack) : base(holder, stat, perStack)
            {
                PerStack = perStack;
            }

            public override double Percentage
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
    }
}
