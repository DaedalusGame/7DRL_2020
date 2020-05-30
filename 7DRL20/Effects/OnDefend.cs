using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnDefend : EffectEvent<Attack>
    {
        public OnDefend(IEffectHolder holder, Func<Attack, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }
}
