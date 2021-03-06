﻿using Microsoft.Xna.Framework;
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
        public Slider Frame = new Slider(5);

        public Message(IEffectHolder holder)
        {
            Holder = holder;
        }

        public void Update()
        {
            Frame += 1;
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
            TextInternal = text;
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

        public override bool CanCombine(Message other)
        {
            if (other.Holder == Holder && other is MessageHeal damage)
                return true;
            return false;
        }

        public override Message[] Combine(Message other)
        {
            if (other is MessageHeal damage)
            {
                return new[] { new MessageHeal(Holder, Heal + damage.Heal) };
            }
            return base.Combine(other);
        }
    }

    class MessageStatusBuildup : Message
    {
        StatusEffect StatusEffect;
        int Buildup;

        public override string Text => GetMessage();

        public MessageStatusBuildup(IEffectHolder holder, StatusEffect statusEffect, int buildup) : base(holder)
        {
            StatusEffect = statusEffect;
            Buildup = buildup;
        }

        public string GetMessage()
        {
            var color = string.Empty;
            if (Buildup < 0)
                color = Game.FormatColor(Microsoft.Xna.Framework.Color.Gray);
            if (!StatusEffect.HasStacks)
                return $"{color}{StatusEffect.Name}";
            else
                return $"{color}{Buildup.ToString("+0;-#")} {StatusEffect.Name}";
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
                if (Math.Abs(buildup.Buildup + Buildup) >= 1)
                    return new Message[] { new MessageStatusBuildup(Holder, StatusEffect, Buildup + buildup.Buildup) };
                else
                    return new Message[] { };
            }
            return base.Combine(other);
        }
    }

    class MessageExperience : Message
    {
        double Experience;

        public override string Text => GetMessage();

        public MessageExperience(IEffectHolder holder, double experience) : base(holder)
        {
            Experience = experience;
        }

        public string GetMessage()
        {
            return $"{Game.FormatColor(Color.LightYellow)}+{Experience} EXP";
        }

        public override bool CanCombine(Message other)
        {
            if (other.Holder == Holder && other is MessageExperience experience)
                return true;
            return false;
        }

        public override Message[] Combine(Message other)
        {
            if (other is MessageExperience experience)
            {
                return new[] { new MessageExperience(Holder, Experience + experience.Experience) };
            }
            return base.Combine(other);
        }
    }

}
