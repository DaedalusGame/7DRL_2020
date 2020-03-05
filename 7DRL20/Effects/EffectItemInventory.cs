using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectItemInventory : Effect, IPosition
    {
        public IEffectHolder Subject => Item;

        public Item Item;
        public IEffectHolder Holder;

        public EffectItemInventory(Item item, IEffectHolder holder)
        {
            Item = item;
            Holder = holder;
        }

        public override void Apply()
        {
            Subject.ClearPosition();
            EffectManager.AddEffect(Holder, this);
            EffectManager.AddEffect(Item, this);
        }

        public override void Remove()
        {
            foreach (var effect in EffectManager.GetEffects<EffectItemEquipped>(Item).Where(stat => stat.Item == Item))
            {
                effect.Remove();
            }
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Holder} has {Item}";
        }
    }
}
