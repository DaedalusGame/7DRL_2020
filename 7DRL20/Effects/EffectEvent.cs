using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectEvent<T> : Effect
    {
        public IEffectHolder Holder;
        public Func<T, IEnumerable<Wait>> EventFunction;

        public EffectEvent(IEffectHolder holder, Func<T, IEnumerable<Wait>> eventFunction)
        {
            Holder = holder;
            EventFunction = eventFunction;
        }

        public Wait Trigger(T mine)
        {
            return Scheduler.Instance.RunAndWait(EventFunction(mine));
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }
    }
}
