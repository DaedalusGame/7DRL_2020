using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
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
        public MapTile[,] Tiles;
        public MapTile Outside;
        public IEnumerable<Creature> Creatures => World.Entities.Where(creature => creature.Map == this);
        public IEnumerable<Cloud> Clouds => World.GameObjects.OfType<Cloud>().Where(cloud => cloud.Map == this);

        public LevelFeelingSet Feelings = new LevelFeelingSet();

        public Map(SceneGame world, int width, int height)
        {
            World = world;
            SetSize(width, height);
            Init((x, y) => new FloorCave());
            Outside = new MapTile.FakeOutside(this, -1, -1);
        }

        private void SetSize(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new MapTile[Width, Height];
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

        public T GetCloud<T>() where T : Cloud
        {
            return (T)Clouds.FirstOrDefault(x => !x.Destroyed && x.GetType() == typeof(T));
        }

        public T AddCloud<T>(Func<Map, T> constructor) where T : Cloud
        {
            T cloud = GetCloud<T>();
            if(cloud == null)
            {
                cloud = constructor(this);
            }
            return cloud;
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

        public JToken WriteJson()
        {
            Context context = new Context(this);
            List<IEffectHolder> effectHolders = new List<IEffectHolder>();

            JObject json = new JObject();
            JArray glowing = new JArray();
            JArray tilesAbove = new JArray();
            JArray tilesUnder = new JArray();
            json["width"] = Width;
            json["height"] = Height;
            for (int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    var tile = Tiles[x, y];
                    glowing.Add(new JValue(tile.Glowing));
                    tilesAbove.Add(tile.Tile?.WriteJson(context));
                    tilesUnder.Add(tile.UnderTile?.WriteJson(context));
                    effectHolders.Add(tile);
                    if(tile.Tile != null)
                        effectHolders.Add(tile.Tile);
                    if(tile.UnderTile != null)
                        effectHolders.Add(tile.UnderTile);
                }
            }
            json["glowMap"] = glowing;
            json["tileMap"] = tilesAbove;
            json["tileMapUnder"] = tilesUnder;

            JArray effectsArray = new JArray();
            JArray entitiesArray = new JArray();

            var gameObjects = World.GameObjects.OfType<IJsonSerializable>().Where(obj => obj.Map == this);
            effectHolders.AddRange(gameObjects.OfType<IEffectHolder>());
            foreach(var entity in gameObjects)
            {
                entitiesArray.Add(entity.WriteJson(context));
            }
           
            var effects = effectHolders.SelectMany(holder => EffectManager.GetEffects<Effect>(holder, false)).Distinct().ToList();
            foreach(var effect in effects)
            {
                if(!effect.Innate)
                    effectsArray.Add(effect.WriteJson());
            }

            json["entities"] = entitiesArray;
            json["effects"] = effectsArray;
            return json;
        }

        public void ReadJson(JToken json)
        {
            Context context = new Context(this);

            var width = json["width"].Value<int>();
            var height = json["height"].Value<int>();

            JArray glowing = json["glowMap"] as JArray;
            JArray tilesAbove = json["tileMap"] as JArray;
            JArray tilesUnder = json["tileMapUnder"] as JArray;

            SetSize(width, height);
            var enumeratorGlowing = glowing.GetEnumerator();
            var enumeratorTiles = tilesAbove.GetEnumerator();
            var enumeratorTilesUnder = tilesUnder.GetEnumerator();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    enumeratorGlowing.MoveNext();
                    enumeratorTiles.MoveNext();
                    enumeratorTilesUnder.MoveNext();
                    var glowJson = enumeratorGlowing.Current;
                    var tileJson = enumeratorTiles.Current;
                    var tileUnderJson = enumeratorTilesUnder.Current;

                    var mapTile = Tiles[x, y] = new MapTile(this, x, y);
                    mapTile.Glowing = glowJson.Value<bool>();
                    mapTile.Tile = context.CreateTile(tileJson);
                    mapTile.UnderTile = context.CreateTile(tileUnderJson);
                }
            }

            JArray entities = json["entities"] as JArray;
            JArray effects = json["effects"] as JArray;

            foreach (var entityJson in entities)
            {
                context.CreateEntity(entityJson);
            } 

            foreach (var effectJson in effects)
            {
                var effect = context.CreateEffect(effectJson);
                if(effect != null)
                {
                    Effect.Apply(effect);
                }
            }
        }

        private string GetID(JToken json)
        {
            string id = null;
            if (json is JValue)
                id = json.Value<string>();
            else if (json is JObject)
                id = json["id"].Value<string>();

            return id;
        }
    }
}
