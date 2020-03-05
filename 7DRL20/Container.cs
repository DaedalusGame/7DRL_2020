using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Container : IEffectHolder
    {
        public ReusableID ObjectID
        {
            get;
            private set;
        }

        public IEnumerable<Item> Items => this.GetEffects<EffectItemInventory>().Select(effect => effect.Item);

        public Container()
        {
            ObjectID = EffectManager.NewID(this);
        }

        public void Add(Item item, bool tryMerge)
        {
            if(tryMerge)
            foreach (Item existing in this.GetInventory())
            {
                bool merged = existing.Merge(item);
                if (merged)
                {
                    item.Destroy();
                    return;
                }
            }
            Effect.Apply(new EffectItemInventory(item, this));
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }
    }
}
