using Newtonsoft.Json.Linq;
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

        public EffectItemInventory()
        {
        }

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

        [Construct("in_inventory")]
        public static EffectItemInventory Construct(Context context)
        {
            return new EffectItemInventory();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["item"] = Serializer.GetHolderID(Item);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder(json["holder"], context);
            Item = Serializer.GetHolder<Item>(json["item"], context);
        }
    }
}
