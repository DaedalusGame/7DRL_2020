using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("stat_lock")]
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

        public override double VisualPriority => Stat.Priority + 0.9;

        public EffectStatLock()
        {
        }

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

        [Construct]
        public static EffectStatLock Construct(Context context)
        {
            return new EffectStatLock();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["stat"] = Stat.ID;
            json["max"] = MaxValue;
            json["min"] = MinValue;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Stat = Stat.GetStat(json["stat"].Value<string>());
            MaxValue = json["max"].Value<double>();
            MinValue = json["min"].Value<double>();
        }
    }
}
