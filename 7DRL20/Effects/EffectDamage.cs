using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("damage")]
    class EffectDamage : Effect
    {
        public IEffectHolder Holder;
        public double Amount;
        public Element Element;

        public EffectDamage()
        {
        }

        public EffectDamage(IEffectHolder holder, double amount, Element element)
        {
            Holder = holder;
            Amount = amount;
            Element = element;
        }

        private void Modify(double damage)
        {
            Amount += damage;
            if (Amount <= 0)
                Remove();
        }

        public override void Apply()
        {
            var damage = EffectManager.GetEffects<EffectDamage>(Holder).Where(x => x.Element == Element && x.Holder == Holder).FirstOrDefault();
            if (damage != null)
                damage.Modify(Amount);
            else if(Amount > 0)
                EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            //EffectManager.RemoveEffect(Holder, this);
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Amount} {Element} Damage ({Holder})";
        }

        [Construct]
        public static EffectDamage Construct(Context context)
        {
            return new EffectDamage();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["element"] = Element.ID;
            json["amount"] = Amount;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Element = Element.GetElement(json["element"].Value<string>());
            Amount = json["amount"].Value<double>();
        }
    }
}
