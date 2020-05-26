using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class Trait : Effect
    {
        public IEffectHolder Holder;
        public string Name;
        public string Description;

        public override double VisualPriority => 1000;

        public Trait(IEffectHolder holder, string name, string description)
        {
            Holder = holder;
            Name = name;
            Description = description;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override bool StatEquals(Effect other)
        {
            return other is Trait trait && trait.Name == Name;
        }

        public override int GetStatHashCode()
        {
            return Name.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            statBlock += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD} Lv{equalityGroup.Count()}\n";
            statBlock += $"- {Description}\n";
        }
    }
}
