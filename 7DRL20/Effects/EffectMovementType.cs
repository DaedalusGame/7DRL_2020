using Newtonsoft.Json.Linq;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectMovementType : Effect
    {
        public IEffectHolder Holder;

        public MovementType MovementType;
        public double Priority;

        public EffectMovementType()
        {
        }

        public EffectMovementType(IEffectHolder holder, MovementType movementType, double priority)
        {
            Holder = holder;
            MovementType = movementType;
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
            return $"Movement \"{MovementType}\" (Priority {Priority})";
        }

        [Construct("movementtype")]
        public static EffectMovementType Construct(Context context)
        {
            return new EffectMovementType();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["type"] = MovementType.ID;
            json["priority"] = Priority;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            MovementType = MovementType.GetMovementType(json["type"].Value<string>());
            Priority = json["priority"].Value<double>();
        }
    }
}
