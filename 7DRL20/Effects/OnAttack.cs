﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnAttack : Effect
    {
        public IEffectHolder Holder;
        public Action<Attack> Trigger;

        public OnAttack(IEffectHolder holder, Action<Attack> trigger)
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
