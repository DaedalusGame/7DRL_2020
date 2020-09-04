using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class PopupHelper
    {
        class WaitPopup : Wait
        {
            public override bool Done => Global.Messages.Empty();

            public override void Update()
            {
                //NOOP
            }
        }

        public static PopupHelper Global = new PopupHelper();

        List<Message> Messages = new List<Message>();
        List<DamagePopup> CurrentMessages = new List<DamagePopup>();
        bool Dirty;

        static public Wait Wait = new WaitDone();

        public void Add(Message message)
        {
            AddInternal(message);
            Dirty = true;
        }

        private void AddInternal(Message message)
        {
            Messages.Insert(0, message);
        }

        public void CombineMessages()
        {
            var toCombine = Messages.ToList();
            Messages.Clear();
            List<Message> shunt = Messages;
            foreach (Message message in toCombine)
            {
                bool combined = false;
                for (int i = shunt.Count - 1; i >= 0; i--)
                {
                    Message existing = shunt[i];
                    if (existing.CanCombine(message))
                    {
                        shunt.RemoveAt(i);
                        foreach (Message toAdd in existing.Combine(message))
                        {
                            shunt.Insert(i, toAdd);
                        }
                        combined = true;
                        break;
                    }
                }
                if (!combined)
                {
                    shunt.Insert(0, message);
                }
            }
        }

        public void Update(SceneGame scene)
        {
            foreach (var message in Messages)
            {
                message.Update();
            }

            if (Dirty)
            {
                CombineMessages();
                Dirty = false;
            }

            CurrentMessages.RemoveAll(message => message.Destroyed);
            var handled = new HashSet<IEffectHolder>();
            var lookupCurrent = CurrentMessages.ToLookup(x => x.Message.Holder);
            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                Message message = Messages[i];
                if (handled.Contains(message.Holder) || !message.Frame.Done)
                    continue;

                DamagePopup current = lookupCurrent[message.Holder].FirstOrDefault();

                if (current == null || current.Frame.Time > 5)
                {
                    CurrentMessages.Remove(current);
                    Messages.RemoveAt(i);
                    if (message.Holder is IHasPosition position)
                        CurrentMessages.Add(new DamagePopup(scene, () => position.VisualTarget, message, 60));
                    handled.Add(message.Holder);
                }
            }
        }

        public void Finish()
        {
            CombineMessages();
            foreach(var message in Messages)
            {
                Global.Add(message);
            }
        }
    }
}
