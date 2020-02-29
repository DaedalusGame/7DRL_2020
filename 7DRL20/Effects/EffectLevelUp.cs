using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectLevelUp : Effect
    {
        public List<Effect> Effects = new List<Effect>();
        public int Level;
        public int StatPoints;
        public Creature Creature;

        public EffectLevelUp(Creature creature, int level, int statPoints)
        {
            Creature = creature;
            Level = level;
            StatPoints = statPoints;
        }

        public void AllocatePoint(Effect effect)
        {
            Effects.Add(effect);
            effect.Apply();
            StatPoints--;
        }

        public override void Apply()
        {
            foreach (Effect effect in Effects)
            {
                Creature.AddEffect(effect);
            }
            Creature.AddEffect(this);
        }

        public override void Remove()
        {
            foreach(Effect effect in Effects)
            {
                effect.Remove();
            }
            base.Remove();
        }
    }
}
