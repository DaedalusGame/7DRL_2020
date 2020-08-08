using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnTurn : EffectEvent<TurnEvent>
    {
        public OnTurn(IEffectHolder holder, Func<TurnEvent, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }

    class TurnEvent
    {
        public Turn Turn;
        public Creature Creature;

        public TurnEvent(Turn turn, Creature creature)
        {
            Turn = turn;
            Creature = creature;
        }
    }
}
