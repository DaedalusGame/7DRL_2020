using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    static class PopupManager
    {
        class WaitPopup : Wait
        {
            public override bool Done => PopupManager.CurrentMessage == null && PopupManager.Messages.Count <= 0;

            public override void Update()
            {
                //NOOP
            }
        }

        static Queue<EffectMessage> Messages = new Queue<EffectMessage>();
        static DamagePopup CurrentMessage;

        static int PopupDelay;
        static public Wait Wait = new WaitPopup();

        public static void Add(EffectMessage message)
        {
            message.Apply();
            Messages.Enqueue(message);
        }

        public static void Update(SceneGame scene)
        {
            if (CurrentMessage != null && CurrentMessage.Destroyed)
                CurrentMessage = null;
            if (PopupDelay-- <= 0 && Messages.Count > 0)
            {
                var message = Messages.Dequeue();
                PopupDelay = 30;
                if(message.Holder is IHasPosition position)
                    CurrentMessage = new DamagePopup(scene, position.VisualTarget, message.Text, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true), 60);
                message.Remove();
            }
        }
    }
}
