using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    interface IHasPosition
    {
        int X
        {
            get;
        }
        int Y
        {
            get;
        }

        Vector2 VisualPosition
        {
            get;
        }
        Vector2 VisualTarget
        {
            get;
        }
    }
}
