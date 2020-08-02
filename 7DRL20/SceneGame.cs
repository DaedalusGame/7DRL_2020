using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.MapGeneration;

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

    abstract class Quest
    {
        protected SceneGame Game;

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

        public Quest(SceneGame game, params Quest[] prerequisites)
        {
            Game = game;
            Prerequisites.AddRange(prerequisites);
        }

        public bool PrerequisitesCompleted => Prerequisites.All(quest => quest.Completed);

        public abstract bool CheckCompletion();

        public abstract bool ShowMarker(Tile tile);

        public abstract string GetMarker(Tile tile);
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

        public Map MapHome;
        public Map Map;
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

        public List<Quest> Quests = new List<Quest>();
        public Skill CurrentSkill;

        public EnemySpawner Spawner;
        //TODO: Move to PlayerUI
        public HashSet<Enemy> SeenBosses = new HashSet<Enemy>();

        string Tooltip = "Test";
        Point? TileCursor;

        public SceneGame(Game game) : base(game)
        {
            ActionQueue = new ActionQueue(this);
            Menu = new PlayerUI(this);
            
            GeneratorTemplate template = new TemplateHome();
            template.Build(this);
            MapHome = template.Map;

            Tile startTile = template.GetStartRoom();
            Tile stairDown = template.BuildStairRoom();

            stairDown.Replace(new StairDown()
            {
                Template = new TemplateRandomLevel(new GroupRandom(), 0)
            });

            Player = new Hero(this);
            Player.MoveTo(startTile, 1);
            Player.AddControlTurn();
            /*Enemy testEnemy = new Wallhach(this);
            testEnemy.MoveTo(startTile.GetNeighbor(-2, 0),0);
            testEnemy.MakeAggressive(Player);
            ActionQueue.Add(testEnemy);*/
            /*testEnemy = new EnderErebizo(this);
            testEnemy.MoveTo(startTile.GetNeighbor(2,0),0);
            testEnemy.MakeAggressive(Player);
            ActionQueue.Add(testEnemy);*/

            CameraMap = MapHome;
            CameraFocus = new CameraFocus(Player);

            Player.Pickup(new Ingot(this, Material.Dilithium, 8));
            Player.Pickup(new Ingot(this, Material.Tiberium, 8));
            Player.Pickup(new Ingot(this, Material.Basalt, 8));
            Player.Pickup(new Ingot(this, Material.Meteorite, 8));
            Player.Pickup(new Ingot(this, Material.Obsidiorite, 8));
            Player.Pickup(new Ingot(this, Material.Jauxum, 8));
            Player.Pickup(new Ingot(this, Material.Karmesine, 8));
            Player.Pickup(new Ingot(this, Material.Ovium, 8));
            Player.Pickup(new Ingot(this, Material.Ardite, 8));
            Player.Pickup(new Ingot(this, Material.Cobalt, 8));
            Player.Pickup(new Ingot(this, Material.Manyullyn, 8));
            Player.Pickup(new Ingot(this, Material.Terrax, 8));
            Player.Pickup(new Ingot(this, Material.Triberium, 8));
            Player.Pickup(new Ingot(this, Material.Aurorium, 8));
            Player.Pickup(new Ingot(this, Material.Violium, 8));
            Player.Pickup(new Ingot(this, Material.Astrium, 8));
            Player.Pickup(new Ingot(this, Material.Ignitz, 8));
            Player.Pickup(new Ingot(this, Material.Tritonite, 8));

            Quest getOre = new TutorialGetOre(this);
            Quest getFuel = new TutorialGetFuel(this, getOre);
            Quest smeltOre = new TutorialSmeltOre(this, getOre, getFuel);
            Quest buildAdze = new TutorialBuildAdze(this, smeltOre);
            Quests.Add(getOre);
            Quests.Add(getFuel);
            Quests.Add(smeltOre);
            Quests.Add(buildAdze);

            Spawner = new EnemySpawner(this, 60);
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


        public void Restart()
        {
            Game.Scene = new SceneLoading(Game);
            EffectManager.Reset();
        }

        public void Quit()
        {
            Game.Exit();
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
            if(CameraFocus.Dead)
            {
                CameraFocus = CameraFocus.MoveNext(Player, 30);
            }
            InputTwinState state = Game.InputState;

            while(ToAdd.Count > 0)
            {
                GameObjects.Add(ToAdd.Dequeue());
            }
            Menu.Update(this);

            if(Player.Dead)
                Menu.HandleInput(this);

            PopupManager.Update(this);
            Wait.Update();

            bool cancel = false;
            while (!cancel && Wait.Done && !Player.Dead && CameraFocus.Done)
            {
                var corpse = Entities.Where(x => x.Dead).FirstOrDefault();
                if(corpse != null)
                {
                    Wait.Add(Scheduler.Instance.RunAndWait(corpse.RoutineDestroy()));
                    break;
                }

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
                if(!quest.Completed && quest.PrerequisitesCompleted)
                    quest.Completed = quest.CheckCompletion();
            }

            Vector2 worldPos = Vector2.Transform(new Vector2(InputState.MouseX, InputState.MouseY), Matrix.Invert(WorldTransform));
            int tileX = Util.FloorDiv((int)worldPos.X,16);
            int tileY = Util.FloorDiv((int)worldPos.Y,16);

            TileCursor = new Point(tileX, tileY);
            if (Menu.IsMouseOver(InputState.MouseX, InputState.MouseY))
                TileCursor = null;

            Tooltip = string.Empty;
            if (TileCursor.HasValue && CameraMap != null)
            {
                Tile tile = CameraMap.GetTile(TileCursor.Value.X, TileCursor.Value.Y);
                if(tile != null && tile.IsVisible())
                    tile.AddTooltip(ref Tooltip);
            }
            Tooltip = Tooltip.Trim();
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
            PushSpriteBatch(shader: Shader, samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shaderSetup: (matrix) => SetupWave(
                new Vector2(-Frame / 30f, -Frame / 30f + MathHelper.PiOver4),
                new Vector2(0.2f, 0.2f),
                new Vector4(0.1f, 0.0f, 0.1f, 0.0f),
                Matrix.Identity
                ));
            SpriteBatch.Draw(lava.Texture, new Rectangle(0, 0, Lava.Width, Lava.Height), new Rectangle(0, 0, Lava.Width, Lava.Height), Color.White);
            PopSpriteBatch();

            if (Water == null || Water.IsContentLost)
                Water = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);
            GraphicsDevice.SetRenderTarget(Water);
            GraphicsDevice.Clear(Color.Transparent);
            PushSpriteBatch(shader: Shader, samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, shaderSetup: (matrix) => SetupWave(
                new Vector2(-Frame / 30f, -Frame / 30f + MathHelper.PiOver4),
                new Vector2(0.2f, 0.2f),
                new Vector4(0.1f, 0.0f, 0.1f, 0.0f),
                Matrix.Identity
                ));
            SpriteBatch.Draw(water.Texture, new Rectangle(0, 0, Water.Width, Water.Height), new Rectangle(0, 0, Water.Width, Water.Height), Color.White);
            PopSpriteBatch();
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTextures();

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
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: WorldTransform);
            drawPasses.DrawPass(this, DrawPass.SeaDistort);
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(CameraTargetA);
            //Render Liquid to Target A
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: WorldTransform);
            drawPasses.DrawPass(this, DrawPass.SeaFloor);
            drawPasses.DrawPass(this, DrawPass.Sea);
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(CameraTargetB);
            SwapBuffers();
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: WorldTransform);
            drawPasses.DrawPass(this, DrawPass.LiquidFloor);
            //Render Target A (Liquid) to Target B (with distortion)
            PushSpriteBatch(samplerState: SamplerState.PointWrap, blendState: NonPremultiplied, transform: Matrix.Identity, shader: Shader, shaderSetup: (matrix) =>
            {
                SetupDistortion(DistortionMap, new Vector2(30f / DistortionMap.Width, 30f / DistortionMap.Height), Matrix.Identity, Matrix.Identity);
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

            GraphicsDevice.SetRenderTarget(null);

            //Render to screen
            ColorMatrix color = ColorMatrix.Identity;

            IEnumerable<ScreenFlash> screenFlashes = visualEffects.OfType<ScreenFlash>();
            foreach (ScreenFlash screenFlash in screenFlashes)
            {
                color *= screenFlash.Color;
            }

            SetupColorMatrix(color, Matrix.Identity);
            SpriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, effect: Shader);
            SpriteBatch.Draw(CameraTargetA, CameraTargetA.Bounds, Color.White);
            SpriteBatch.End();

            SpriteReference cursor_tile = SpriteLoader.Instance.AddSprite("content/cursor_tile");

            SetupNormal(Matrix.Identity);
            //SpriteBatch.Begin(blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transformMatrix: WorldTransform);
            PushSpriteBatch(blendState: NonPremultiplied, samplerState: SamplerState.PointWrap, transform: WorldTransform);

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

            PushSpriteBatch(blendState: NonPremultiplied, samplerState: SamplerState.PointWrap);

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
                string fitTooltip = FontUtil.FitString(FontUtil.StripFormat(Tooltip), tooltipParameters);
                int tooltipWidth = FontUtil.GetStringWidth(fitTooltip, tooltipParameters);
                int tooltipHeight = FontUtil.GetStringHeight(fitTooltip);
                int tooltipX = InputState.MouseX + 4;
                int tooltipY = Math.Max(0, InputState.MouseY - 4 - tooltipHeight);
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
    }
}
