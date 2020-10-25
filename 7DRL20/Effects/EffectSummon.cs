using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectSummon : Effect
    {
        public IEffectHolder Master;
        public IEffectHolder Slave;

        public EffectSummon()
        {
        }

        public EffectSummon(IEffectHolder master, IEffectHolder slave)
        {
            Master = master;
            Slave = slave;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Master, this);
            EffectManager.AddEffect(Slave, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        [Construct("summoned")]
        public static EffectSummon Construct(Context context)
        {
            return new EffectSummon();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["master"] = Serializer.GetHolderID(Master);
            json["slave"] = Serializer.GetHolderID(Slave);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Master = Serializer.GetHolder(json["master"], context);
            Slave = Serializer.GetHolder(json["slave"], context);
        }
    }
}
