using Newtonsoft.Json.Linq;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectFamily : Effect
    {
        public IEffectHolder Holder;

        public Family Family;
        public bool Value;
        public double Priority;

        public EffectFamily()
        {
        }

        public EffectFamily(IEffectHolder holder, Family family) : this(holder, family, true, 0)
        {
            
        }

        public EffectFamily(IEffectHolder holder, Family family, bool value, double priority)
        {
            Holder = holder;
            Family = family;
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

        [Construct("family")]
        public static EffectFamily Construct(Context context)
        {
            return new EffectFamily();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["family"] = Family.ID;
            json["value"] = Value;
            json["priority"] = Priority;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Family = Family.GetFamily(json["family"].Value<string>());
            Value = json["value"].Value<bool>();
            Priority = json["priority"].Value<double>();
        }

        public override string ToString()
        {
            return $"{Family} {Value} (Priority {Priority})";
        }
    }
}
