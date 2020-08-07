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

        public override double VisualPriority => Stat.Priority + 0.1;

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

        public override bool StatEquals(Effect other)
        {
            return other is EffectStatPercent stat && stat.Stat == Stat;
        }

        public override int GetStatHashCode()
        {
            return Stat.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            var percentage = equalityGroup.OfType<EffectStatPercent>().Sum(stat => stat.Percentage);
            if (percentage != 0)
                statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {((int)Math.Round(percentage * 100)).ToString("+0;-#")}%\n";
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

        public class Randomized : EffectStatPercent
        {
            Random Random = new Random();
            double Lower;
            double Upper;

            public Randomized(IEffectHolder holder, Stat stat, double lower, double upper) : base(holder, stat, 0)
            {
                Lower = lower;
                Upper = upper;
            }

            public override double Percentage
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
