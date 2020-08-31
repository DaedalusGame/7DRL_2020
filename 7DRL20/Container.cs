using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    [SerializeInfo]
    class Container : IEffectHolder, IJsonSerializable
    {
        public ReusableID ObjectID
        {
            get;
            private set;
        }

        public IEnumerable<Item> Items => this.GetEffects<EffectItemInventory>().Select(effect => effect.Item);

        public Guid GlobalID
        {
            get;
            private set;
        }

        public IEffectHolder Owner;
        public Map Map
        {
            get
            {
                if (Owner is Tile tile)
                    return tile.Map;
                else if (Owner is IJsonSerializable serializable)
                    return serializable.Map;
                else
                    return null;
            }
            set
            {
                //NOOP
            }
        }

        public Container()
        {
            ObjectID = EffectManager.SetID(this);
            GlobalID = EffectManager.SetGlobalID(this);
        }

        [Construct("container")]
        public static Container Construct(Context context)
        {
            return new Container();
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

        public JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["objectId"] = Serializer.GetHolderID(this);
            return json;
        }

        public void ReadJson(JToken json, Context context)
        {
            Guid globalId = Guid.Parse(json["objectId"].Value<string>());
            GlobalID = EffectManager.SetGlobalID(this, globalId);
        }
    }
}
