using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Map
    {
        public SceneGame World;
        public int Width;
        public int Height;
        MapTile[,] Tiles;
        public MapTile Outside;

        public Map(SceneGame world, int width, int height)
        {
            World = world;
            Width = width;
            Height = height;
            Tiles = new MapTile[width, height];
            Init((x, y) => new FloorCave());
            Outside = new MapTile.FakeOutside(this, -1, -1);
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
            if (InMap(x, y))
                return Tiles[x, y].Tile;
            else
                return new Tile.FakeOutside(this, x, y);
        }

        public IEnumerable<Tile> GetNearby(int x, int y, int radius)
        {
            for (int dx = MathHelper.Clamp(x - radius, 0, Width - 1); dx <= MathHelper.Clamp(x + radius, 0, Width - 1); dx++)
            {
                for (int dy = MathHelper.Clamp(y - radius, 0, Height - 1); dy <= MathHelper.Clamp(y + radius, 0, Height - 1); dy++)
                {
                    yield return GetTile(dx, dy);
                }
            }
        }

        public IEnumerable<Tile> GetNearby(Rectangle rectangle, int radius)
        {
            for (int dx = MathHelper.Clamp(rectangle.Left - radius, 0, Width - 1); dx <= MathHelper.Clamp(rectangle.Right + radius, 0, Width - 1); dx++)
            {
                for (int dy = MathHelper.Clamp(rectangle.Top - radius, 0, Height - 1); dy <= MathHelper.Clamp(rectangle.Bottom + radius, 0, Height - 1); dy++)
                {
                    yield return GetTile(dx, dy);
                }
            }
        }

        public bool InMap(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 1 && y >= 1 && x < Width-1 && y < Height-1;
        }
    }
}
