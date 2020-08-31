using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
using RoguelikeEngine.MapGeneration;
using RoguelikeEngine.Traits;

namespace RoguelikeEngine
{
    interface IMineable
    {
        double Durability
        {
            get;
        }
        double Damage
        {
            get;
            set;
        }

        Wait Mine(MineEvent mine);

        void Destroy();
    }

    abstract class Tile : IEffectHolder, IHasPosition, IDrawable
    {
        protected static Random Random = new Random();

        public class FakeOutside : Tile //Subtype that handles the tiles outside the map.
        {
            Map _Map;
            int _X;
            int _Y;

            public override ReusableID ObjectID
            {
                get
                {
                    return ReusableID.Null;
                }
                set
                {
                    //NOOP
                }
            }

            public override SceneGame World => _Map.World;
            public override Map Map => _Map;
            public override int X => _X;
            public override int Y => _Y;
            public override Tile Under => null;

            public FakeOutside(Map map, int x, int y) : base()
            {
                _Map = map;
                _X = x;
                _Y = y;
                Parent = map.Outside;
                Opaque = true;
                Solid = true;
            }

            public override void Draw(SceneGame scene, DrawPass drawPass)
            {
                //NOOP
            }
        }

        public static TileColor HiddenColor = new TileColor(Color.Black, Color.Black);

        public MapTile Parent;
        public virtual ReusableID ObjectID
        {
            get;
            set;
        }
        public virtual SceneGame World => Parent.World;
        public virtual Map Map => Parent.Map;
        public virtual int X => Parent.X;
        public virtual int Y => Parent.Y;
        public Vector2 VisualPosition => new Vector2(X*16,Y*16);
        public Vector2 VisualTarget => VisualPosition + new Vector2(8, 8);
        public virtual Tile Under => Parent.UnderTile;
        public bool Orphaned => false;
        public double DrawOrder => Y;

        public Func<Color> VisualUnderColor = () => Color.TransparentBlack;

        public string Name;
        public virtual double Durability => double.PositiveInfinity;
        public double Damage {
            get;
            set;
        }

        public bool Opaque;
        public bool Solid;

        public GeneratorGroup Group
        {
            get
            {
                return Parent.Group;
            }
            set
            {
                Parent.Group = value;
            }
        }
        public bool Glowing
        {
            get
            {
                return Parent.Glowing;
            }
            set
            {
                Parent.Glowing = value;
            }
        }

        public virtual Tile NewTile => Parent.Tile;
        public virtual IEnumerable<IEffectHolder> Contents => Parent.GetEffects<Effects.OnTile>().Select(x => x.Holder);
        public IEnumerable<Creature> Creatures => Contents.OfType<Creature>();
        public IEnumerable<Item> Items => Contents.OfType<Item>();
        public IEnumerable<Cloud> Clouds => Contents.OfType<Cloud>();

        List<Effect> TileEffects = new List<Effect>();
        
        private Tile()
        {

        }

        public Tile(string name)
        {
            ObjectID = EffectManager.SetID(this);
            Name = name;
        }

        public bool IsVisible()
        {
            return !Opaque || (Map.InBounds(X, Y) && GetAllNeighbors().Where(p => Map.InMap(p.X, p.Y)).Any(neighbor => !neighbor.Opaque));
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public void AddTileEffect(Effect effect)
        {
            effect.Apply();
            TileEffects.Add(effect);
        }

        public IEnumerable<Effect> GetTileEffects()
        {
            return TileEffects.GetAndClean(effect => effect.Removed);
        }

        public void Replace(Tile newTile)
        {
            Parent.Set(newTile);
        }

        public void PlaceOn(Tile newTile)
        {
            Parent.SaveUnder();
            Parent.Set(newTile);
        }

        public void Scrape()
        {
            Parent.RestoreUnder();
        }

        public void MakeFloor()
        {
            if (Parent.UnderTile != null && !Parent.UnderTile.Solid)
                Scrape();
            else
                Replace(new FloorCave());
        }

        public void SetParent(MapTile parent)
        {
            Parent = parent;
        }

        public Tile GetNeighbor(int dx, int dy)
        {
            return Map.GetTile(X + dx, Y + dy);
        }

        public IEnumerable<Tile> GetAdjacentNeighbors()
        {
            return new[] { GetNeighbor(1, 0), GetNeighbor(0, 1), GetNeighbor(-1, 0), GetNeighbor(0, -1) };
        }

        public IEnumerable<Tile> GetAllNeighbors()
        {
            return new[] { GetNeighbor(1, 0), GetNeighbor(0, 1), GetNeighbor(-1, 0), GetNeighbor(0, -1), GetNeighbor(1, 1), GetNeighbor(-1, 1), GetNeighbor(-1, -1), GetNeighbor(1, -1) };
        }

        public IEnumerable<Tile> GetNearby(int radius)
        {
            return Map.GetNearby(X, Y, radius);
        }

        public IEnumerable<Tile> GetNearby(Rectangle rectangle, int radius)
        {
            return Map.GetNearby(rectangle, radius);
        }

        public void AddPrimary(IEffectHolder holder)
        {
            Effect.Apply(new Effects.OnTile.Primary(Parent, holder));
        }

        public void Add(IEffectHolder holder)
        {
            Effect.Apply(new Effects.OnTile(Parent, holder));
        }

        public static void ConnectStair(Tile down, Tile up)
        {
            StairDown downStair = new StairDown();
            StairUp upStair = new StairUp();
            down.Replace(downStair);
            up.Replace(upStair);
            upStair.SetTarget(downStair);
        }

        protected Color GetUnderColor(SceneGame scene)
        {
            Color glow = Group.GlowColor.GetColor(scene.Frame);
            Color underColor = VisualUnderColor();
            if (IsVisible() && Glowing)
                return new Color(glow.R + underColor.R, glow.G + underColor.G, glow.B + underColor.B, glow.A + underColor.A);
            else
                return underColor;
        }

        public virtual void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            foreach (Item item in Items)
            {
                item.AddActions(ui, player, selection);
            }
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            if (Clouds.Any())
                tooltip += "\n";
            int cloudCount = 0;
            foreach (Cloud cloud in Clouds.Take(10 + 1))
            {
                if (cloudCount >= 10)
                    tooltip += "...\n";
                else
                    cloud.AddTooltip(ref tooltip);
                cloudCount++;
            }
            if (Creatures.Any())
                tooltip += "\n";
            int creatureCount = 0;
            foreach (Creature creature in Creatures.Take(10+1))
            {
                if (creatureCount >= 10)
                    tooltip += "...\n";
                else
                    creature.AddTooltip(ref tooltip);
                creatureCount++;
            }
            if (Items.Any())
                tooltip += "\n";
            int itemCount = 0;
            foreach (Item item in Items.Take(10+1))
            {
                if (itemCount >= 10)
                    tooltip += "...\n";
                else
                    item.AddTooltip(ref tooltip);
                itemCount++;
            }
        }

        protected bool Connects(ConnectivityHelper a, ConnectivityHelper b)
        {
            return a != null && b != null;
        }

        public virtual IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Tile;
        }

        public abstract void Draw(SceneGame scene, DrawPass drawPass);

        protected void DrawUnderTile(SceneGame scene, DrawPass drawPass)
        {
            if (Under != null)
            {
                Under.VisualUnderColor = VisualUnderColor;
                Under.Draw(scene, drawPass);
            }
        }

        protected void DrawFloor(SceneGame scene, SpriteReference baseSprite, SpriteReference layerSprite, int ox, int oy, TileColor color)
        {
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.SpriteBatch.Draw(baseSprite.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(ox, oy, 16, 16), color.Background);
            scene.SpriteBatch.Draw(layerSprite.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(ox, oy, 16, 16), color.Foreground);
        }

        protected void DrawBrick(SceneGame scene, int ox, int oy, TileColor color)
        {
            var brick0 = SpriteLoader.Instance.AddSprite("content/brick_base");
            var brick1 = SpriteLoader.Instance.AddSprite("content/brick_layer");
            DrawFloor(scene, brick0, brick1, ox, oy, color);
        }

        protected void DrawCave(SceneGame scene, int ox, int oy, TileColor color)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");
            DrawFloor(scene, cave0, cave1, ox, oy, color);
        }

        protected void DrawBigTile(SceneGame scene, int ox, int oy, TileColor color)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/bigtile_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/bigtile_layer");
            DrawFloor(scene, cave0, cave1, ox, oy, color);
        }

        protected void DrawDanceFloor(SceneGame scene, int ox, int oy, TileColor color)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/dancefloor_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/dancefloor_layer");
            DrawFloor(scene, cave0, cave1, ox, oy, color);
        }

        protected void DrawPlankFloor(SceneGame scene, int ox, int oy, TileColor color)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/planks_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/planks_layer");
            DrawFloor(scene, cave0, cave1, ox, oy, color);
        }

        protected void DrawConnected(SceneGame scene, SpriteReference sprite, Connectivity connectivity, Color color)
        {
            int blobIndex = connectivity.GetBlobTile();
            int ix = blobIndex % 7;
            int iy = blobIndex / 7;
            scene.SpriteBatch.Draw(sprite.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(ix * 16, iy * 16, 16, 16), color);
        }

        protected void DrawNoise(SceneGame scene, Connectivity connectivity, Vector2 noiseOffset, float distance)
        {
            var noise = SpriteLoader.Instance.AddSprite("content/noise");
            var edge = SpriteLoader.Instance.AddSprite("content/connected_edge");
            scene.PushSpriteBatch(blendState: Microsoft.Xna.Framework.Graphics.BlendState.Additive);
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Color(0, distance, 0));
            scene.SpriteBatch.Draw(noise.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(16 * Parent.X + (int)noiseOffset.X, 16 * Parent.Y + (int)noiseOffset.Y, 16, 16), Color.Red);
            scene.PopSpriteBatch();
            DrawConnected(scene, edge, connectivity, Color.White);
        }

        protected void DrawCracks(SceneGame scene, double slide, Color color)
        {
            if (slide <= 0)
                return;
            var cracks = SpriteLoader.Instance.AddSprite("content/cracks");
            int frame = Math.Min((int)(slide * (cracks.SubImageCount)), cracks.SubImageCount-1);
            scene.DrawSprite(cracks, frame, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color, 0);
        }

        public virtual JToken WriteJson(Context context)
        {
            return Serializer.GetID(this);
        }

        public virtual void ReadJson(JToken token, Context context)
        {
            //NOOP
        }
    }

    struct TileColor
    {
        static ColorMatrix FloorBackground = ColorMatrix.Scale(0.25f) * ColorMatrix.Saturate(0.5f);
        static ColorMatrix FloorForeground = ColorMatrix.Scale(0.35f) * ColorMatrix.Saturate(0.5f);
        static ColorMatrix SeaFloor = ColorMatrix.Scale(0.25f) * ColorMatrix.Saturate(0.25f);

        public Color Background;
        public Color Foreground;

        public TileColor(JToken json)
        {
            Background = Color.Black;
            Foreground = Color.White;
            ReadJson(json);
        }

        public TileColor(Color background, Color foreground)
        {
            Background = background;
            Foreground = foreground;
        }

        public TileColor ToFloor()
        {
            return new TileColor(FloorBackground.Transform(Background), FloorForeground.Transform(Foreground));
        }

        public TileColor ToSeaFloor()
        {
            return new TileColor(SeaFloor.Transform(Background), SeaFloor.Transform(Foreground));
        }

        public JToken WriteJson()
        {
            JObject json = new JObject();
            json["foreground"] = Util.WriteColor(Foreground);
            json["background"] = Util.WriteColor(Background);
            return json;
        }

        public void ReadJson(JToken json)
        {
            Foreground = Util.ReadColor(json["foreground"]);
            Background = Util.ReadColor(json["background"]);
        }
    }

    class StairType
    {
        public static List<StairType> AllStairTypes = new List<StairType>();

        public int Index;
        public string ID;
        public string Name;
        Func<Stair, GeneratorTemplate> Function;

        public StairType(string id, string name, Func<Stair, GeneratorTemplate> function)
        {
            Index = AllStairTypes.Count;
            ID = id;
            Name = name;
            Function = function;
            AllStairTypes.Add(this);
        }

        public GeneratorTemplate Generate(Stair stair)
        {
            return Function(stair);
        }

        public static StairType GetStairType(string id)
        {
            return AllStairTypes.Find(x => x.ID == id);
        }

        public static StairType RandomStart = new StairType("start_random", "Start", stair => new TemplateRandomLevel(new GroupRandom(), 0));
        public static StairType Random = new StairType("random", "Random", stair => new TemplateRandomLevel(new GroupRandom(stair.Group.MakeTemplate()), stair.Seed));
    }

    class StairBonus
    {
        public static List<StairBonus> AllStairBonuses = new List<StairBonus>();

        public int Index;
        public string ID;
        public string Name;
        Action<GeneratorTemplate> Function;

        public StairBonus(string id, string name, Action<GeneratorTemplate> function)
        {
            Index = AllStairBonuses.Count;
            ID = id;
            Name = name;
            Function = function;
            AllStairBonuses.Add(this);
        }

        public static StairBonus NoBonus = new StairBonus("no_bonus", "No Bonus", feelings => { });

        public static StairBonus Difficult = new StairBonus("difficult", "Difficult Level", template => { template.Feelings.Add(LevelFeeling.Difficulty, +30); });
        public static StairBonus Easy = new StairBonus("easy", "Easy Level", template => { template.Feelings.Add(LevelFeeling.Difficulty, -30); });

        public static StairBonus Hell = new StairBonus("hell", "Hellish Environment", template => {
            template.Feelings.Add(LevelFeeling.Fire, +30);
            template.Feelings.Add(LevelFeeling.Hell, +50);
            template.Feelings.Add(LevelFeeling.Difficulty, +10);
        });

        public static StairBonus Dungeon = new StairBonus("dungeon", "Dungeon", template => {
            if (template is TemplateRandomLevel level)
                level.GroupGenerator = new GroupSet(level.GroupGenerator.Groups.Concat(new[] { GroupGenerator.Dungeon }));
        });
        public static StairBonus SeaOfDirac = new StairBonus("sea_of_dirac", "Sea of Dirac", template => {
            if (template is TemplateRandomLevel level)
                level.GroupGenerator = new GroupSet(level.GroupGenerator.Groups.Concat(new[] { GroupGenerator.SeaOfDirac }));
        });

        public void Apply(GeneratorTemplate template)
        {
            Function(template);
        }

        public static StairBonus GetStairBonus(string id)
        {
            return AllStairBonuses.Find(x => x.ID == id);
        }
    }

    abstract class Stair : Tile
    {
        public int Seed;
        public StairType Type;
        RemoteTile TargetTile;
        public List<StairBonus> Bonuses = new List<StairBonus>();

        public Tile Target
        {
            get
            {
                if (TargetTile != null)
                    return TargetTile.Tile;
                else
                    return null;
            }
            set
            {
                TargetTile = new RemoteTile(value.World, value.Map.ID, value.X, value.Y);
            }
        }

        public Stair() : base("Stairs")
        {
        }

        public void InitBonuses()
        {
            List<StairBonus> validBonuses = new List<StairBonus>(StairBonus.AllStairBonuses);
            int amount = Random.Next(3) + 2;
            for(int i = 0; i < amount && validBonuses.Any(); i++)
            {
                Bonuses.Add(validBonuses.PickAndRemove(Random));
            }
            Bonuses.Sort((x, y) => x.Index.CompareTo(y.Index));
        }

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            base.AddActions(ui, player, selection);
            if (player.Tiles.Contains(this))
            {
                if (Bonuses.Any())
                {
                    foreach (var bonus in Bonuses)
                    {
                        selection.Add(new ActAction($"Stairs ({bonus.Name})", "", () =>
                        {
                            ui.TakeAction(Scheduler.Instance.RunAndWait(RoutineTakeStairs(player, bonus)), true);
                            selection.Close();
                            Bonuses.Clear();
                        }, () => CanUseStairs(player)));
                    }
                }
                else
                {
                    selection.Add(new ActAction("Take the Stairs", "", () =>
                    {
                        ui.TakeAction(Scheduler.Instance.RunAndWait(RoutineTakeStairs(player, StairBonus.NoBonus)), true);
                        selection.Close();
                    }, () => CanUseStairs(player)));
                }
            }
        }

        private IEnumerable<Wait> RoutineTakeStairs(Creature player, StairBonus bonus)
        {
            var fadeOut = new ScreenFade(player.World, () => ColorMatrix.Tint(Color.Black), LerpHelper.Linear, false, 20);
            yield return new WaitTime(20);
            if (Target == null && Type != null)
            {
                var template = Type.Generate(this);
                template.SetFeelings(Map.Feelings);
                bonus.Apply(template);
                template.Build(World);
                var stair = template.BuildStairRoom(Group.GetType());
                BuildTarget(stair);
            }
            player.MoveTo(Target, 0);
            player.World.CameraMap = player.Map;
            fadeOut.Destroy();
            var fadeIn = new ScreenFade(player.World, () => ColorMatrix.Tint(Color.Black), LerpHelper.Invert(LerpHelper.Linear), true, 20);
            yield return new WaitTime(20);
        }

        public void SetTarget(Stair other)
        {
            Target = other;
            other.Target = this;
        }

        public abstract void BuildTarget(Tile target);

        private bool CanUseStairs(Creature player)
        {
            if (Target is Stair)
                return true;
            if (Type != null)
                return true;
            return false;
        }

        public override JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["seed"] = Seed;
            if (Type != null)
                json["type"] = Type.ID;
            if (TargetTile != null)
                json["target"] = TargetTile.WriteJson(context);
            JArray bonusArray = new JArray();
            foreach(var bonus in Bonuses)
            {
                bonusArray.Add(bonus.ID);
            }
            json["bonuses"] = bonusArray;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Seed = json["seed"].Value<int>();
            if (json.HasKey("type"))
                Type = StairType.GetStairType(json["type"].Value<string>());
            if (json.HasKey("target"))
                TargetTile = new RemoteTile(json["target"], context);
            var bonusArray = json["bonuses"];
            foreach(var bonusJson in bonusArray)
            {
                Bonuses.Add(StairBonus.GetStairBonus(bonusJson.Value<string>()));
            }
        }
    }

    [SerializeInfo("stair_up")]
    class StairUp : Stair
    {
        private bool StairsHidden => HasContent(this) || HasContent(GetNeighbor(0, -1));

        public StairUp() : base()
        {
        }

        [Construct]
        public static StairUp Construct(Context context)
        {
            return new StairUp();
        }

        private bool HasContent(Tile tile)
        {
            return tile != null && tile.Contents.Any();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var hidden0 = SpriteLoader.Instance.AddSprite("content/upstairs_base");
            var hidden1 = SpriteLoader.Instance.AddSprite("content/upstairs_layer");
            var stairs0 = SpriteLoader.Instance.AddSprite("content/stairs_base");
            var stairs1 = SpriteLoader.Instance.AddSprite("content/stairs_layer");

            var hidden = StairsHidden;
            var color = Group.BrickColor;

            if (hidden)
                color = Group.CaveColor.ToFloor();
            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            if (StairsHidden)
            {
                scene.DrawSprite(hidden0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
                scene.DrawSprite(hidden1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            }
            else
            {
                scene.DrawSprite(stairs0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y - 16), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
                scene.DrawSprite(stairs1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y - 16), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            }
        }

        public override void BuildTarget(Tile target)
        {
            var stairDown = new StairDown();
            target.Replace(stairDown);
            SetTarget(stairDown);
        }
    }

    [SerializeInfo("stair_down")]
    class StairDown : Stair
    {
        public StairDown() : base()
        {
        }

        [Construct]
        public static StairDown Construct(Context context)
        {
            return new StairDown();
        }

        private bool HasContent(Tile tile)
        {
            return tile != null && tile.Contents.Any();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var stairs0 = SpriteLoader.Instance.AddSprite("content/downstair_base");
            var stairs1 = SpriteLoader.Instance.AddSprite("content/downstair_layer");

            var color = Group.BrickColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.DrawSprite(stairs0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(stairs1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
        }

        public override void BuildTarget(Tile target)
        {
            var stairUp = new StairUp();
            target.Replace(stairUp);
            SetTarget(stairUp);
        }
    }

    [SerializeInfo("floor_cave")]
    class FloorCave : Tile
    {
        public FloorCave() : base("Cave Floor")
        {
        }

        [Construct]
        public static FloorCave Construct(Context context)
        {
            return new FloorCave();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.CaveColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            DrawCave(scene, 0, 0, color);
        }
    }

    [SerializeInfo("floor_tiles")]
    class FloorTiles : Tile
    {
        public FloorTiles() : base("Tiled Floor")
        {
        }

        [Construct]
        public static FloorTiles Construct(Context context)
        {
            return new FloorTiles();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.CaveColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            DrawDanceFloor(scene, 0, 0, color);
        }
    }

    [SerializeInfo("floor_big_tile")]
    class FloorBigTile : Tile
    {
        public FloorBigTile() : base("Tiled Floor")
        {
        }

        [Construct]
        public static FloorBigTile Construct(Context context)
        {
            return new FloorBigTile();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.CaveColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            DrawBigTile(scene, 0, 0, color);
        }
    }

    [SerializeInfo("floor_bridge")]
    class FloorBridge : Tile
    {
        public ConnectivityHelper Connectivity;

        public FloorBridge() : base("Bridge")
        {
            Connectivity = new ConnectivityHelper(this, GetConnection, Connects);
        }

        [Construct]
        public static FloorBridge Construct(Context context)
        {
            return new FloorBridge();
        }

        private ConnectivityHelper GetConnection(Tile tile)
        {
            if (tile is FloorBridge bridge)
                return bridge.Connectivity;
            return null;
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            Connectivity.CalculateIfNeeded();

            var cave0 = SpriteLoader.Instance.AddSprite("content/connected_bridge_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/connected_bridge_layer");

            var color = Group.WoodColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            //scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            DrawConnected(scene, cave0, Connectivity.Connectivity, color.Background);
            DrawConnected(scene, cave1, Connectivity.Connectivity, color.Foreground);
        }
    }

    [SerializeInfo("floor_carpet")]
    class FloorCarpet : Tile
    {
        public TileColor Color = new TileColor(new Color(85, 107, 168), new Color(198, 190, 55));
        public Connectivity Connectivity;

        public FloorCarpet() : base("Carpet")
        {
        }

        [Construct]
        public static FloorCarpet Construct(Context context)
        {
            return new FloorCarpet();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/carpet_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/connected_carpet_layer");

            var color = Color.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            //scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.SpriteBatch.Draw(cave0.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(0, 0, 16, 16), color.Background);
            DrawConnected(scene, cave1, Connectivity, color.Foreground);
        }

        public override JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["connect"] = new JValue(Connectivity);
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Connectivity = (Connectivity)json["connect"].Value<int>();
        }
    }

    [SerializeInfo("floor_plank")]
    class FloorPlank : Tile
    {
        public FloorPlank() : base("Plank Floor")
        {
        }

        [Construct]
        public static FloorPlank Construct(Context context)
        {
            return new FloorPlank();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.WoodColor.ToFloor();

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            DrawPlankFloor(scene, 0, 0, color);
        }
    }

    [SerializeInfo("wall_cave")]
    class WallCave : Tile, IMineable
    {
        public override double Durability => 100;

        public WallCave() : base("Cave Wall")
        {
            Solid = true;
            Opaque = true;
        }

        [Construct]
        public static WallCave Construct(Context context)
        {
            return new WallCave();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            var color = Group.CaveColor;
            Color glow = Group.GlowColor.GetColor(scene.Frame);

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            DrawCracks(scene, Damage / Durability, new Color(0, 0, 0, 160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.25, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            //NOOP
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("wall_plank")]
    class WallPlank : Tile, IMineable
    {
        public override double Durability => 200;

        public WallPlank() : base("Plank Wall")
        {
            Solid = true;
            Opaque = true;
        }

        [Construct]
        public static WallPlank Construct(Context context)
        {
            return new WallPlank();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/planks_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/planks_layer");

            var color = Group.CaveColor;
            Color glow = Group.GlowColor.GetColor(scene.Frame);

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            DrawCracks(scene, Damage / Durability, new Color(0, 0, 0, 160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.25, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            //NOOP
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("wall_ore")]
    class WallOre : Tile, IMineable
    {
        public override double Durability => (Under?.Durability ?? 0) + 50;

        int Frame = Random.Next(1000);
        Material Material;

        public WallOre() : base("Ore Vein") //TODO: fix the name
        {
            Solid = true;
            Opaque = true;
        }

        public WallOre(Material material) : this()
        {
            Material = material;
        }

        [Construct]
        public static WallOre Construct(Context context)
        {
            return new WallOre();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            DrawUnderTile(scene, drawPass);
            if (!IsVisible())
                return;
            var ore = SpriteLoader.Instance.AddSprite("content/ore");

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Material.ColorTransform, matrix, projection);
            });
            scene.DrawSprite(ore, Frame, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);
            scene.PopSpriteBatch();
            DrawCracks(scene, Damage / Durability, new Color(0,0,0,160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.1, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Scrape();
        }

        public override JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["material"] = Material.ID;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Material = Material.GetMaterial(json["material"].Value<string>());
        }
    }

    [SerializeInfo("wall_obsidiorite")]
    class WallObsidiorite : Tile, IMineable
    {
        public override double Durability => 2000;

        public WallObsidiorite() : base("Obsidiorite")
        {
            Solid = true;
            Opaque = true;
        }

        [Construct]
        public static WallObsidiorite Construct(Context context)
        {
            return new WallObsidiorite();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = new TileColor(new Color(69 / 2, 54 / 2, 75 / 2), new Color(157, 143, 167));
            Color glow = Color.TransparentBlack;

            if (!IsVisible())
            {
                color = HiddenColor;
                glow = Color.TransparentBlack;
            }

            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            glow = Util.AddColor(glow, VisualUnderColor());
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), glow);
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            DrawCracks(scene, Damage / Durability, new Color(0, 0, 0, 160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 2, 0.02, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material.Obsidiorite, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("wall_meteorite")]
    class WallMeteorite : Tile, IMineable
    {
        public override double Durability => 4500;

        public WallMeteorite() : base("Meteorite")
        {
            Solid = true;
            Opaque = true;
        }

        [Construct]
        public static WallMeteorite Construct(Context context)
        {
            return new WallMeteorite();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = new TileColor(new Color(69, 75, 54), new Color(157, 167, 143));
            Color glow = Color.Lerp(new Color(16, 4, 1), new Color(255, 64, 16), 0.5f + 0.5f * (float)Math.Sin(scene.Frame / 60f));

            if (!IsVisible())
            {
                color = HiddenColor;
                glow = Color.TransparentBlack;
            }

            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            glow = Util.AddColor(glow, VisualUnderColor());
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), glow);
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            DrawCracks(scene, Damage / Durability, new Color(0, 0, 0, 160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 2, 0.01, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material.Meteorite, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("wall_basalt")]
    class WallBasalt : Tile, IMineable
    {
        public override double Durability => 500;

        public WallBasalt() : base("Basalt")
        {
            Solid = true;
            Opaque = true;
        }

        [Construct]
        public static WallBasalt Construct(Context context)
        {
            return new WallBasalt();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = new TileColor(new Color(169, 169, 169), new Color(239, 236, 233));
            Color glow = new Color(128, 128, 128);

            if (!IsVisible())
            {
                color = HiddenColor;
                glow = Color.TransparentBlack;
            }

            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");

            glow = Util.AddColor(glow, VisualUnderColor());
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), glow);
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            DrawCracks(scene, Damage / Durability, new Color(0, 0, 0, 160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.1, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            new Ore(creature.World, Material.Basalt, 50).MoveTo(creature.Tile);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("wall_brick")]
    class WallBrick : Tile, IMineable
    {
        public override double Durability => 1000;

        public WallBrick() : base("Brick Wall")
        {
            Solid = true;
            Opaque = true;
        }

        [Construct]
        public static WallBrick Construct(Context context)
        {
            return new WallBrick();
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/brick_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/brick_layer");

            var color = Group.BrickColor;

            if (!IsVisible())
                color = HiddenColor;

            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), VisualUnderColor());
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Background, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, color.Foreground, 0);
            DrawCracks(scene, Damage / Durability, new Color(0, 0, 0, 160));
        }

        public Wait Mine(MineEvent mine)
        {
            mine.Setup(this, 1, 0.1, LootGenerator);
            return Scheduler.Instance.RunAndWait(mine.RoutineStart());
        }

        private void LootGenerator(Creature creature)
        {
            //NOOP
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("pool_water")]
    class Water : Tile
    {
        public ConnectivityHelper Connectivity;

        public Water() : base("Water")
        {
            Effect.ApplyInnate(new EffectTrait(this, Trait.Water));

            Connectivity = new ConnectivityHelper(this, GetConnection, Connects);
        }

        [Construct]
        public static Water Construct(Context context)
        {
            return new Water();
        }

        private ConnectivityHelper GetConnection(Tile tile)
        {
            if (tile is Water water)
                return water.Connectivity;
            return null;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.SeaDistort;
            yield return DrawPass.Sea;
            yield return DrawPass.SeaFloor;
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            if (drawPass == DrawPass.SeaDistort)
            {
                Connectivity.CalculateIfNeeded();
                DrawNoise(scene, Connectivity.Connectivity, new Vector2(-base.World.Frame * 0.2f, -base.World.Frame * 0.5f), 0.125f);
            }
            else if (drawPass == DrawPass.SeaFloor)
            {
                bool visible = IsVisible();
                var color = visible ? Group.CaveColor.ToSeaFloor() : HiddenColor;

                scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
                DrawCave(scene, 0, 6, color);
                if (Under is Coral coral && visible)
                    coral.DrawCoral(scene);
            }
            else if (drawPass == DrawPass.Sea)
            {
                var lava = SpriteLoader.Instance.AddSprite("content/water");
                scene.PushSpriteBatch(blendState: Microsoft.Xna.Framework.Graphics.BlendState.Additive);
                int wiggle = (int)(8 * Math.Sin(World.Frame * 0.01));
                scene.SpriteBatch.Draw(lava.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(16 * Parent.X + wiggle, 16 * Parent.Y, 16, 16), Color.White);
                scene.PopSpriteBatch();
            }
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("pool_water_shallow")]
    class WaterShallow : Water
    {
        public WaterShallow() : base()
        {
            Name = "Shallow Water";
        }

        [Construct]
        public static WaterShallow Construct(Context context)
        {
            return new WaterShallow();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.SeaDistort;
            yield return DrawPass.Sea;
            yield return DrawPass.SeaFloor;
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            if (drawPass == DrawPass.SeaDistort)
            {
                Connectivity.CalculateIfNeeded();
                DrawNoise(scene, Connectivity.Connectivity, new Vector2(-base.World.Frame * 0.2f, -base.World.Frame * 0.5f), 0.0f);
            }
            else if (drawPass == DrawPass.SeaFloor)
            {
                var color = Group.CaveColor.ToFloor();

                if (!IsVisible())
                    color = HiddenColor;

                scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), GetUnderColor(scene));
                DrawCave(scene, 0, 0, color);
            }
            else if (drawPass == DrawPass.Sea)
            {
                var lava = SpriteLoader.Instance.AddSprite("content/water");
                scene.PushSpriteBatch(blendState: Microsoft.Xna.Framework.Graphics.BlendState.Additive);
                int wiggle = (int)(8 * Math.Sin(World.Frame * 0.01));
                scene.SpriteBatch.Draw(lava.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(16 * Parent.X + wiggle, 16 * Parent.Y, 16, 16), Color.Gray);
                scene.PopSpriteBatch();
            }
        }
    }

    [SerializeInfo("pool_lava")]
    class Lava : Tile
    {
        public Lava() : base("Lava")
        {
            Effect.ApplyInnate(new EffectTrait(this, Trait.Lava));
        }

        [Construct]
        public static Lava Construct(Context context)
        {
            return new Lava();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.BrickColor;

            if (!IsVisible())
                color = HiddenColor;

            scene.DrawLava(new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), Color.White);
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("pool_lava_super")]
    class SuperLava : Tile
    {
        static ColorMatrix ColorMatrix = new ColorMatrix(new Matrix(
            1.2f, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1),
            new Vector4(0.2f, 0.2f, 0, 0));

        public SuperLava() : base("Super Lava")
        {
            Effect.ApplyInnate(new EffectTrait(this, Trait.SuperLava));
        }

        [Construct]
        public static SuperLava Construct(Context context)
        {
            return new SuperLava();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.BrickColor;

            if (!IsVisible())
                color = HiddenColor;

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(ColorMatrix, matrix, projection);
            });
            scene.DrawLava(new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), Color.White);
            scene.PopSpriteBatch();
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("pool_lava_hyper")]
    class HyperLava : Tile
    {
        static ColorMatrix ColorMatrix = new ColorMatrix(new Matrix(
            1.3f, 0, 0, 0,
            0, 1.3f, 0, 0,
            0, 0, 1.3f, 0,
            0, 0, 0, 1),
            new Vector4(0, 0, 0.4f, 0));

        public HyperLava() : base("Hyper Lava")
        {
            Effect.ApplyInnate(new EffectTrait(this, Trait.HyperLava));
        }

        [Construct]
        public static HyperLava Construct(Context context)
        {
            return new HyperLava();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.BrickColor;

            if (!IsVisible())
                color = HiddenColor;

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(ColorMatrix, matrix, projection);
            });
            scene.DrawLava(new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), Color.White);
            scene.PopSpriteBatch();
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("pool_lava_dark")]
    class DarkLava : Tile
    {
        static ColorMatrix ColorMatrix = ColorMatrix.Identity;

        ConnectivityHelper Connectivity;

        public DarkLava() : base("Dark Lava")
        {
            Connectivity = new ConnectivityHelper(this, GetConnection, Connects);
        }

        [Construct]
        public static DarkLava Construct(Context context)
        {
            return new DarkLava();
        }

        private ConnectivityHelper GetConnection(Tile tile)
        {
            if (tile is DarkLava lava)
                return lava.Connectivity;
            return null;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.SeaDistort;
            yield return DrawPass.Sea;
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.BrickColor;

            if (drawPass == DrawPass.SeaDistort)
            {
                Connectivity.CalculateIfNeeded();
                DrawNoise(scene, Connectivity.Connectivity, new Vector2(-base.World.Frame * 0.2f, -base.World.Frame * 0.5f), 0.25f);
            }
            if (drawPass == DrawPass.Sea)
            {
                var lava = SpriteLoader.Instance.AddSprite("content/lava_dark");
                scene.PushSpriteBatch(blendState: Microsoft.Xna.Framework.Graphics.BlendState.Additive);
                scene.SpriteBatch.Draw(lava.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), Color.White);
                scene.PopSpriteBatch();
            }
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("coral")]
    class Coral : Tile
    {
        protected int Frame = Random.Next(1000);

        public static List<Color> Colors = new List<Color>()
        {
            Color.LightPink,
            Color.LightSkyBlue,
            Color.LightSeaGreen,
            Color.Orange
        };

        public Coral() : base("Coral")
        {
        }

        public Coral(string name) : base(name)
        {
        }

        [Construct]
        public static Coral Construct(Context context)
        {
            return new Coral();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var color = Group.BrickColor;

            if (!IsVisible())
                color = HiddenColor;

            if (Under != null)
            {
                Under.VisualUnderColor = VisualUnderColor;
                Under.Draw(scene, drawPass);
            }
            if (!IsVisible())
                return;
            DrawCoral(scene);
        }

        public virtual void DrawCoral(SceneGame scene)
        {
            var coral = SpriteLoader.Instance.AddSprite("content/env_coral");

            scene.DrawSprite(coral, Frame, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Colors[Frame % Colors.Count], 0);
        }

        public void Destroy()
        {
            Scrape();
        }
    }

    [SerializeInfo("coral_acid")]
    class AcidCoral : Coral
    {
        public static List<Color> Colors = new List<Color>()
        {
            new Color(184, 177, 97),
            new Color(157, 147, 87),
            new Color(169, 186, 173),
            new Color(190, 189, 165),
        };
        
        public AcidCoral() : base("Acid Coral")
        {
        }

        [Construct]
        public static AcidCoral Construct(Context context)
        {
            return new AcidCoral();
        }

        public override void DrawCoral(SceneGame scene)
        {
            var coral = SpriteLoader.Instance.AddSprite("content/env_coral");

            scene.DrawSprite(coral, Frame, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Colors[Frame % Colors.Count], 0);
        }
    }

    [SerializeInfo("pool_acid")]
    class AcidPool : Tile
    {
        static ColorMatrix ColorMatrix = new ColorMatrix(new Matrix(
            0.8f, 0, 0, 0,
            0, 1.3f, 0, 0,
            0, 0, 1.3f, 0,
            0, 0, 0, 1),
            new Vector4(0, 0.2f, 0, 0));
        //static ColorMatrix ColorMatrix = ColorMatrix.TwoColorLight(new Color(152, 234, 0), new Color(236, 248, 201));
        //static ColorMatrix ColorMatrix = ColorMatrix.Greyscale() * ColorMatrix.TwoColorLight(Color.Lerp(Color.Black,Color.GreenYellow,0.5f), Color.YellowGreen);

        ConnectivityHelper Connectivity;

        public AcidPool() : base("Acid")
        {
            Effect.ApplyInnate(new EffectTrait(this, Trait.Acid));

            Connectivity = new ConnectivityHelper(this, GetConnection, Connects);
        }

        [Construct]
        public static AcidPool Construct(Context context)
        {
            return new AcidPool();
        }

        private ConnectivityHelper GetConnection(Tile tile)
        {
            if (tile is AcidPool acid)
                return acid.Connectivity;
            return null;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.SeaDistort;
            yield return DrawPass.Sea;
            yield return DrawPass.SeaFloor;
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            if (drawPass == DrawPass.SeaDistort)
            {
                Connectivity.CalculateIfNeeded();
                DrawNoise(scene, Connectivity.Connectivity, new Vector2(-base.World.Frame * 0.2f, -base.World.Frame * 0.5f), 0.25f);
            }
            else if (drawPass == DrawPass.SeaFloor)
            {
                bool visible = IsVisible();
                var color = visible ? Group.CaveColor.ToSeaFloor() : HiddenColor;

                DrawCave(scene, 0, 6, color);
                if (Under is Coral coral && visible)
                    coral.DrawCoral(scene);
            }
            else if (drawPass == DrawPass.Sea)
            {
                var lava = SpriteLoader.Instance.AddSprite("content/acid");
                scene.PushSpriteBatch(blendState: Microsoft.Xna.Framework.Graphics.BlendState.Additive);
                scene.SpriteBatch.Draw(lava.Texture, new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), Color.White * 0.8f);
                scene.PopSpriteBatch();
            }
        }

        public void Destroy()
        {
            Replace(new FloorCave());
        }
    }

    [SerializeInfo("anvil")]
    class Anvil : Tile
    {
        public Container Container;

        public Anvil() : base("Anvil")
        {
            Solid = true;

            Container = new Container();
        }

        [Construct]
        public static Anvil Construct(Context context)
        {
            return new Anvil();
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            base.AddTooltip(ref tooltip);
        }

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            base.AddActions(ui, player, selection);
            selection.Add(new ActAction("Anvil", "Anvils make tools from materials.", () =>
            {
                selection.Close();
                ui.Open(new MenuAnvil(ui, player, this));
            }));
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var anvil = SpriteLoader.Instance.AddSprite("content/anvil");

            if (Under != null)
                Under.Draw(scene, drawPass);
            if (!IsVisible())
                return;

            scene.DrawSprite(anvil, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);
        }

        public void Empty()
        {
            foreach (Item item in Container.Items)
                item.MoveTo(this);
        }
    }

    class TurnTakerSmelter : TurnTaker
    {
        Smelter Smelter;

        public TurnTakerSmelter(ActionQueue queue, Smelter smelter) : base(queue)
        {
            Smelter = smelter;
        }

        public override object Owner => Smelter;
        public override double Speed => 1;
        public override bool RemoveFromQueue => Smelter.Orphaned;

        public override Wait TakeTurn(Turn turn)
        {
            return Smelter.TakeTurn(turn);
        }
    }

    [SerializeInfo("smelter")]
    class Smelter : Tile
    {
        public double Ready;
        public Container OreContainer;
        public Container FuelContainer;
        public Dictionary<Material, int> Fuels = new Dictionary<Material, int>();
        public Dictionary<Material, int> FuelCapacities = new Dictionary<Material, int>();

        public double FuelTemperature => Fuels.Any() ? Fuels.Max(x => x.Key.FuelTemperature) : 0;
        public int FuelAmount => Fuels.Sum(fuel => fuel.Value);
        public int FuelCapacity => FuelCapacities.Sum(fuel => fuel.Value);
        public double SpeedBoost => 1.0f;

        public Smelter(SceneGame world) : base("Smelter")
        {
            Solid = true;

            world.ActionQueue.Add(new TurnTakerSmelter(world.ActionQueue, this));
            OreContainer = new Container() { Owner = this };
            FuelContainer = new Container() { Owner = this };
        }

        [Construct]
        public static Smelter Construct(Context context)
        {
            return new Smelter(context.World);
        }

        public Wait TakeTurn(Turn turn)
        {
            Work();

            return Wait.NoWait;
        }

        private void Work()
        {
            ConsumeFuel();

            if (!HasValidWork() || !HasFuel())
            {
                Ready = 0;
                return;
            }

            if (Ready >= 1)
            {
                IEnumerable<Item> ores = OreContainer.Items.Where(item => item is IOre);
                Dictionary<Material, int> alloySoup = ores.OfType<IOre>().GroupBy(ore => ore.Material).ToDictionary(group => group.Key, group => group.Sum(ore => ore.Amount));

                foreach(Material alloy in Material.Alloys.OrderBy(alloy => alloy.Priority))
                {
                    alloy.MakeAlloy(alloySoup);
                }

                foreach(Item item in ores)
                {
                    item.Destroy();
                }

                foreach(var pair in alloySoup)
                {
                    int value = pair.Value;
                    if(value >= 200)
                    {
                        int ingots = value / 200;
                        Ingot ingot = new Ingot(World, pair.Key, ingots);
                        ingot.MoveTo(this);
                        value -= ingots * 200;
                    }
                    if (value > 0)
                    {
                        Ore leftovers = new Ore(World, pair.Key, value);
                        OreContainer.Add(leftovers, true);
                    }
                }
                
                Ready = 0;
            }
            else
            {
                Ready += 0.2f * SpeedBoost;
                Ready = Math.Max(Math.Min(Ready, 1), 0);
            }
        }

        private void ConsumeFuel()
        {
            RemoveFuel(1);

            foreach (var item in FuelContainer.Items)
            {
                if (item is IFuel fuel)
                {
                    if (!Fuels.ContainsKey(fuel.Material))
                    {
                        Fuels.Add(fuel.Material, fuel.Amount);
                        FuelCapacities.Add(fuel.Material, fuel.Amount);
                        item.Destroy();
                    }
                }
            }
        }

        private void RemoveFuel(int i)
        {
            foreach (var key in Fuels.Keys.ToList())
            {
                Fuels[key] -= i;
                if (Fuels[key] <= 0)
                {
                    Fuels.Remove(key);
                    FuelCapacities.Remove(key);
                }
            }
        }

        private bool HasValidWork()
        {
            return OreContainer.Items.Any(x => x is IOre ore && ore.Material.MeltingTemperature <= FuelTemperature);
        }

        private bool HasFuel()
        {
            return Fuels.Max(x => x.Value) > OreContainer.Items.OfType<IOre>().Where(x => x.Material.MeltingTemperature <= FuelTemperature).Sum(x => x.Amount) / 50;
        }

        public override void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Name}{Game.FORMAT_BOLD}\n";
            AddDescription(ref tooltip);
            base.AddTooltip(ref tooltip);
        }

        public void AddDescription(ref string tooltip)
        {
            tooltip += $"Ready: {(int)(Math.Round(Ready, 2) * 100)}% ({Math.Round(SpeedBoost, 1)}x Speed)\n";
            float percentage = FuelCapacity > 0 ? (float)FuelAmount / FuelCapacity : 0;
            tooltip += $"Heat: {FuelTemperature} ({Game.FormatBar(Symbol.FuelBar, percentage)}{FuelAmount} Fuel left)\n";
            tooltip += "\n";
            tooltip += "Ore:\n";
            if (OreContainer.Items.Any())
                foreach (var item in OreContainer.Items)
                    tooltip += $"- {Game.FormatIcon(item)}{Game.FORMAT_BOLD}{item.InventoryName}{Game.FORMAT_BOLD}\n";
            else
                tooltip += "- Empty\n";
            tooltip += "Fuel:\n";
            if (FuelContainer.Items.Any())
                foreach (var item in FuelContainer.Items)
                    tooltip += $"- {Game.FormatIcon(item)}{Game.FORMAT_BOLD}{item.InventoryName}{Game.FORMAT_BOLD}\n";
            else
                tooltip += "- Empty\n";
        }

        public override void Draw(SceneGame scene, DrawPass drawPass)
        {
            var smelter = SpriteLoader.Instance.AddSprite("content/smelter_receptacle");
            var smelter_overlay = SpriteLoader.Instance.AddSprite("content/smelter_receptacle_overlay");

            if (!IsVisible())
                return;

            scene.DrawSprite(smelter, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);

            var allMaterials = new Dictionary<Material, float>();

            foreach (var ore in OreContainer.Items.OfType<IOre>())
                if (allMaterials.ContainsKey(ore.Material))
                    allMaterials[ore.Material] += ore.Amount;
                else
                    allMaterials.Add(ore.Material, ore.Amount);
            foreach (var fuel in FuelContainer.Items.OfType<IFuel>())
                if (allMaterials.ContainsKey(fuel.Material))
                    allMaterials[fuel.Material] += fuel.Amount;
                else
                    allMaterials.Add(fuel.Material, fuel.Amount);
            foreach(var fuel in Fuels)
                if (allMaterials.ContainsKey(fuel.Key))
                    allMaterials[fuel.Key] += fuel.Value;
                else
                    allMaterials.Add(fuel.Key, fuel.Value);

            if (allMaterials.Any())
            {
                ColorMatrix color = ColorMatrix.Lerp(allMaterials.ToDictionary(x => x.Key.ColorTransform, x => x.Value));

                scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
                {
                    scene.SetupColorMatrix(color, matrix, projection);
                });
                scene.DrawLava(new Rectangle(16 * Parent.X, 16 * Parent.Y, 16, 16), Color.White);
                scene.PopSpriteBatch();
            }
            
            scene.DrawSprite(smelter_overlay, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.White, 0);
        }

        public override void AddActions(PlayerUI ui, Creature player, MenuTextSelection selection)
        {
            base.AddActions(ui, player, selection);
            selection.Add(new ActAction("Smelter", "Smelters smelt ores into bars.", () =>
            {
                selection.Close();
                ui.Open(new MenuSmelter(ui, player, this));
            }));
        }

        public void Empty()
        {
            foreach (Item item in OreContainer.Items)
                item.MoveTo(this);
            foreach (Item item in FuelContainer.Items)
                item.MoveTo(this);
        }

        public override JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["containerOre"] = OreContainer.WriteJson(context);
            json["containerFuel"] = FuelContainer.WriteJson(context);
            json["fuel"] = WriteFuel();

            return json;
        }

        private JArray WriteFuel()
        {
            JArray fuelDict = new JArray();

            var fuelKeys = Fuels.Keys.Concat(FuelCapacities.Keys).Distinct();
            foreach (var key in fuelKeys)
            {
                JObject fuelJson = new JObject();
                fuelJson["material"] = key.ID;
                if (Fuels.ContainsKey(key))
                    fuelJson["amount"] = Fuels[key];
                if (FuelCapacities.ContainsKey(key))
                    fuelJson["capacity"] = FuelCapacities[key];
                fuelDict.Add(fuelJson);
            }

            return fuelDict;
        }

        public override void ReadJson(JToken json, Context context)
        {
            OreContainer = context.CreateEntity<Container>(json["containerOre"]);
            OreContainer.Owner = this;
            FuelContainer = context.CreateEntity<Container>(json["containerFuel"]);
            FuelContainer.Owner = this;
            ReadFuel(json["fuel"] as JArray);
        }

        private void ReadFuel(JArray fuelDict)
        {
            foreach(var fuelJson in fuelDict)
            {
                var key = Material.GetMaterial(fuelJson["material"].Value<string>());
                if (fuelJson.HasKey("amount"))
                    Fuels[key] = fuelJson["amount"].Value<int>();
                if (fuelJson.HasKey("capacity"))
                    FuelCapacities[key] = fuelJson["capacity"].Value<int>();
            }
        }
    }
}
