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
    }
}
