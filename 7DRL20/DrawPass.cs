using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    enum DrawPass
    {
        Tile,
        EffectLow,
        Item,
        Creature,
        Effect,
        EffectAdditive,
        UIWorld,
        UI,
    }
}
