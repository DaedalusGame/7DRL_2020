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
            public override bool Done => Messages.Empty();

            public override void Update()
            {
                //NOOP
            }
        }

        static Stack<List<Message>> Collections = new Stack<List<Message>>();
        static List<Message> Messages = new List<Message>();
        static List<DamagePopup> CurrentMessages = new List<DamagePopup>();

        static public Wait Wait = new WaitPopup();

        public static void AddInternal(Message message)
        {
            Messages.Insert(0, message);
        }

        public static void StartCollect()
        {
            Collections.Push(new List<Message>());
        }

        public static void Add(Message message)
        {
            if(Collections.Count > 0)
            {
                List<Message> messages = Collections.Peek();
                messages.Add(message);
            }
            else
            {
                AddInternal(message);
            }
        }

        public static void FinishCollect()
        {
            List<Message> messages = Collections.Pop();
            foreach(var message in CombineMessages(messages))
            {
                Add(message);
            }
        }

        public static IEnumerable<Message> CombineMessages(IEnumerable<Message> messages)
        {
            List<Message> shunt = new List<Message>();
            foreach(Message message in messages)
            {
                bool combined = false;
                for(int i = shunt.Count-1; i >= 0; i--)
                {
                    Message existing = shunt[i];
                    if(existing.CanCombine(message))
                    {
                        shunt.RemoveAt(i);
                        foreach(Message toAdd in existing.Combine(message))
                        {
                            shunt.Insert(i, toAdd);
                        }
                        combined = true;
                        break;
                    }
                }
                if(!combined)
                {
                    shunt.Insert(0, message);
                }
            }
            return shunt;
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
                Message message = Messages[i];
                DamagePopup current = lookupCurrent[message.Holder].FirstOrDefault();

                if (current == null || current.Frame.Time > 15)
                {
                    CurrentMessages.Remove(current);
                    Messages.RemoveAt(i);
                    if (message.Holder is IHasPosition position)
                        CurrentMessages.Add(new DamagePopup(scene, () => position.VisualTarget, message, 60));
                    break;
                }
            }
        }
    }
}
