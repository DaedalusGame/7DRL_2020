using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
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

        public bool Innate = false;
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
            effect.Innate = true;
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
