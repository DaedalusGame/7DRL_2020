using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Events
{
    class EventCreature : Event
    {
        public Creature Creature;

        public EventCreature(Creature creature)
        {
            Creature = creature;
        }
    }

    class EventMove : EventCreature
    {
        public class Finish : EventMove
        {
            public Finish(Creature creature, Tile source, Tile destination) : base(creature, source, destination)
            {
            }
        }

        public Tile Source;
        public Tile Destination;

        public EventMove(Creature creature, Tile source, Tile destination) : base(creature)
        {
            Source = source;
            Destination = destination;
        }
    }
}
