using Microsoft.Xna.Framework;
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
        public int Level;

        public override double VisualPriority => 1000;

        public EffectTrait(IEffectHolder holder, Trait trait, int level = 1)
        {
            Holder = holder;
            Trait = trait;
            Level = level;
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
            int totalLevel = equalityGroup.OfType<EffectTrait>().Sum(trait => trait.Level);
            statBlock += $"{Game.FORMAT_BOLD}{Game.FormatColor(Trait.Color)}{Trait.Name}{Game.FormatColor(Color.White)}{Game.FORMAT_BOLD} Lv{totalLevel}\n";
            string description = string.Join(string.Empty, Trait.Description.Split('\n').Select(str => $"- {str}\n"));
            statBlock += description;
        }

        public IEnumerable<T> GetSubEffects<T>() where T : Effect
        {
            return Trait.GetEffects<T>();
        }
    }
}
