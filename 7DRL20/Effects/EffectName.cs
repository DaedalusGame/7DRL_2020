using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectName : Effect
    {
        public IEffectHolder Holder;

        public string Name;
        public double Priority;

        public EffectName()
        {
        }

        public EffectName(IEffectHolder holder, string name, double priority)
        {
            Holder = holder;
            Name = name;
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
            return $"Named \"{Name}\" (Priority {Priority})";
        }

        [Construct("named")]
        public static EffectName Construct(Context context)
        {
            return new EffectName();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["name"] = Name;
            json["priority"] = Priority;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Name = json["name"].Value<string>();
            Priority = json["priority"].Value<double>();
        }
    }
}
