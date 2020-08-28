using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("on_tile")]
    class OnTile : Effect, IPosition
    {
        public IEffectHolder Subject => Holder;

        MapTile MapTile;
        public Tile Tile => MapTile.Tile;
        public IEffectHolder Holder;
        public IEnumerable<Effect> Effects => Tile.GetEffects<Effect>();

        public OnTile(MapTile tile, IEffectHolder holder)
        {
            MapTile = tile;
            Holder = holder;
        }

        [Construct]
        public static OnTile Construct()
        {
            return null;
        }

        public override void Apply()
        {
            MapTile.AddEffect(this);
            Holder.AddEffect(this);
        }

        public override string ToString()
        {
            return $"On tile {MapTile}";
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["tile"] = Serializer.GetHolderID(MapTile);
            return json;
        }

        public override void ReadJson()
        {
            base.ReadJson();
        }

        [SerializeInfo("on_tile_primary")]
        public class Primary : OnTile
        {
            public Primary(MapTile tile, IEffectHolder holder) : base(tile, holder)
            {
            }

            [Construct]
            public static Primary Construct()
            {
                return null;
            }

            public override void Apply()
            {
                Subject.ClearPosition();
                base.Apply();
            }

            public override string ToString()
            {
                return $"On tile {MapTile} (Primary)";
            }
        }
    }
}
