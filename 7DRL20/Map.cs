using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class LevelFeelingSet
    {
        public Dictionary<LevelFeeling, double> Feelings = new Dictionary<LevelFeeling, double>();

        public double this[LevelFeeling feeling]
        {
            get
            {
                return Feelings.GetOrDefault(feeling, 0);
            }
            set
            {
                Feelings[feeling] = value;
            }
        }

        public LevelFeelingSet()
        {

        }

        public LevelFeelingSet(IDictionary<LevelFeeling, double> feelings)
        {
            foreach (var pair in feelings)
                Feelings.Add(pair.Key, pair.Value);
        }

        public void Set(LevelFeeling feeling, double n)
        {
            Feelings[feeling] = n;
        }

        public void Add(LevelFeeling feeling, double n)
        {
            Feelings[feeling] = Feelings.GetOrDefault(feeling, 0) + n;
        }

        public void Multiply(LevelFeeling feeling, double n)
        {
            Feelings[feeling] = Feelings.GetOrDefault(feeling, 0) * n;
        }

        public LevelFeelingSet Copy()
        {
            return new LevelFeelingSet(Feelings);
        }
    }

    class LevelFeeling
    {
        public static List<LevelFeeling> AllFeelings = new List<LevelFeeling>();

        int ID;
        string Name;

        public LevelFeeling(string name)
        {
            ID = AllFeelings.Count;
            Name = name;
            AllFeelings.Add(this);
        }

        public static LevelFeeling Difficulty = new LevelFeeling("Difficulty");
        public static LevelFeeling Acid = new LevelFeeling("Acid");
        public static LevelFeeling Fire = new LevelFeeling("Fire");
        public static LevelFeeling Hell = new LevelFeeling("Hell");

        public override string ToString()
        {
            return Name;
        }
    }

    class Map
    {
        public SceneGame World;
        public int Width;
        public int Height;
        MapTile[,] Tiles;
        public MapTile Outside;

        public LevelFeelingSet Feelings = new LevelFeelingSet();

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
            for (int dx = MathHelper.Clamp(rectangle.Left - radius, 0, Width - 1); dx <= MathHelper.Clamp(rectangle.Right - 1 + radius, 0, Width - 1); dx++)
            {
                for (int dy = MathHelper.Clamp(rectangle.Top - radius, 0, Height - 1); dy <= MathHelper.Clamp(rectangle.Bottom - 1 + radius, 0, Height - 1); dy++)
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
