using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RoguelikeEngine
{
    class SceneGame : Scene
    {
        const float ViewScale = 2f;

        Random random = new Random();

        public PlayerUI Menu;
        public ActionQueue ActionQueue = new ActionQueue();
        public Wait Wait = Wait.NoWait;

        public Map Map;
        public RenderTarget2D CameraTargetA;
        public RenderTarget2D CameraTargetB;

        public RenderTarget2D Lava;
        public RenderTarget2D Water;

        public Vector2 Camera => Player.VisualCamera() + new Vector2(8,8);
        public Vector2 CameraSize => new Vector2(Viewport.Width / 2, Viewport.Height / 2);
        public Vector2 CameraPosition => FitCamera(Camera - CameraSize / 2, new Vector2(Map.Width * 16, Map.Height * 16));

        public Creature Player;
        public IEnumerable<Creature> Entities => GameObjects.OfType<Creature>();
        public IEnumerable<Item> Items => GameObjects.OfType<Item>();
        public IEnumerable<VisualEffect> VisualEffects => GameObjects.OfType<VisualEffect>();

        public List<IGameObject> GameObjects = new List<IGameObject>();

        string Tooltip = "Test";
        Point? TileCursor;

        public SceneGame(Game game) : base(game)
        {
            Menu = new PlayerUI(this);
            Map = new Map(500, 500);

            Player = new Creature(this)
            {
                Name = "You",
                Description = "This is you.",
            };
            Player.MoveTo(Map.GetTile(250, 250));
            ActionQueue.TurnTakers.Add(Player);

            var enemy = new Creature(this)
            {
                Name = "Gay Bowser",
                Description = "?????",
            };
            enemy.MoveTo(Map.GetTile(255, 250));
            ActionQueue.TurnTakers.Add(enemy);

            Item testItem = ToolBlade.Create(this, Material.Karmesine, Material.Ovium, Material.Jauxum);
            testItem.MoveTo(Map.GetTile(250, 255));

            new Ore(this, Material.Dilithium, 1000).MoveTo(Map.GetTile(251, 255));
            new Ore(this, Material.Tiberium, 1000).MoveTo(Map.GetTile(251, 256));
            new Ore(this, Material.Basalt, 1000).MoveTo(Map.GetTile(251, 257));
            new Ore(this, Material.Triberium, 1000).MoveTo(Map.GetTile(251, 258));
            new Ore(this, Material.Jauxum, 1000).MoveTo(Map.GetTile(252, 255));
            new Ore(this, Material.Ovium, 1000).MoveTo(Map.GetTile(252, 256));
            new Ore(this, Material.Karmesine, 1000).MoveTo(Map.GetTile(252, 257));
            new Ore(this, Material.Meteorite, 1000).MoveTo(Map.GetTile(253, 255));
            new Ore(this, Material.Obsidiorite, 1000).MoveTo(Map.GetTile(253, 256));

            Map.GetTile(245, 250).Replace(new Smelter());
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

        public override void Update(GameTime gameTime)
        {
            InputTwinState state = Game.InputState;

            if (Wait.Done)
            {
                ActionQueue.Step();

                Creature creature = ActionQueue.CurrentTurnTaker as Creature;

                if (creature == Player && creature.CurrentAction.Done)
                {
                    Menu.HandleInput(this);
                }
                else if(creature != null && creature.CurrentAction.Done)
                {
                    //creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineMove(1, 0));
                    creature.ResetTurn();
                }
            }

            foreach (var obj in GameObjects.GetAndClean(x => x.Remove))
            {
                obj.Update();
            }

            Vector2 worldPos = Vector2.Transform(new Vector2(InputState.MouseX, InputState.MouseY), Matrix.Invert(WorldTransform));
            int tileX = Util.FloorDiv((int)worldPos.X,16);
            int tileY = Util.FloorDiv((int)worldPos.Y,16);

            TileCursor = new Point(tileX, tileY);
            if (Menu.IsMouseOver(InputState.MouseX, InputState.MouseY))
                TileCursor = null;

            Tooltip = string.Empty;
            if(TileCursor.HasValue)
                Map.GetTile(TileCursor.Value.X, TileCursor.Value.Y)?.AddTooltip(ref Tooltip);
            Tooltip = Tooltip.Trim();
        }

        public void DrawLava(Rectangle rectangle)
        {
            SpriteBatch.Draw(Lava, rectangle, new Rectangle(0,0,rectangle.Width,rectangle.Height), Color.White);
        }

        private void DrawTextures()
        {
            SpriteReference lava = SpriteLoader.Instance.AddSprite("content/lava");
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
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTextures();

            if (CameraTargetA == null || CameraTargetA.IsContentLost)
                CameraTargetA = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            if (CameraTargetB == null || CameraTargetB.IsContentLost)
                CameraTargetB = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            GraphicsDevice.SetRenderTarget(CameraTargetA);

            Projection = Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, 0, -1);
            WorldTransform = CreateViewMatrix();

            var drawPasses = GameObjects.ToMultiLookup(x => x.GetDrawPasses());

            PushSpriteBatch(samplerState: SamplerState.PointClamp, blendState: NonPremultiplied, transform: WorldTransform);
            DrawMap(Map);

            drawPasses.DrawPass(this, DrawPass.Tile);
            drawPasses.DrawPass(this, DrawPass.Item);
            drawPasses.DrawPass(this, DrawPass.Creature);
            PopSpriteBatch();

            GraphicsDevice.SetRenderTarget(null);

            //Render to screen
            ColorMatrix color = ColorMatrix.Identity;
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

            drawPasses.DrawPass(this, DrawPass.UIWorld);

            //SpriteBatch.End();
            PopSpriteBatch();

            //SetupNormal(Matrix.Identity);
            //SpriteBatch.Begin(blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap);

            PushSpriteBatch(blendState: NonPremultiplied, samplerState: SamplerState.PointWrap);

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

        private void DrawMap(Map map)
        {
            int drawX = (int)(Camera.X / 16);
            int drawY = (int)(Camera.Y / 16);
            int drawRadius = 30;

            foreach (Tile tile in EnumerateCloseTiles(map, drawX, drawY, drawRadius))
            {
                tile.Draw(this);
            }
        }  
    }
}
