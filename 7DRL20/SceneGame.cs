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

        public ActionQueue ActionQueue = new ActionQueue();
        public Wait Wait = Wait.NoWait;

        public Map Map;
        public RenderTarget2D CameraTargetA;
        public RenderTarget2D CameraTargetB;

        public Vector2 Camera => Player.VisualCamera() + new Vector2(8,8);
        public Vector2 CameraSize => new Vector2(Viewport.Width / 2, Viewport.Height / 2);
        public Vector2 CameraPosition => FitCamera(Camera - CameraSize / 2, new Vector2(Map.Width * 16, Map.Height * 16));

        Creature Player;
        List<Creature> Entities = new List<Creature>();
        List<Item> Items = new List<Item>();

        string Tooltip = "Test";
        Point? TileCursor;

        public SceneGame(Game game) : base(game)
        {
            Map = new Map(500, 500);

            Player = new Creature()
            {
                Name = "You",
                Description = "This is you.",
            };
            Player.MoveTo(Map.GetTile(250, 250));
            ActionQueue.TurnTakers.Add(Player);
            Entities.Add(Player);

            var enemy = new Creature()
            {
                Name = "Gay Bowser",
                Description = "?????",
            };
            enemy.MoveTo(Map.GetTile(255, 250));
            ActionQueue.TurnTakers.Add(enemy);
            Entities.Add(enemy);

            Item testItem = ToolBlade.Create(Material.Karmesine, Material.Ovium, Material.Jauxum);
            testItem.MoveTo(Map.GetTile(250, 255));
            Items.Add(testItem);
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
                    if (state.IsKeyPressed(Keys.W, 20, 5))
                    {
                        creature.Facing = Facing.North;
                        creature.ResetTurn();
                        creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineMove(0, -1));
                    }
                    if (state.IsKeyPressed(Keys.S, 20, 5))
                    {
                        Player.Facing = Facing.South;
                        creature.ResetTurn();
                        creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineMove(0, 1));
                    }
                    if (state.IsKeyPressed(Keys.A, 20, 5))
                    {
                        creature.Facing = Facing.West;
                        creature.ResetTurn();
                        creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineMove(-1, 0));
                    }
                    if (state.IsKeyPressed(Keys.D, 20, 5))
                    {
                        creature.Facing = Facing.East;
                        creature.ResetTurn();
                        creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineMove(1, 0));
                    }
                    if (state.IsKeyPressed(Keys.Space))
                    {
                        var offset = creature.Facing.ToOffset();
                        creature.ResetTurn();
                        Wait = creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineAttack(offset.X, offset.Y));
                    }
                }
                else if(creature != null && creature.CurrentAction.Done)
                {
                    creature.CurrentAction = Scheduler.Instance.RunAndWait(creature.RoutineMove(1, 0));
                    creature.ResetTurn();
                }
            }

            foreach (var entity in Entities)
            {
                entity.Update();
            }

            Vector2 worldPos = Vector2.Transform(new Vector2(InputState.MouseX, InputState.MouseY), Matrix.Invert(WorldTransform));
            int tileX = Util.FloorDiv((int)worldPos.X,16);
            int tileY = Util.FloorDiv((int)worldPos.Y,16);

            TileCursor = new Point(tileX, tileY);

            Tooltip = string.Empty;
            if(TileCursor.HasValue)
                Map.GetTile(TileCursor.Value.X, TileCursor.Value.Y)?.AddTooltip(ref Tooltip);
            Tooltip = Tooltip.Trim();
        }

        public override void Draw(GameTime gameTime)
        {
            if (CameraTargetA == null || CameraTargetA.IsContentLost)
                CameraTargetA = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            if (CameraTargetB == null || CameraTargetB.IsContentLost)
                CameraTargetB = new RenderTarget2D(GraphicsDevice, Viewport.Width, Viewport.Height);

            GraphicsDevice.SetRenderTarget(CameraTargetA);

            Projection = Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, 0, -1);
            WorldTransform = CreateViewMatrix();

            PushSpriteBatch(samplerState: SamplerState.PointClamp, blendState: NonPremultiplied, transform: WorldTransform);
            DrawMap(Map);

            foreach (var item in Items)
            {
                Tile tile = item.Tile;
                item.DrawIcon(this, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8));
            }

            foreach (var entity in Entities)
            {
                entity.Draw(this);
            }
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
            SpriteBatch.Begin(blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transformMatrix: WorldTransform);

            if (TileCursor.HasValue)
            {
                DrawSprite(cursor_tile, Frame / 8, new Vector2(TileCursor.Value.X * 16, TileCursor.Value.Y * 16), SpriteEffects.None, 0);
            }

            SpriteBatch.End();

            DrawTooltip();
        }

        private void DrawTooltip()
        {
            if (!String.IsNullOrWhiteSpace(Tooltip))
            {
                SpriteReference ui_tooltip = SpriteLoader.Instance.AddSprite("content/ui_box");
                SetupNormal(Matrix.Identity);
                SpriteBatch.Begin(blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap);
                TextParameters tooltipParameters = new TextParameters().SetColor(Color.White, Color.Black);
                string fitTooltip = FontUtil.FitString(Tooltip, tooltipParameters);
                int tooltipWidth = FontUtil.GetStringWidth(Tooltip, tooltipParameters);
                int tooltipHeight = FontUtil.GetStringHeight(Tooltip);
                int tooltipX = InputState.MouseX + 4;
                int tooltipY = Math.Max(0, InputState.MouseY - 4 - tooltipHeight);
                DrawUI(ui_tooltip, new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight), Color.White);
                DrawText(Tooltip, new Vector2(tooltipX, tooltipY), Alignment.Left, tooltipParameters);
                SpriteBatch.End();
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
