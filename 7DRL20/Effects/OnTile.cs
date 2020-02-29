using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class OnTile : Effect
    {
        MapTile MapTile;
        public Tile Tile => MapTile.Tile;
        public IEffectHolder Holder;
        public IEnumerable<Effect> Effects => Tile.GetEffects<Effect>();

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

        public class Primary : OnTile
        {
            public Primary(MapTile tile, IEffectHolder holder) : base(tile, holder)
            {
            }

            public override string ToString()
            {
                return $"On tile {MapTile} (Primary)";
            }
        }
    }
}
