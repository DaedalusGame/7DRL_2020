using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectStatusEffect : Effect
    {
        public StatusEffect StatusEffect;
        public IEffectHolder Creature;
        public IEnumerable<Effect> Effects => StatusEffect.GetEffects<Effect>();

        public EffectStatusEffect()
        {
        }

        public EffectStatusEffect(StatusEffect statusEffect, IEffectHolder creature)
        {
            StatusEffect = statusEffect;
            Creature = creature;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Creature, this);
            StatusEffect.OnAdd();
        }

        public override void Remove()
        {
            //EffectManager.RemoveEffect(Creature, this);
            StatusEffect.OnRemove();
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Creature} with {StatusEffect}";
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Creature);
            json["statusEffect"] = StatusEffect.WriteJson();
            return json;
        }

        [Construct("status_effect")]
        public static EffectStatusEffect Construct(Context context)
        {
            return new EffectStatusEffect();
        }

        public override void ReadJson(JToken json, Context context)
        {
            Creature = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            StatusEffect = ReadStatusEffect(json["statusEffect"], context);
        }

        private StatusEffect ReadStatusEffect(JToken json, Context context)
        {
            string id = json["id"].Value<string>();
            var statusEffect = Serializer.Create<StatusEffect>(id, context);
            if (statusEffect != null)
                statusEffect.ReadJson(json, context);
            return statusEffect;
        }
    }
}
