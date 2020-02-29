using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Map
    {
        public int Width;
        public int Height;
        MapTile[,] Tiles;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new MapTile[width, height];
            Init((x, y) => new FloorCave());
        }

        public void Init(Func<int,int,Tile> generator)
        {
            Tiles = new MapTile[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tiles[x, y] = new MapTile(this, x, y, generator(x,y));
                }
            }
        }

        public Tile GetTile(int x, int y)
        {
            if (InBounds(x, y))
                return Tiles[x, y].Tile;
            else
                return null;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }
    }
}
