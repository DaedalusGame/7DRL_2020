using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class MapTile : IEffectHolder
    {
        public ReusableID ObjectID
        {
            get;
            set;
        }

        public Map Map;
        public int X, Y;

        public Tile Tile;

        public Tile UnderTile;

        public MapTile(Map map, int x, int y, Tile tile)
        {
            ObjectID = EffectManager.NewID();
            Map = map;
            X = x;
            Y = y;
            Set(tile);
        }

        public void Set(Tile tile)
        {
            Tile = tile;
            Tile.SetParent(this);
        }

        public void SaveUnder()
        {
            UnderTile = Tile;
        }

        public void RestoreUnder()
        {
            Tile = UnderTile;
        }

        public override string ToString()
        {
            return $"{Tile} ({X},{Y})";
        }
    }
}
