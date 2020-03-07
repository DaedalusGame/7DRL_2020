using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnStartMine : Effect
    {
        public IEffectHolder Holder;
        public Action<MineEvent> Trigger;

        public OnStartMine(IEffectHolder holder, Action<MineEvent> trigger)
        {
            Holder = holder;
            Trigger = trigger;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }
    }
}
