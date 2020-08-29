using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("stat_damage")]
    class EffectStatDamage : Effect, IStat
    {
        public IEffectHolder Holder;
        public double Amount;
        public Stat Stat
        {
            get;
            set;
        }

        public override double VisualPriority => Stat.Priority + 0.8;

        public EffectStatDamage()
        {
        }

        public EffectStatDamage(IEffectHolder holder, double amount, Stat stat)
        {
            Holder = holder;
            Amount = amount;
            Stat = stat;
        }

        private void Modify(double damage)
        {
            Amount += damage;
            if (Amount <= 0)
                Remove();
        }

        public override void Apply()
        {
            var damage = EffectManager.GetEffects<EffectStatDamage>(Holder).Where(x => x.Stat == Stat && x.Holder == Holder).FirstOrDefault();
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
            return $"{Amount} {Stat} Damage ({Holder})";
        }

        [Construct]
        public static EffectStatDamage Construct(Context context)
        {
            return new EffectStatDamage();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["stat"] = Stat.Id;
            json["amount"] = Amount;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Stat = Stat.GetStat(json["stat"].Value<string>());
            Amount = json["amount"].Value<double>();
        }
    }
}
