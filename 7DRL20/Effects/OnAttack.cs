using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnAttack : EffectEvent<Attack>
    {
        public OnAttack(IEffectHolder holder, Func<Attack, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }
}
