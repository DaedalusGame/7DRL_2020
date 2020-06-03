using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    interface IEffectContainer
    {
        IEnumerable<T> GetSubEffects<T>() where T : Effect;
     }
}
