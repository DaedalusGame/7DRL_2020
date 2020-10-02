using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    abstract class EffectStatPercentBase : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public abstract double Percentage(IEffectHolder holder);

        public override double VisualPriority => Stat.Priority + 0.1;

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
            return other is EffectStatPercentBase stat && stat.Stat == Stat;
        }

        public override int GetStatHashCode()
        {
            return Stat.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            if (Stat.Hidden)
                return;

            var stats = equalityGroup.OfType<EffectStatPercentBase>();
            AddStatLine(ref statBlock, stats);
        }

        public abstract void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup);
    }

    class EffectStatPercent : EffectStatPercentBase
    {
        public double BasePercentage;
        public override double Percentage(IEffectHolder holder) => BasePercentage;

        public EffectStatPercent()
        {
        }

        public EffectStatPercent(IEffectHolder holder, Stat stat, double percentage)
        {
            Holder = holder;
            Stat = stat;
            BasePercentage = percentage;
        }

        public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            var percentage = equalityGroup.OfType<EffectStatPercent>().Sum(stat => stat.BasePercentage);
            if (percentage != 0)
                statBlock += $"{Game.FormatStat(Stat)} {Stat.Name} {((int)Math.Round(percentage * 100)).ToString("+0;-#")}%\n";
        }

        public override string ToString()
        {
            return $"{Stat} {BasePercentage * 100:+0;-#}% ({Holder})";
        }

        [Construct("stat_percent")]
        public static EffectStatPercent Construct(Context context)
        {
            return new EffectStatPercent();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["stat"] = Stat.ID;
            json["percentage"] = BasePercentage;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Stat = Stat.GetStat(json["stat"].Value<string>());
            BasePercentage = json["percentage"].Value<double>();
        }

        public class Stackable : EffectStatPercentBase
        {
            double PerStack;

            public override double Percentage(IEffectHolder holder) => PerStack * ((StatusEffect)Holder).Stacks;

            public Stackable(StatusEffect holder, Stat stat, double perStack)
            {
                Holder = holder;
                Stat = stat;
                PerStack = perStack;
            }

            public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup)
            {
                var percentage = equalityGroup.OfType<Stackable>().Sum(stat => stat.PerStack);
                if (percentage != 0)
                    statBlock += $"{Game.FormatColor(GetStatColor(Holder))}{Game.FormatStat(Stat)} {Stat.Name} {((int)Math.Round(percentage * 100)).ToString("+0;-#")}% per Stack{Game.FormatColor()}\n";
            }
        }

        public class Randomized : EffectStatPercentBase
        {
            Random Random = new Random();
            double Lower;
            double Upper;

            public override double Percentage(IEffectHolder holder) => Lower + Random.NextDouble() * (Upper - Lower);

            public Randomized(IEffectHolder holder, Stat stat, double lower, double upper)
            {
                Holder = holder;
                Stat = stat;
                Lower = lower;
                Upper = upper;
            }

            public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup)
            {
                var lower = equalityGroup.OfType<Randomized>().Sum(stat => stat.Lower);
                var upper = equalityGroup.OfType<Randomized>().Sum(stat => stat.Upper);
                if (lower != 0 || upper != 0)
                    statBlock += $"{Game.FormatColor(GetStatColor(Holder))}{Game.FormatStat(Stat)} {Stat.Name} {((int)Math.Round(lower * 100)).ToString("+0;-#")}% ~ {((int)Math.Round(upper * 100)).ToString("+0;-#")}%{Game.FormatColor()}\n";
            }
        }
    }
}
