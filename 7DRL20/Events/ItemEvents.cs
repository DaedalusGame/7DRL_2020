using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Events
{
    class EventItem : Event
    {
        Item Item;

        public EventItem(Item item)
        {
            Item = item;
        }
    }
}
