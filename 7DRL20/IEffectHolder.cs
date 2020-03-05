using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    interface IEffectHolder
    {
        ReusableID ObjectID
        {
            get;
        }

        IEnumerable<T> GetEffects<T>() where T : Effect;
    }
}
