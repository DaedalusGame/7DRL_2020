﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.MapGeneration;
using RoguelikeEngine.Menus;
using RoguelikeEngine.VisualEffects;

namespace RoguelikeEngine
{
    class CameraFocus
    {
        public Vector2 Last;
        public Creature Current;
        Slider Slider;
        public bool Dead;

        public bool Done => Slider.Done;

        public Vector2 CurrentPos => Vector2.Lerp(Last, GetCameraPos(Current), Slider.Slide);

        static Vector2 GetCameraPos(Creature creature)
        {
            return creature.VisualCamera() + new Vector2(8, 8);
        }

        public CameraFocus(Creature creature) : this(GetCameraPos(creature), creature, 1)
        {
        }

        public CameraFocus(Vector2 last, Creature current, int time)
        {
            Last = last;
            Current = current;
            Slider = new Slider(time);
        }

        public CameraFocus MoveNext(Creature creature, int time)
        {
            return new CameraFocus(CurrentPos, creature, time);
        }

        public void Update()
        {
            Slider += 1;
        }

        public void SetDead()
        {
            Dead = true;
        }
    }

    class QuestSet : IEnumerable<Quest>
    {
        public List<Quest> Quests = new List<Quest>();

        public void Add(Quest quest)
        {
            quest.GlobalID = Guid.NewGuid();
            Quests.Add(quest);
        }

        public Quest Get(Guid globalId)
        {
            return Quests.Find(x => x.GlobalID == globalId);
        }

        public IEnumerator<Quest> GetEnumerator()
        {
            return Quests.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Quests.GetEnumerator();
        }

        public JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            var connections = new List<Tuple<Quest, Quest>>();
            JArray questArray = new JArray();
            JArray connectionArray = new JArray();
            foreach (var quest in Quests)
            {
                questArray.Add(quest.WriteJson());
                foreach (var prerequisite in quest.Prerequisites)
                    connections.Add(new Tuple<Quest, Quest>(prerequisite, quest));
            }
            foreach (var connection in connections)
            {
                JObject connectionJson = new JObject();
                connectionJson["start"] = connection.Item1.GlobalID.ToString();
                connectionJson["end"] = connection.Item2.GlobalID.ToString();
                connectionArray.Add(connectionJson);
            }
            json["quests"] = questArray;
            json["connections"] = connectionArray;
            return json;
        }

        public void ReadJson(JToken json, Context context)
        {
            JArray questArray = json["quests"] as JArray;
            JArray connectionArray = json["connections"] as JArray;

            foreach(var questJson in questArray)
            {
                Quests.Add(context.CreateQuest(questJson));
            }

            foreach(var connectionJson in connectionArray)
            {
                Quest start = Get(Guid.Parse(connectionJson["start"].Value<string>()));
                Quest end = Get(Guid.Parse(connectionJson["end"].Value<string>()));
                end.Prerequisites.Add(start);
            }
        }
    }

    [SerializeInfo]
    abstract class Quest
    {
        protected SceneGame Game;

        public Guid GlobalID;
        public virtual string Name
        {
            get;
        }
        public virtual string Description
        {
            get;
        }
        public bool Completed;
        public List<Quest> Prerequisites = new List<Quest>();
        public bool PrerequisitesCompleted => Prerequisites.All(quest => quest.Completed);


        public Quest(SceneGame game, params Quest[] prerequisites)
        {
            Game = game;
            Prerequisites.AddRange(prerequisites);
        }
        
        public abstract bool CheckCompletion();

        public abstract bool ShowMarker(Tile tile);

        public abstract string GetMarker(Tile tile);

        public JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["globalId"] = GlobalID.ToString();
            json["completed"] = Completed;
            return json;
        }

        public void ReadJson(JToken json, Context context)
        {
            GlobalID = Guid.Parse(json["globalId"].Value<string>());
            Completed = json["completed"].Value<bool>();
        }
    }

    class SceneGame : Scene
    {
        class TutorialGetOre : Quest
        {
            public override string Name => "Collect Ore";
            public override string Description => $"{Count}/{CountMax}";

            public int CountMax => 3;
            public int Count => Game.Player.GetInventory().OfType<IOre>().Where(ore => !IsFuel(ore) && ore.Amount >= 200).Sum(ore => ore.Amount / 200);

            public TutorialGetOre(SceneGame game, params Quest[] prerequisites) : base(game, prerequisites)
            {
            }

            [Construct("tutorial_get_ore")]
            public static TutorialGetOre Construct(Context context)
            {
                return new TutorialGetOre(context.World);
            }

            private bool IsFuel(IOre ore)
            {
                if(ore is IFuel fuel)
                {
                    return fuel.FuelTemperature > 0;
                }
                return false;
            }

            public override bool CheckCompletion()
            {
                return Count >= CountMax;
            }

            public override string GetMarker(Tile tile)
            {
                return "Collect Ore";
            }

            public override bool ShowMarker(Tile tile)
            {
                return tile.Items.Any(x => x is IOre ore && !IsFuel(ore));
            }
        }

        class TutorialGetFuel : Quest
        {
            public override string Name => "Collect Fuel";
            public override string Description => $"{Count}/{CountMax}";

            public int CountMax => 1;
            public int Count => Game.Player.GetInventory().OfType<IOre>().Where(ore => IsFuel(ore) && ore.Amount >= 200).Sum(ore => ore.Amount / 200);

            public TutorialGetFuel(SceneGame game, params Quest[] prerequisites) : base(game, prerequisites)
            {
            }

            [Construct("tutorial_get_fuel")]
            public static TutorialGetFuel Construct(Context context)
            {
                return new TutorialGetFuel(context.World);
            }

            private bool IsFuel(IOre ore)
            {
                if (ore is IFuel fuel)
                {
                    return fuel.FuelTemperature > 0;
                }
                return false;
            }

            public override bool CheckCompletion()
            {
                return Count >= CountMax;
            }

            public override string GetMarker(Tile tile)
            {
                return "Collect Fuel";
            }

            public override bool ShowMarker(Tile tile)
            {
                return tile.Items.Any(x => x is IOre ore && IsFuel(ore));
            }
        }

        class TutorialSmeltOre : Quest
        {
            public override string Name => "Smelt Ore";
            public override string Description => $"{Count}/{CountMax}";

            public int CountMax => 3;
            public int Count => Game.Player.GetInventory().OfType<Ingot>().Sum(ingot => ingot.Count);

            public TutorialSmeltOre(SceneGame game, params Quest[] prerequisites) : base(game, prerequisites)
            {
            }

            [Construct("tutorial_smelt_ore")]
            public static TutorialSmeltOre Construct(Context context)
            {
                return new TutorialSmeltOre(context.World);
            }

            public override bool CheckCompletion()
            {
                return Count >= CountMax;
            }

            public override string GetMarker(Tile tile)
            {
                return "Smelt Ore";
            }

            public override bool ShowMarker(Tile tile)
            {
                return tile is Smelter;
            }
        }

        class TutorialBuildAdze : Quest
        {
            public override string Name => "Build an Adze";
            public override string Description => String.Empty;

            public TutorialBuildAdze(SceneGame game, params Quest[] prerequisites) : base(game, prerequisites)
            {
            }

            [Construct("tutorial_build_adze")]
            public static TutorialBuildAdze Construct(Context context)
            {
                return new TutorialBuildAdze(context.World);
            }

            public override bool CheckCompletion()
            {
                return Game.Player.GetInventory().Any(x => x is ToolAdze);
            }

            public override string GetMarker(Tile tile)
            {
                return "Build Adze";
            }

            public override bool ShowMarker(Tile tile)
            {
                return tile is Anvil;
            }
        }

        const float ViewScale = 2.0f;

        Random Random = new Random();

        public PlayerUI Menu;
        public ActionQueue ActionQueue;
        public Turn Turn;
        public WaitWorld Wait = new WaitWorld();

        SaveFile SaveFile;

        public Map MapHome => GetMap("home");
        public Dictionary<string, Map> Maps = new Dictionary<string, Map>();
        public RenderTarget2D CameraTargetA;
        public RenderTarget2D CameraTargetB;
        public RenderTarget2D DistortionMap;

        public RenderTarget2D Lava;
        public RenderTarget2D Water;

        public CameraFocus CameraFocus;
        public Map CameraMap;
        public Vector2 Camera => CameraFocus.CurrentPos;
        public Vector2 CameraSize => new Vector2(Viewport.Width / 2, Viewport.Height / 2);
        public Vector2 CameraPosition => CameraMap != null ? FitCamera(Camera - CameraSize / 2, new Vector2(CameraMap.Width * 16, CameraMap.Height * 16)) : (Camera - CameraSize / 2);

        public Creature Player;
        public IEnumerable<Creature> Entities => GameObjects.OfType<Creature>();
        public IEnumerable<Item> Items => GameObjects.OfType<Item>();
        public IEnumerable<VisualEffect> VisualEffects => GameObjects.OfType<VisualEffect>();

        public List<IGameObject> GameObjects = new List<IGameObject>();
        public Queue<IGameObject> ToAdd = new Queue<IGameObject>();

        public QuestSet Quests = new QuestSet();
        public Skill CurrentSkill;

        public EnemySpawner Spawner;
        //TODO: Move to PlayerUI
        public HashSet<Enemy> SeenBosses = new HashSet<Enemy>();

        string Tooltip = "Test";
        Point? TileCursor;

        public SceneGame(Game game, SaveFile saveFile) : base(game)
        {
            SaveFile = saveFile;

            ActionQueue = new ActionQueue(this);
            Menu = new PlayerUI(this);

            Load();

            Player = Entities.First(x => x is Hero);

            CameraMap = Player.Map;
            CameraFocus = new CameraFocus(Player);

            Spawner = new EnemySpawner(this, 5);
        }

        public SceneGame(Game game) : base(game)
        {
            string saveName = $"save{DateTime.Now.ToString("ddMMyyyyHHmm")}";
            SaveFile = new SaveFile(new DirectoryInfo(Path.Combine(SaveFile.SaveDirectory.FullName, saveName)))
            {
                Name = "Test",
                CreateTime = DateTime.Now,
            };

            ActionQueue = new ActionQueue(this);
            Menu = new PlayerUI(this);

            CreateHome();

            PushObjects();

            var startTile = MapHome.EnumerateTiles().Where(tile => !tile.Solid).Shuffle(Random).First();

            Player = new Hero(this);
            Player.MoveTo(startTile, 1);
            Player.AddControlTurn();

            CameraMap = MapHome;
            CameraFocus = new CameraFocus(Player);

            Player.Pickup(new Ingot(this, Material.Bone, 80));
            Player.Pickup(new Ingot(this, Material.Dilithium, 80));
            Player.Pickup(new Ingot(this, Material.Tiberium, 80));
            Player.Pickup(new Ingot(this, Material.Basalt, 80));
            Player.Pickup(new Ingot(this, Material.Meteorite, 80));
            Player.Pickup(new Ingot(this, Material.Obsidiorite, 80));
            Player.Pickup(new Ingot(this, Material.Jauxum, 80));
            Player.Pickup(new Ingot(this, Material.Karmesine, 80));
            Player.Pickup(new Ingot(this, Material.Ovium, 80));
            Player.Pickup(new Ingot(this, Material.Ardite, 80));
            Player.Pickup(new Ingot(this, Material.Cobalt, 80));
            Player.Pickup(new Ingot(this, Material.Manyullyn, 80));
            Player.Pickup(new Ingot(this, Material.Terrax, 80));
            Player.Pickup(new Ingot(this, Material.Triberium, 80));
            Player.Pickup(new Ingot(this, Material.Aurorium, 80));
            Player.Pickup(new Ingot(this, Material.Violium, 80));
            Player.Pickup(new Ingot(this, Material.Astrium, 80));
            Player.Pickup(new Ingot(this, Material.Ignitz, 80));
            Player.Pickup(new Ingot(this, Material.Tritonite, 80));
            Player.Pickup(new ItemFeather(this, 80));
            Player.Pickup(new ItemHandle(this, 80));

            Quest getOre = new TutorialGetOre(this);
            Quest getFuel = new TutorialGetFuel(this, getOre);
            Quest smeltOre = new TutorialSmeltOre(this, getOre, getFuel);
            Quest buildAdze = new TutorialBuildAdze(this, smeltOre);
            Quests.Add(getOre);
            Quests.Add(getFuel);
            Quests.Add(smeltOre);
            Quests.Add(buildAdze);

            Spawner = new EnemySpawner(this, 5);
        }

        public void Save()
        {
            SaveFile.LastPlayedTime = DateTime.Now;
            SaveFile.Save(this);
        }

        public void Load()
        {
            SaveFile.Load(this);
            PushObjects();
        }

        public void ReturnToTitle()
        {
            Game.Scene = new SceneTitle(Game);
            EffectManager.Reset();
        }

        public void Restart()
        {
            Game.Scene = new SceneLoading(Game);
            EffectManager.Reset();
        }

        public void Quit()
        {
            Game.Exit();
        }

        public void SetMapId(string id, Map map)
        {
            if(map.ID != null)
                Maps.Remove(map.ID);
            Maps.Add(id, map);
            map.ID = id;
        }

        public void SetMapId(Guid guid, Map map)
        {
            SetMapId(guid.ToString(), map);
        }

        private void CreateHome()
        {
            GeneratorTemplate template = new TemplateHome(Random.Next());
            template.Build(this);
            Map map = template.Map;
            SetMapId("home", map);

            Tile startTile = template.GetStartRoom();
            Tile stairDown = template.BuildStairRoom();

            StairDown stairTile = new StairDown()
            {
                Type = StairType.RandomStart,
            };
            stairTile.InitBonuses();
            stairDown.Replace(stairTile);
        }

        private void BuildSmelterRoom(Tile center, GeneratorGroup group)
        {
            var offsets = new[] { new Point(1,0), new Point(0,1), new Point(-1,0), new Point(0,-1) };
            var offset = offsets.Pick(Random);
            int radius = Random.Next(3,5);
            int smelterX = -Random.Next(radius - 1) + Random.Next(radius - 1);
            int smelterY = -Random.Next(radius - 1) + Random.Next(radius - 1);

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var tile = center.GetNeighbor(x, y);
                    tile.Group = group;
                    if (x == offset.X * radius && y == offset.Y * radius)
                    {
                        tile.Replace(new FloorCave());
                    }
                    else if (x == smelterX && y == smelterY)
                    {
                        tile.Replace(new FloorCave());
                        tile.PlaceOn(new Smelter(this));
                    }
                    else if (x <= -radius || y <= -radius || x >= radius || y >= radius)
                    {
                        tile.Replace(new WallBrick());
                    }
                    else
                    {
                        tile.Replace(new FloorCave());
                    }
                }
            }
        }

        public Wait StartTurn(Turn turn)
        {
            return RoguelikeEngine.Wait.NoWait;
        }

        public Wait TakeTurn(Turn turn)
        {
            return RoguelikeEngine.Wait.NoWait;
        }

        public Wait EndTurn(Turn turn)
        {
            return RoguelikeEngine.Wait.NoWait;
        }

        private Vector2 FitCamera(Vector2 camera, Vector2 size)
        {
            if (camera.X < 0)
                camera.X = 0;
            if (camera.Y < 0)
                camera.Y = 0;
            if (camera.X > size.X - CameraSize.X)
                camera.X = size.X - CameraSize.X;
            if (camera.Y > size.Y - CameraSize.Y)
                camera.Y = size.Y - CameraSize.Y;
            return camera;
        }

        private Matrix CreateViewMatrix()
        {
            return Matrix.Identity
                * Matrix.CreateTranslation(new Vector3(-CameraPosition, 0))
                * Matrix.CreateTranslation(new Vector3(-CameraSize / 2, 0)) //These two lines center the character on (0,0)
                * Matrix.CreateScale(ViewScale, ViewScale, 1) //Scale it up by 2
                * Matrix.CreateTranslation(Viewport.Width / 2, Viewport.Height / 2, 0); //Translate the character to the middle of the viewport
        }

        private void SwapBuffers()
        {
            var helper = CameraTargetA;
            CameraTargetA = CameraTargetB;
            CameraTargetB = helper;
        }

        private bool IsBossVisible(Enemy enemy)
        {
            return enemy.IsVisible() && enemy.ShouldDraw(CameraMap);
        }

        private IEnumerable<Wait> RoutineBossWarning(Enemy boss)
        {
            yield return new WaitTime(40);
            SeenBosses.Add(boss);
            boss.OnManifest();
            CameraFocus = CameraFocus.MoveNext(boss, 30);
            Menu.BossWarning = new BossWarning(boss.BossDescription);
            yield return new WaitMenu(Menu.BossWarning);
            CameraFocus.SetDead();
        }

        public override void Update(GameTime gameTime)
        {
            SeenBosses.RemoveWhere(x => x.Destroyed);

            CameraFocus.Update();
            if (CameraFocus.Dead)
            {
                CameraFocus = CameraFocus.MoveNext(Player, 30);
            }
            InputTwinState state = Game.InputState;
            PushObjects();
            Menu.Update(this);

            if (Player.Dead)
                Menu.HandleInput(this);

            PopupHelper.Global.Update(this);
            //PopupManager.Update(this);
            Wait.Update();

            bool cancel = false;
            while (!cancel && Wait.Done && !Player.Dead && CameraFocus.Done)
            {
                var corpses = Entities.Where(x => x.Dead && !x.Control.HasImmediateTurns());
                List<Wait> waitForDestruction = new List<Wait>();
                foreach (var corpse in corpses)
                {
                    waitForDestruction.Add(Scheduler.Instance.RunAndWait(corpse.RoutineDestroy()));
                }
                if (waitForDestruction.Any())
                    Wait.Add(new WaitAll(waitForDestruction));

                Enemy foundBoss = Spawner.Bosses.Find(x => !x.Dead && IsBossVisible(x) && !SeenBosses.Contains(x));
                if (foundBoss != null)
                {
                    Wait.Add(Scheduler.Instance.RunAndWait(RoutineBossWarning(foundBoss)));
                    break;
                }

                ActionQueue.Step();

                Turn = ActionQueue.CurrentTurn;
                if (Turn == null)
                    break;

                switch (Turn.Phase)
                {
                    case TurnPhase.Start:
                        Wait.Add(Turn.StartTurn());
                        break;
                    case TurnPhase.Tick:
                        if (Turn.TurnTaker.Controllable(Player))
                        {
                            Menu.HandleInput(this);
                            cancel = true;
                        }
                        else if (Turn.TakeTurn())
                            Wait.Add(Turn.Wait);
                        break;
                    case TurnPhase.End:
                        Wait.Add(Turn.EndTurn());
                        break;
                }

                Wait.Update();
            }

            foreach (var obj in GameObjects.GetAndClean(x => x.Destroyed))
            {
                obj.Update();
            }

            foreach (var quest in Quests)
            {
                if (!quest.Completed && quest.PrerequisitesCompleted)
                    quest.Completed = quest.CheckCompletion();
            }

            Vector2 worldPos = Vector2.Transform(new Vector2(InputState.MouseX, InputState.MouseY), Matrix.Invert(WorldTransform));
            int tileX = Util.FloorDiv((int)worldPos.X, 16);
            int tileY = Util.FloorDiv((int)worldPos.Y, 16);

            TileCursor = new Point(tileX, tileY);
            if (Menu.IsMouseOver(InputState.MouseX, InputState.MouseY))
                TileCursor = null;

            Tooltip = string.Empty;
            if (TileCursor.HasValue && CameraMap != null)
            {
                Tile tile = CameraMap.GetTile(TileCursor.Value.X, TileCursor.Value.Y);
                if (tile != null && tile.IsVisible())
                    tile.AddTooltip(ref Tooltip);
            }
            Tooltip = Tooltip.Trim();
        }

        private void PushObjects()
        {
            while (ToAdd.Count > 0)
            {
                GameObjects.Add(ToAdd.Dequeue());
            }
        }

        public void DrawLava(Rectangle rectangle, Color color)
        {
            SpriteBatch.Draw(Lava, rectangle, rectangle, color);
        }

        private void DrawTextures()
        {
            SpriteReference lava = SpriteLoader.Instance.AddSprite("content/lava");
            SpriteReference water = SpriteLoader.Instance.AddSprite("content/water");

            if (Lava == null || Lava.IsContentLost)
                Lava = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);
            GraphicsDevice.SetRenderTarget(Lava);
            GraphicsDevice.Clear(Color.Transparent);
            PushSpriteBatch(shader: Shader, samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shaderSetup: (matrix, projection) => SetupWave(
                new Vector2(-Frame / 30f, -Frame / 30f + MathHelper.PiOver4),
                new Vector2(0.2f, 0.2f),
                new Vector4(0.1f, 0.0f, 0.1f, 0.0f),
                Matrix.Identity,
                Projection
                ));
            SpriteBatch.Draw(lava.Texture, new Rectangle(0, 0, Lava.Width, Lava.Height), new Rectangle(0, 0, Lava.Width, Lava.Height), Color.White);
            PopSpriteBatch();

            if (Water == null || Water.IsContentLost)
                Water = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);
            GraphicsDevice.SetRenderTarget(Water);
            GraphicsDevice.Clear(Color.Transparent);
            PushSpriteBatch(shader: Shader, samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shaderSetup: (matrix, projection) => SetupWave(
                new Vector2(-Frame / 30f, -Frame / 30f + MathHelper.PiOver4),
                new Vector2(0.2f, 0.2f),
                new Vector4(0.1f, 0.0f, 0.1f, 0.0f),
                Matrix.Identity,
                Projection
                ));
            SpriteBatch.Draw(water.Texture, new Rectangle(0, 0, Water.Width, Water.Height), new Rectangle(0, 0, Water.Width, Water.Height), Color.White);
            PopSpriteBatch();
            Menu.PreDraw(this);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTextures();

            GraphicsDevice.SetRenderTarget(null);

            if (CameraTargetA == null || CameraTargetA.IsContentLost)
                CameraTargetA = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            if (CameraTargetB == null || CameraTargetB.IsContentLost)
                CameraTargetB = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            if (DistortionMap == null || DistortionMap.IsContentLost)
                DistortionMap = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            Projection = Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, 0, -1);
            WorldTransform = CreateViewMatrix();

            IEnumerable<VisualEffect> visualEffects = VisualEffects.Where(x => x.ShouldDraw(CameraMap));
            IEnumerable<ScreenShake> screenShakes = visualEffects.OfType<ScreenShake>();
            if (screenShakes.Any())
            {
                ScreenShake screenShake = screenShakes.WithMax(effect => effect.Offset.LengthSquared());
                if (screenShake != null)
                    WorldTransform *= Matrix.CreateTranslation(screenShake.Offset.X, screenShake.Offset.Y, 0);
            }

            var gameObjects = GameObjects.Where(x => x.ShouldDraw(CameraMap));
            var tiles = DrawMap(CameraMap);
            var drawPasses = gameObjects.Concat(tiles).ToMultiLookup(x => x.GetDrawPasses());

            GraphicsDevice.SetRenderTarget(DistortionMap);
            /*var noise = SpriteLoader.Instance.AddSprite("content/noise");
            var noiseOffset = Util.AngleToVector(Frame * 0.1f) * 30;
            noiseOffset = new Vector2(-Frame * 0.2f, -Frame * 0.5f);
            PushSpriteBatch(samplerState: SamplerState.LinearWrap, blendState: BlendState.Additive);
            SpriteBatch.Draw(Pixel, DistortionMap.Bounds, new Color(0,64,0));
            SpriteBatch.Draw(noise.Texture, DistortionMap.Bounds, new Rectangle((int)noiseOffset.X, (int)noiseOffset.Y, DistortionMap.Width, DistortionMap.Height), Color.Red);
            PopSpriteBatch();*/
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: WorldTransform, projection: Projection);
            drawPasses.DrawPass(this, DrawPass.SeaDistort);
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(CameraTargetA);
            //Render Liquid to Target A
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: WorldTransform, projection: Projection);
            drawPasses.DrawPass(this, DrawPass.SeaFloor);
            drawPasses.DrawPass(this, DrawPass.Sea);
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(CameraTargetB);
            SwapBuffers();
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: WorldTransform, projection: Projection);
            drawPasses.DrawPass(this, DrawPass.LiquidFloor);
            //Render Target A (Liquid) to Target B (with distortion)
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: Matrix.Identity, shader: Shader, shaderSetup: (matrix, projection) =>
            {
                SetupDistortion(DistortionMap, new Vector2(30f / DistortionMap.Width, 30f / DistortionMap.Height), Matrix.Identity, Matrix.Identity, Projection);
            });
            SpriteBatch.Draw(CameraTargetB, CameraTargetB.Bounds, Color.White);
            //SpriteBatch.Draw(DistortionMap, DistortionMap.Bounds, Color.White);
            PopSpriteBatch();
            drawPasses.DrawPass(this, DrawPass.Liquid);
            //Render Map


            drawPasses.DrawPass(this, DrawPass.Tile);
            drawPasses.DrawPass(this, DrawPass.Item);
            drawPasses.DrawPass(this, DrawPass.EffectLow);
            PushSpriteBatch(blendState: BlendState.Additive);
            drawPasses.DrawPass(this, DrawPass.EffectLowAdditive);
            PopSpriteBatch();
            drawPasses.DrawPass(this, DrawPass.Creature);
            drawPasses.DrawPass(this, DrawPass.Effect);
            PushSpriteBatch(blendState: BlendState.Additive);
            drawPasses.DrawPass(this, DrawPass.EffectAdditive);
            PopSpriteBatch();
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(CameraTargetB);
            SwapBuffers();

            //Draw screenflashes
            ColorMatrix color = ColorMatrix.Identity;

            IEnumerable<ScreenFlash> screenFlashes = visualEffects.OfType<ScreenFlash>();
            foreach (ScreenFlash screenFlash in screenFlashes)
            {
                color *= screenFlash.Color;
            }

            //SetupColorMatrix(color, Matrix.Identity, Projection);
            //SetupGlitch(Game.Noise, Matrix.Identity, Projection, Random);
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shader: Shader, shaderSetup: (transform, projection) =>
            {
                SetupColorMatrix(color, Matrix.Identity, Projection);
            });
            SpriteBatch.Draw(CameraTargetB, CameraTargetB.Bounds, Color.White);
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(CameraTargetB);
            SwapBuffers();

            //Draw glitches
            IEnumerable<ScreenGlitch> screenGlitches = visualEffects.OfType<ScreenGlitch>();

            foreach (var glitch in screenGlitches)
            {
                GlitchParams glitchParams = glitch.Glitch;

                PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shader: Shader, shaderSetup: (transform, projection) =>
                {
                    SetupGlitch(Game.Noise, glitchParams, Random, Matrix.Identity, Projection);
                });
                SpriteBatch.Draw(CameraTargetB, CameraTargetB.Bounds, Color.White);
                PopSpriteBatch();

                GraphicsDevice.SetRenderTarget(CameraTargetB);
                SwapBuffers();
            }

            //Draw to screen
            GraphicsDevice.SetRenderTarget(null);

            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shader: Shader, shaderSetup: (transform, projection) =>
            {
                SetupColorMatrix(ColorMatrix.Identity, Matrix.Identity, Projection);
            });
            SpriteBatch.Draw(CameraTargetB, CameraTargetB.Bounds, Color.White);
            PopSpriteBatch();

            SpriteReference cursor_tile = SpriteLoader.Instance.AddSprite("content/cursor_tile");

            SetupNormal(Matrix.Identity, Projection);
            //SpriteBatch.Begin(blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transformMatrix: WorldTransform);
            PushSpriteBatch(blendState: NonPremultiplied, samplerState: SamplerState.PointWrap, transform: WorldTransform, projection: Projection);

            if (TileCursor.HasValue)
            {
                DrawSprite(cursor_tile, Frame / 8, new Vector2(TileCursor.Value.X * 16, TileCursor.Value.Y * 16), SpriteEffects.None, 0);
            }

            DrawQuests(CameraMap);

            drawPasses.DrawPass(this, DrawPass.UIWorld);

            //SpriteBatch.End();
            PopSpriteBatch();

            //SetupNormal(Matrix.Identity);
            //SpriteBatch.Begin(blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap);

            PushSpriteBatch(blendState: NonPremultiplied, samplerState: SamplerState.PointWrap, projection: Projection);

            DrawQuestText(CameraMap);

            drawPasses.DrawPass(this, DrawPass.UI);

            Menu.Draw(this);

            DrawTooltip();

            PopSpriteBatch();
        }

        private void DrawTooltip()
        {
            if (!string.IsNullOrWhiteSpace(Tooltip))
            {
                SpriteReference ui_tooltip = SpriteLoader.Instance.AddSprite("content/ui_box");
                TextParameters tooltipParameters = new TextParameters().SetColor(Color.White, Color.Black);
                int tooltipWidth = FontUtil.GetStringWidth(Tooltip, tooltipParameters);
                int screenWidth = Viewport.Width - 8 - InputState.MouseX + 4;
                bool invert = false;
                if (tooltipWidth > screenWidth)
                {
                    screenWidth = Viewport.Width - screenWidth;
                    invert = true;
                }
                tooltipParameters = new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(screenWidth,int.MaxValue);
                tooltipWidth = FontUtil.GetStringWidth(Tooltip, tooltipParameters);
                int tooltipHeight = FontUtil.GetStringHeight(Tooltip, tooltipParameters);
                int tooltipX = InputState.MouseX + 4;
                int tooltipY = Math.Max(0, InputState.MouseY - 4 - tooltipHeight);
                if (invert)
                    tooltipX -= tooltipWidth;
                DrawUI(ui_tooltip, new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight), Color.White);
                DrawText(Tooltip, new Vector2(tooltipX, tooltipY), Alignment.Left, tooltipParameters);
            }
        }

        private Rectangle GetDrawZone()
        {
            var drawZone = Viewport.Bounds;
            drawZone.Inflate(32, 32);
            return drawZone;
        }

        public Vector2 GetScreenPosition(float xSlide, float ySlide)
        {
            Vector2 a = Camera - CameraSize / 2;
            Vector2 b = Camera - CameraSize / 2 + new Vector2(CameraSize.X, 0);
            Vector2 c = Camera - CameraSize / 2 + new Vector2(0, CameraSize.Y);
            Vector2 d = Camera - CameraSize / 2 + new Vector2(CameraSize.X, CameraSize.Y);

            Vector2 ab = Vector2.Lerp(a, b, xSlide);
            Vector2 cd = Vector2.Lerp(c, d, xSlide);

            return Vector2.Lerp(ab, cd, ySlide);
        }

        private IEnumerable<Tile> EnumerateCloseTiles(Map map, int drawX, int drawY, int drawRadius)
        {
            Rectangle drawZone = GetDrawZone();

            for (int x = MathHelper.Clamp(drawX - drawRadius, 0, map.Width - 1); x <= MathHelper.Clamp(drawX + drawRadius, 0, map.Width - 1); x++)
            {
                for (int y = MathHelper.Clamp(drawY - drawRadius, 0, map.Height - 1); y <= MathHelper.Clamp(drawY + drawRadius, 0, map.Height - 1); y++)
                {
                    Vector2 truePos = Vector2.Transform(new Vector2(x * 16, y * 16), WorldTransform);

                    if (!drawZone.Contains(truePos))
                        continue;

                    yield return map.GetTile(x,y);
                }
            }
        }

        private IEnumerable<IDrawable> DrawMap(Map map)
        {
            if (map == null)
                return Enumerable.Empty<IDrawable>();

            int drawX = (int)(Camera.X / 16);
            int drawY = (int)(Camera.Y / 16);
            int drawRadius = 30;

            return EnumerateCloseTiles(map, drawX, drawY, drawRadius).OfType<IDrawable>();
        }

        private void DrawQuests(Map map)
        {
            if (map == null)
                return;

            SpriteReference cursor_tile = SpriteLoader.Instance.AddSprite("content/cursor_tile");

            int drawX = (int)(Camera.X / 16);
            int drawY = (int)(Camera.Y / 16);
            int drawRadius = 30;

            foreach (Tile tile in EnumerateCloseTiles(map, drawX, drawY, drawRadius))
            {
                if (!tile.IsVisible())
                    continue;
                foreach (var quest in Quests)
                {
                    if (!quest.Completed && quest.PrerequisitesCompleted && quest.ShowMarker(tile))
                        DrawSprite(cursor_tile, Frame / 1, new Vector2(tile.X * 16, tile.Y * 16), SpriteEffects.None, 0);
                }
            }
        }

        private void DrawQuestText(Map map)
        {
            if (map == null)
                return;

            HashSet<Quest> RenderedQuests = new HashSet<Quest>();

            int drawX = (int)(Camera.X / 16);
            int drawY = (int)(Camera.Y / 16);
            int drawRadius = 30;

            foreach (Tile tile in EnumerateCloseTiles(map, drawX, drawY, drawRadius))
            {
                if (!tile.IsVisible())
                    continue;
                foreach (var quest in Quests)
                {
                    if (!quest.Completed && !RenderedQuests.Contains(quest) && quest.PrerequisitesCompleted && quest.ShowMarker(tile))
                    {
                        Vector2 worldPos = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8);
                        var text = quest.GetMarker(tile);
                        DrawText(text,Vector2.Transform(worldPos,WorldTransform) + new Vector2(0,-32),Alignment.Center,new TextParameters().SetColor(Color.White,Color.Black).SetBold(true));
                        RenderedQuests.Add(quest);
                    }
                }
            }
        }

        public Map CreateMap(int width, int height)
        {
            return new Map(this, width, height);
        }

        public Map GetMap(string mapID)
        {
            return Maps.GetOrDefault(mapID, null);
        }

        public JToken WriteJson()
        {
            Context context = new Context(this);

            JObject json = new JObject();
            json["quests"] = Quests.WriteJson(context);

            return json;
        }

        public void ReadJson(JToken json)
        {
            Context context = new Context(this);

            Quests.ReadJson(json["quests"], context);
        }
    }
}
