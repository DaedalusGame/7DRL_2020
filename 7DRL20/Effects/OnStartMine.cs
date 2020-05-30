using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnStartMine : EffectEvent<MineEvent>
    {
        public OnStartMine(IEffectHolder holder, Func<MineEvent, IEnumerable<Wait>> eventFunction) : base(holder, eventFunction)
        {
        }
    }
}
