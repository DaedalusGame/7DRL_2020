using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    abstract class EffectStatMultiplyBase : Effect, IStat
    {
        public IEffectHolder Holder;
        public Stat Stat
        {
            get;
            set;
        }
        public abstract double Multiplier(IEffectHolder holder);

        public override double VisualPriority => Stat.Priority + 0.2;

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
            return other is EffectStatMultiply stat && stat.Stat == Stat;
        }

        public override int GetStatHashCode()
        {
            return Stat.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            if (Stat.Hidden)
                return;
            

            var stats = equalityGroup.OfType<EffectStatMultiplyBase>();
            AddStatLine(ref statBlock, stats);
        }

        public abstract void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup);
    }

    class EffectStatMultiply : EffectStatMultiplyBase
    {
        public double BaseMultiplier;
        public override double Multiplier(IEffectHolder holder) => BaseMultiplier;

        public EffectStatMultiply()
        {
        }

        public EffectStatMultiply(IEffectHolder holder, Stat stat, double multiplier)
        {
            Holder = holder;
            Stat = stat;
            BaseMultiplier = multiplier;
        }

        public override string ToString()
        {
            return $"{Stat} x{BaseMultiplier} ({Holder})";
        }

        [Construct("stat_multiply")]
        public static EffectStatMultiply Construct(Context context)
        {
            return new EffectStatMultiply();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["stat"] = Stat.ID;
            json["multiplier"] = BaseMultiplier;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Stat = Stat.GetStat(json["stat"].Value<string>());
            BaseMultiplier = json["multiplier"].Value<double>();
        }

        public override void AddStatLine(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            var multiplier = equalityGroup.OfType<EffectStatMultiply>().Aggregate(1.0, (seed, stat) => seed * stat.BaseMultiplier);
            if (multiplier != 1)
                statBlock += $"{Game.FormatColor(GetStatColor(Holder))}{Game.FormatStat(Stat)} {Stat.Name} x{Math.Round(multiplier, 2)}{Game.FormatColor()}\n";
        }
    }
}
