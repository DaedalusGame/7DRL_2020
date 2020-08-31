using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectFlag : Effect
    {
        public IEffectHolder Holder;

        public Stat Flag;
        public bool Value;
        public double Priority;

        public EffectFlag()
        {
        }

        public EffectFlag(IEffectHolder holder, Stat flag, bool value, double priority)
        {
            Holder = holder;
            Flag = flag;
            Value = value;
            Priority = priority;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Flag} {Value} (Priority {Priority})";
        }

        [Construct("flag")]
        public static EffectFlag Construct(Context context)
        {
            return new EffectFlag();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["flag"] = Flag.ID;
            json["value"] = Value;
            json["priority"] = Priority;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Flag = Stat.GetStat(json["flag"].Value<string>());
            Value = json["value"].Value<bool>();
            Priority = json["priority"].Value<double>();
        }
    }
}
