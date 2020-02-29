using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class EffectSingleton : IEffectHolder
    {
        public ReusableID ObjectID
        {
            get;
            private set;
        }

        public EffectSingleton()
        {
            ObjectID = EffectManager.NewID();
        }
    }
}
