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

        static Stack<List<EffectMessage>> Collections = new Stack<List<EffectMessage>>();
        static List<EffectMessage> Messages = new List<EffectMessage>();
        static List<DamagePopup> CurrentMessages = new List<DamagePopup>();
        static DamagePopup CurrentMessage;

        static int PopupDelay;
        static public Wait Wait = new WaitPopup();

        public static void AddInternal(EffectMessage message)
        {
            Messages.Insert(0, message);
        }

        public static void StartCollect()
        {
            Collections.Push(new List<EffectMessage>());
        }

        public static void Add(EffectMessage message)
        {
            if(Collections.Count > 0)
            {
                List<EffectMessage> messages = Collections.Peek();
                messages.Add(message);
            }
            else
            {
                AddInternal(message);
            }
        }

        public static void FinishCollect()
        {
            List<EffectMessage> messages = Collections.Pop();
            foreach(var message in messages)
            {
                AddInternal(message);
            }
        }

        public static void Update(SceneGame scene)
        {
            /*if (CurrentMessage != null && CurrentMessage.Destroyed)
                CurrentMessage = null;
            if (PopupDelay-- <= 0 && Messages.Count > 0)
            {
                var message = Messages.Dequeue();
                PopupDelay = 30;
                if(message.Holder is IHasPosition position)
                    CurrentMessage = new DamagePopup(scene, position.VisualTarget, message.Text, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true), 60);
                message.Remove();
            }*/

            CurrentMessages.RemoveAll(message => message.Destroyed);
            var lookupCurrent = CurrentMessages.ToLookup(x => x.Message.Holder);
            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                EffectMessage message = Messages[i];
                DamagePopup current = lookupCurrent[message.Holder].FirstOrDefault();

                if (current == null || current.Frame.Time > 15)
                {
                    CurrentMessages.Remove(current);
                    Messages.RemoveAt(i);
                    if (message.Holder is IHasPosition position)
                        CurrentMessages.Add(new DamagePopup(scene, position.VisualTarget, message, 60));
                    break;
                }
            }
        }
    }
}
