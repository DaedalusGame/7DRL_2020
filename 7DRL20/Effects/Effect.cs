using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    abstract class Effect
    {
        public bool Removed = false;

        public abstract void Apply();

        public virtual void Remove()
        {
            Removed = true;
        }

        public static void Apply(Effect effect)
        {
            effect.Apply();
        }
    }
}
