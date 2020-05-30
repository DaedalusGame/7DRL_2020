using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class Message
    {
        public IEffectHolder Holder;
        public abstract string Text
        {
            get;
        }

        public Message(IEffectHolder holder)
        {
            Holder = holder;
        }

        public virtual bool CanCombine(Message other)
        {
            return false;
        }

        public virtual Message[] Combine(Message other)
        {
            return new[] { this, other };
        }

        public override string ToString()
        {
            return $"- {Text}";
        }
    }

    class MessageText : Message
    {
        string TextInternal;
        public override string Text => TextInternal;

        public MessageText(IEffectHolder holder, string text) : base(holder)
        {
            TextInternal = Text;
        }
    }

    class MessageDamage : Message
    {
        double Damage;
        Element Element;

        public override string Text => GetMessage();

        public MessageDamage(IEffectHolder holder, double damage, Element element) : base(holder)
        {
            Damage = damage;
            Element = element;
        }

        public string GetMessage()
        {
            if (Damage > 0)
                return $"-{Damage}{Game.FormatElement(Element)}";
            else
                return $"Immune{Game.FormatElement(Element)}";
        }

        public override bool CanCombine(Message other)
        {
            if (other.Holder == Holder && other is MessageDamage damage && damage.Element == Element)
                return true;
            return false;
        }

        public override Message[] Combine(Message other)
        {
            if(other is MessageDamage damage)
            {
                return new[] { new MessageDamage(Holder, Damage + damage.Damage, Element) };
            }
            return base.Combine(other);
        }
    }

    class MessageHeal : Message
    {
        double Heal;
        public override string Text => $"{Game.FormatColor(new Microsoft.Xna.Framework.Color(128, 255, 128))}+{Heal}{Game.FormatColor(Microsoft.Xna.Framework.Color.White)}";

        public MessageHeal(IEffectHolder holder, double heal) : base(holder)
        {
            Heal = heal;
        }
    }

    class MessageStatusBuildup : Message
    {
        StatusEffect StatusEffect;
        double Buildup;

        public override string Text => GetMessage();

        public MessageStatusBuildup(IEffectHolder holder, StatusEffect statusEffect, double buildup) : base(holder)
        {
            StatusEffect = statusEffect;
            Buildup = buildup;
        }

        public string GetMessage()
        {
            if (Buildup >= StatusEffect.MaxStacks)
                return $"{StatusEffect.Name}";
            else
                return $"{StatusEffect.Name} {StatusEffect.BuildupText(Buildup)}";
        }

        public override bool CanCombine(Message other)
        {
            if (other.Holder == Holder && other is MessageStatusBuildup buildup && buildup.StatusEffect == StatusEffect)
                return true;
            return false;
        }

        public override Message[] Combine(Message other)
        {
            if (other is MessageStatusBuildup buildup)
            {
                return new[] { new MessageStatusBuildup(Holder, StatusEffect, Buildup + buildup.Buildup) };
            }
            return base.Combine(other);
        }
    }

    class MessageStatusEffect : Message
    {
        StatusEffect StatusEffect;

        public override string Text => GetMessage();

        public MessageStatusEffect(IEffectHolder holder, StatusEffect statusEffect) : base(holder)
        {
            StatusEffect = statusEffect;
        }

        public string GetMessage()
        {
            if (StatusEffect.Stacks > 0)
                return $"{StatusEffect.Name} {StatusEffect.StackText}";
            else
                return $"{Game.FormatColor(Microsoft.Xna.Framework.Color.Gray)}{StatusEffect.Name}";
        }
    }
}
