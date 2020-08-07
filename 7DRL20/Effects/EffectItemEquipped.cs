using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    enum EquipSlot
    {
        Body,
        Offhand,
        Mainhand,
        Quiver,
    }

    class EffectItemEquipped : Effect
    {
        public Item Item;
        public Creature Wearer;
        public EquipSlot Slot;
        public IEnumerable<Effect> Effects => GetEffects();

        public IEnumerable<Effect> GetEffects()
        {
            if(Slot == EquipSlot.Offhand && !ProvidesOffhandStats()) //TODO: Should be generalized to other slots
            {
                return Enumerable.Empty<Effect>();
            }
            else
            {
                return Item.GetEquipEffects(Slot);
            }
        }

        public EffectItemEquipped(Item item, Creature wearer, EquipSlot slot)
        {
            Item = item;
            Wearer = wearer;
            Slot = slot;
        }

        private bool ProvidesOffhandStats()
        {
            if (Item is ToolPlate)
                return true;
            return false;
        }

        public override void Apply()
        {
            foreach (var equip in Enumerable.Concat(EffectManager.GetEffects<EffectItemEquipped>(Item), EffectManager.GetEffects<EffectItemEquipped>(Wearer).Where(x => x.Wearer != Wearer || x.Slot == Slot)).ToList())
                equip.Remove();
            EffectManager.AddEffect(Wearer, this);
            EffectManager.AddEffect(Item, this);
        }

        public override void Remove()
        {
            //EffectManager.RemoveEffect(Wearer, this);
            //EffectManager.RemoveEffect(Item, this);
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Wearer} equipped {Item} in slot {Slot}";
        }
    }
}
