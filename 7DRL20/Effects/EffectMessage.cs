using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectMessage : Effect
    {
        public IEffectHolder Holder;
        public string Text
        {
            get;
            set;
        }

        public EffectMessage(IEffectHolder holder, string text)
        {
            Holder = holder;
            Text = text;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override string ToString()
        {
            return $"- {Text}";
        }
    }
}
