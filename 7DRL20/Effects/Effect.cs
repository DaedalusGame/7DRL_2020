﻿using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    [Flags]
    enum EffectType
    {
        None = 0,
        NoSerialize = 1,
        NoApply = 2,

        Innate = NoSerialize, //Doesn't serialize
        Transient = NoSerialize | NoApply, //Doesn't serialize and doesn't apply directly
    }

    [SerializeInfo]
    abstract class Effect
    {
        class StatEqualityComparer : IEqualityComparer<Effect>
        {
            public bool Equals(Effect x, Effect y)
            {
                return x.StatEquals(y);
            }

            public int GetHashCode(Effect obj)
            {
                return obj.GetStatHashCode();
            }
        }

        public static IEqualityComparer<Effect> StatEquality = new StatEqualityComparer();

        public EffectType Type = EffectType.None;
        public bool Removed = false;
        public virtual double VisualPriority => 0;

        public abstract void Apply();

        public virtual void Remove()
        {
            Removed = true;
        }

        public static void Apply(Effect effect)
        {
            effect.Apply();
        }

        public static void ApplyInnate(Effect effect)
        {
            effect.Type = EffectType.Innate;
            effect.Apply();
        }

        public virtual bool StatEquals(Effect other)
        {
            return false;
        }

        public virtual int GetStatHashCode()
        {
            return GetHashCode();
        }

        public virtual void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            //NOOP
        }

        public Color GetStatColor(IEffectHolder holder)
        {
            if(holder is Trait trait)
            {
                return trait.Color;
            }
            return Color.White;
        }

        //TODO: Mark abstract so I can't make mistakes
        public virtual JToken WriteJson()
        {
            return null;
        }

        //TODO: Mark abstract so I can't make mistakes
        public virtual void ReadJson(JToken json, Context context)
        {
            //NOOP
        }
    }
}
