using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoguelikeEngine.Effects
{
    class OnTile : Effect, IPosition
    {
        public IEffectHolder Subject => Holder;

        MapTile MapTile;
        public Tile Tile => MapTile.Tile;
        public IEffectHolder Holder;
        public IEnumerable<Effect> Effects => Tile.GetEffects<Effect>();

        public OnTile()
        {
        }

        public OnTile(MapTile tile, IEffectHolder holder)
        {
            MapTile = tile;
            Holder = holder;
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

        [Construct("on_tile")]
        public static OnTile Construct(Context context)
        {
            return new OnTile();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["tile"] = Serializer.GetHolderID(MapTile);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder(json["holder"], context);
            MapTile = Serializer.GetHolder<MapTile>(json["tile"], context);
        }

        public class Primary : OnTile
        {
            public Primary() : base()
            {

            }

            public Primary(MapTile tile, IEffectHolder holder) : base(tile, holder)
            {
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

            [Construct("on_tile_primary")]
            public static Primary Construct(Context context)
            {
                return new Primary();
            }
        }
    }
}
