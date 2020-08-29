using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("stat_add")]
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
        public bool Base = true;

        public override double VisualPriority => Stat.Priority + 0;

        public EffectStat()
        {
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

        public override bool StatEquals(Effect other)
        {
            return other is EffectStat stat && stat.Stat == Stat;
        }

        public override int GetStatHashCode()
        {
            return Stat.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            if (Stat.Hidden)
                return;
            var baseStat = equalityGroup.OfType<EffectStat>().Where(stat => stat.Base).Sum(stat => stat.Amount);
            if (baseStat != 0)
                statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {baseStat.ToString("+0;-#")} Base\n";
            var add = equalityGroup.OfType<EffectStat>().Where(stat => !stat.Base).Sum(stat => stat.Amount);
            if (add != 0)
                statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {add.ToString("+0;-#")}\n";
        }

        public override string ToString()
        {
            return $"{Stat} {Amount:+0;-#} ({Holder})";
        }

        [Construct]
        public static EffectStat Construct(Context context)
        {
            return new EffectStat();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["stat"] = Stat.Id;
            json["amount"] = Amount;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Stat = Stat.GetStat(json["stat"].Value<string>());
            Amount = json["amount"].Value<double>();
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
