using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectTrait : Effect, IEffectContainer
    {
        public IEffectHolder Holder;
        public Trait Trait;

        public override double VisualPriority => 1000;

        public EffectTrait(IEffectHolder holder, Trait trait)
        {
            Holder = holder;
            Trait = trait;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override bool StatEquals(Effect other)
        {
            return other is EffectTrait trait && trait.Trait == Trait;
        }

        public override int GetStatHashCode()
        {
            return Trait.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            statBlock += $"{Game.FORMAT_BOLD}{Trait.Name}{Game.FORMAT_BOLD} Lv{equalityGroup.Count()}\n";
            statBlock += $"- {Trait.Description}\n";
        }

        public IEnumerable<T> GetSubEffects<T>() where T : Effect
        {
            return Trait.GetEffects<T>();
        }
    }
}
