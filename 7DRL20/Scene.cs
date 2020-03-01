using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class DrawStackFrame
    {
        public SpriteSortMode SortMode;
        public BlendState BlendState;
        public SamplerState SamplerState;
        public Matrix Transform;
        public Microsoft.Xna.Framework.Graphics.Effect Shader;
        public Action<Matrix> ShaderSetup;

        public DrawStackFrame(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, Matrix transform, Microsoft.Xna.Framework.Graphics.Effect shader, Action<Matrix> shaderSetup)
        {
            SortMode = sortMode;
            BlendState = blendState;
            SamplerState = samplerState;
            Transform = transform;
            Shader = shader;
            ShaderSetup = shaderSetup;
        }

        public void Apply(Scene scene)
        {
            ShaderSetup(Transform);
            scene.SpriteBatch.Begin(SortMode, BlendState, SamplerState, null, RasterizerState.CullNone, Shader, Transform);
        }
    }

    abstract class Scene
    {
        protected Game Game;

        public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        public SpriteBatch SpriteBatch => Game.SpriteBatch;
        public Texture2D Pixel => Game.Pixel;
        public int Frame => Game.Frame;
        public GameWindow Window => Game.Window;
        public Viewport Viewport => GraphicsDevice.Viewport;
        public Microsoft.Xna.Framework.Graphics.Effect Shader => Game.Shader;

        public Matrix WorldTransform;
        protected Matrix Projection;

        Stack<DrawStackFrame> SpriteBatchStack = new Stack<DrawStackFrame>();

        public InputTwinState InputState => Game.InputState;

        public BlendState NonPremultiplied = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.InverseSourceAlpha,
        };
        public BlendState Multiply = new BlendState()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
        };

        public Scene(Game game)
        {
            Game = game;
        }

        public abstract void Update(GameTime gameTime);

        public abstract void Draw(GameTime gameTime);

        public void SetupNormal(Matrix transform)
        {
            Shader.CurrentTechnique = Shader.Techniques["BasicColorDrawing"];
            Shader.Parameters["WorldViewProjection"].SetValue(transform * Projection);
        }

        public void SetupColorMatrix(ColorMatrix matrix)
        {
            SetupColorMatrix(matrix, WorldTransform);
        }

        public void SetupColorMatrix(ColorMatrix matrix, Matrix transform)
        {
            Shader.CurrentTechnique = Shader.Techniques["ColorMatrix"];
            Shader.Parameters["color_matrix"].SetValue(matrix.Matrix);
            Shader.Parameters["color_add"].SetValue(matrix.Add);
            Shader.Parameters["WorldViewProjection"].SetValue(transform * Projection);
        }

        public void PushSpriteBatch(SpriteSortMode? sortMode = null, BlendState blendState = null, SamplerState samplerState = null, Matrix? transform = null, Microsoft.Xna.Framework.Graphics.Effect shader = null, Action<Matrix> shaderSetup = null)
        {
            var lastState = SpriteBatchStack.Any() ? SpriteBatchStack.Peek() : null;
            if (sortMode == null)
                sortMode = lastState?.SortMode ?? SpriteSortMode.Deferred;
            if (blendState == null)
                blendState = lastState?.BlendState ?? NonPremultiplied;
            if (samplerState == null)
                samplerState = lastState?.SamplerState ?? SamplerState.PointClamp;
            if (transform == null)
                transform = lastState?.Transform ?? Matrix.Identity;
            if (shaderSetup == null)
                shaderSetup = SetupNormal;
            var newState = new DrawStackFrame(sortMode.Value, blendState, samplerState, transform.Value, shader, shaderSetup);
            if (!SpriteBatchStack.Empty())
                SpriteBatch.End();
            newState.Apply(this);
            SpriteBatchStack.Push(newState);
        }

        public void PopSpriteBatch()
        {
            SpriteBatch.End();
            SpriteBatchStack.Pop();
            if (!SpriteBatchStack.Empty())
            {
                var lastState = SpriteBatchStack.Peek();
                lastState.Apply(this);
            }
        }

        public void DrawUI(SpriteReference sprite, Rectangle area, Color color)
        {
            sprite.ShouldLoad = true;
            if (sprite.Width % 2 == 0 || sprite.Height % 2 == 0)
                return;
            int borderX = sprite.Width / 2;
            int borderY = sprite.Height / 2;
            var leftBorder = area.X - borderX;
            var topBorder = area.Y - borderY;
            var rightBorder = area.X + area.Width;
            var bottomBorder = area.Y + area.Height;
            SpriteBatch.Draw(sprite.Texture, new Rectangle(leftBorder, topBorder, borderX, borderY), new Rectangle(0, 0, borderX, borderY), color);
            SpriteBatch.Draw(sprite.Texture, new Rectangle(leftBorder, bottomBorder, borderX, borderY), new Rectangle(0, borderY + 1, borderX, borderY), color);
            SpriteBatch.Draw(sprite.Texture, new Rectangle(rightBorder, topBorder, borderX, borderY), new Rectangle(borderX + 1, 0, borderX, borderY), color);
            SpriteBatch.Draw(sprite.Texture, new Rectangle(rightBorder, bottomBorder, borderX, borderY), new Rectangle(borderX + 1, borderY + 1, borderX, borderY), color);

            SpriteBatch.Draw(sprite.Texture, new Rectangle(area.X, topBorder, area.Width, borderY), new Rectangle(borderX, 0, 1, borderY), color);
            SpriteBatch.Draw(sprite.Texture, new Rectangle(area.X, bottomBorder, area.Width, borderY), new Rectangle(borderX, borderY + 1, 1, borderY), color);

            SpriteBatch.Draw(sprite.Texture, new Rectangle(leftBorder, area.Y, borderX, area.Height), new Rectangle(0, borderY, borderX, 1), color);
            SpriteBatch.Draw(sprite.Texture, new Rectangle(rightBorder, area.Y, borderX, area.Height), new Rectangle(borderX + 1, borderY, borderX, 1), color);

            SpriteBatch.Draw(sprite.Texture, new Rectangle(area.X, area.Y, area.Width, area.Height), new Rectangle(borderX, borderY, 1, 1), color);
        }

        public string ConvertToPixelText(string text)
        {
            return Game.ConvertToPixelText(text);
        }

        public string ConvertToSmallPixelText(string text)
        {
            return Game.ConvertToSmallPixelText(text);
        }

        public void DrawText(string str, Vector2 drawpos, Alignment alignment, TextParameters parameters)
        {
            Game.DrawText(str, drawpos, alignment, parameters);
        }

        public void DrawSprite(SpriteReference sprite, int frame, Vector2 position, SpriteEffects mirror, float depth)
        {
            DrawSprite(sprite, frame, position, mirror, Color.White, depth);
        }

        public void DrawSprite(SpriteReference sprite, int frame, Vector2 position, SpriteEffects mirror, Color color, float depth)
        {
            SpriteBatch.Draw(sprite.Texture, position, sprite.GetFrameRect(frame), color, 0, Vector2.Zero, Vector2.One, mirror, depth);
        }

        public void DrawSpriteExt(SpriteReference sprite, int frame, Vector2 position, Vector2 origin, float angle, SpriteEffects mirror, float depth)
        {
            DrawSpriteExt(sprite, frame, position, origin, angle, Vector2.One, mirror, Color.White, depth);
        }

        public void DrawSpriteExt(SpriteReference sprite, int frame, Vector2 position, Vector2 origin, float angle, Vector2 scale, SpriteEffects mirror, Color color, float depth)
        {
            SpriteBatch.Draw(sprite.Texture, position + origin, sprite.GetFrameRect(frame), color, angle, origin, scale.Mirror(mirror), SpriteEffects.None, depth);
        }
    }
}
