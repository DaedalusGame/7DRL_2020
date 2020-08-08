using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnDeath : EffectEvent<DeathEvent>
    {
        public OnDeath(IEffectHolder holder, Func<DeathEvent, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }

    class DeathEvent
    {
        public Creature Creature;

        public DeathEvent(Creature creature)
        {
            Creature = creature;
        }
    }
}
