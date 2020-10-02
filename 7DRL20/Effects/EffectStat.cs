using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoguelikeEngine.Effects
{
    abstract class EffectStatBase : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public abstract double Amount(IEffectHolder holder); 
        public bool Base = true;

        public override double VisualPriority => Stat.Priority + 0;

        public EffectStatBase()
        {
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override bool StatEquals(Effect other)
        {
            return other is IStat stat && other.GetType() == GetType() && stat.Stat == Stat;
        }

        public override int GetStatHashCode()
        {
            return Stat.GetHashCode() ^ GetType().GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            if (Stat.Hidden)
                return;
            var baseStats = equalityGroup.OfType<EffectStatBase>().Where(stat => stat.Base);
            AddStatLine(ref statBlock, baseStats, true);
            var stats = equalityGroup.OfType<EffectStatBase>().Where(stat => !stat.Base);
            AddStatLine(ref statBlock, stats, false);
        }

        public abstract void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase);
    }

    class EffectStat : EffectStatBase
    {
        public double BaseAmount;
        public override double Amount(IEffectHolder holder) => BaseAmount;

        public EffectStat()
        {
        }

        public EffectStat(IEffectHolder holder, Stat stat, double amount)
        {
            Holder = holder;
            Stat = stat;
            BaseAmount = amount;
        }

        public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase)
        {
            var baseText = isBase ? "Base" : string.Empty;
            var amount = equalityGroup.OfType<EffectStat>().Sum(stat => stat.BaseAmount);
            if (amount != 0)
                statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {amount.ToString("+0;-#")} {baseText}\n";
        }

        public override string ToString()
        {
            return $"{Stat} {BaseAmount:+0;-#} ({Holder})";
        }

        [Construct("stat_add")]
        public static EffectStat Construct(Context context)
        {
            return new EffectStat();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["stat"] = Stat.ID;
            json["amount"] = BaseAmount;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Stat = Stat.GetStat(json["stat"].Value<string>());
            BaseAmount = json["amount"].Value<double>();
        }

        public class Stackable : EffectStatBase
        {
            double PerStack;
            public override double Amount(IEffectHolder holder) => PerStack * ((StatusEffect)Holder).Stacks;

            public Stackable(StatusEffect holder, Stat stat, double perStack) : base()
            {
                Holder = holder;
                Stat = stat;
                PerStack = perStack;
            }

            public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase)
            {
                var baseText = isBase ? "Base" : string.Empty;
                var amount = equalityGroup.OfType<Stackable>().Sum(stat => stat.PerStack);
                if (amount != 0)
                    statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {amount.ToString("+0;-#")} {baseText} per Stack\n";
            }

            public override bool StatEquals(Effect other)
            {
                return other is Stackable stat && other.GetType() == GetType() && stat.Stat == Stat && stat.Holder == Holder;
            }

            public override int GetStatHashCode()
            {
                return Stat.GetHashCode() ^ GetType().GetHashCode() ^ Holder.GetHashCode();
            }
        }

        public class Randomized : EffectStatBase
        {
            Random Random = new Random();
            double Lower;
            double Upper;
            public override double Amount(IEffectHolder holder) => Lower + Random.NextDouble() * (Upper - Lower);

            public Randomized(IEffectHolder holder, Stat stat, double lower, double upper) : base()
            {
                Holder = holder;
                Stat = stat;
                Lower = lower;
                Upper = upper;
            }

            public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase)
            {
                var baseText = isBase ? "Base" : string.Empty;
                var lower = equalityGroup.OfType<Randomized>().Sum(stat => stat.Lower);
                var upper = equalityGroup.OfType<Randomized>().Sum(stat => stat.Upper);
                if (lower != 0 || upper != 0)
                    statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {lower.ToString("+0;-#")} - {upper.ToString("+0;-#")} {baseText}\n";
            }
        }

        public class Special : EffectStatBase
        {
            public delegate double AmountDelegate(Effect effect, IEffectHolder holder);
            public delegate void StatLineDelegate(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase);

            public override double Amount(IEffectHolder holder) => AmountFunc(this, holder);
            AmountDelegate AmountFunc;
            StatLineDelegate StatLineFunc;
            string Tag;

            public Special(IEffectHolder holder, Stat stat, AmountDelegate amount, StatLineDelegate statLine, string tag) : base()
            {
                Holder = holder;
                Stat = stat;
                AmountFunc = amount;
                StatLineFunc = statLine;
                Tag = tag;
            }

            public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase)
            {
                StatLineFunc(ref statBlock, equalityGroup, isBase);
            }

            public override bool StatEquals(Effect other)
            {
                return other is Special stat && other.GetType() == GetType() && stat.Stat == Stat && stat.Tag == Tag;
            }

            public override int GetStatHashCode()
            {
                return Stat.GetHashCode() ^ GetType().GetHashCode() ^ Tag.GetHashCode();
            }
        }
    }
}
