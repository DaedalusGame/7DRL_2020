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
        public Matrix Projection;
        public Microsoft.Xna.Framework.Graphics.Effect Shader;
        public Action<Matrix, Matrix> ShaderSetup;

        public DrawStackFrame(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, Matrix transform, Matrix projection, Microsoft.Xna.Framework.Graphics.Effect shader, Action<Matrix, Matrix> shaderSetup)
        {
            SortMode = sortMode;
            BlendState = blendState;
            SamplerState = samplerState;
            Transform = transform;
            Projection = projection;
            Shader = shader;
            ShaderSetup = shaderSetup;
        }

        public void Apply(Scene scene)
        {
            ShaderSetup(Transform, Projection);
            scene.SpriteBatch.Begin(SortMode, BlendState, SamplerState, null, RasterizerState.CullNone, Shader, Transform);
        }
    }

    abstract class Scene
    {
        protected Game Game;

        public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        public SpriteBatch SpriteBatch => Game.SpriteBatch;
        public PrimitiveBatch<VertexPositionColorTexture> PrimitiveBatch => Game.PrimitiveBatch;
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

        public int AnimationFrame(SpriteReference sprite, float frame, float frameEnd)
        {
            return (int)MathHelper.Clamp(sprite.SubImageCount * frame / frameEnd, 0, sprite.SubImageCount - 1);
        }

        public void SetupNormal(Matrix transform, Matrix projection)
        {
            Shader.CurrentTechnique = Shader.Techniques["BasicColorDrawing"];
            Shader.Parameters["WorldViewProjection"].SetValue(transform * projection);
        }

        public void SetupColorMatrix(ColorMatrix matrix)
        {
            SetupColorMatrix(matrix, WorldTransform, Projection);
        }

        public void SetupColorMatrix(ColorMatrix matrix, Matrix transform, Matrix projection)
        {
            Shader.CurrentTechnique = Shader.Techniques["ColorMatrix"];
            Shader.Parameters["color_matrix"].SetValue(matrix.Matrix);
            Shader.Parameters["color_add"].SetValue(matrix.Add);
            Shader.Parameters["WorldViewProjection"].SetValue(transform * projection);
        }

        public void SetupWave(Vector2 waveTime, Vector2 waveDistance, Vector4 components)
        {
            SetupWave(waveTime, waveDistance, components, WorldTransform, Projection);
        }

        public void SetupWave(Vector2 waveTime, Vector2 waveDistance, Vector4 components, Matrix transform, Matrix projection)
        {
            Shader.CurrentTechnique = Shader.Techniques["Wave"];
            Shader.Parameters["wave_time_horizontal"].SetValue(waveTime.X);
            Shader.Parameters["wave_time_vertical"].SetValue(waveTime.Y);
            Shader.Parameters["wave_distance_horizontal"].SetValue(waveDistance.X);
            Shader.Parameters["wave_distance_vertical"].SetValue(waveDistance.Y);
            Shader.Parameters["wave_components"].SetValue(components);
            Shader.Parameters["WorldViewProjection"].SetValue(transform * projection);
        }

        public void SetupDistortion(Texture2D map, Vector2 offset, Matrix mapTransform)
        {
            SetupDistortion(map, offset, mapTransform, WorldTransform, Projection);
        }

        public void SetupDistortion(Texture2D map, Vector2 offset, Matrix mapTransform, Matrix transform, Matrix projection)
        {
            Shader.CurrentTechnique = Shader.Techniques["Distort"];
            Shader.Parameters["distort_offset"].SetValue(offset);
            Shader.Parameters["texture_map"].SetValue(map);
            Shader.Parameters["map_transform"].SetValue(mapTransform);
            Shader.Parameters["WorldViewProjection"].SetValue(transform * projection);
        }

        public void PushSpriteBatch(SpriteSortMode? sortMode = null, BlendState blendState = null, SamplerState samplerState = null, Matrix? transform = null, Matrix? projection = null, Microsoft.Xna.Framework.Graphics.Effect shader = null, Action<Matrix, Matrix> shaderSetup = null)
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
            if (projection == null)
                projection = lastState?.Projection ?? Matrix.Identity;
            if (shaderSetup == null)
                shaderSetup = SetupNormal;
            var newState = new DrawStackFrame(sortMode.Value, blendState, samplerState, transform.Value, projection.Value, shader, shaderSetup);
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

        public void DrawLine(SpriteReference sprite, Vector2 pos1, Vector2 pos2, float widthMod, float lengthMod, float offset)
        {
            var delta = pos2 - pos1;
            var dist = delta.Length();
            var side = (delta / dist).TurnLeft();
            var width = sprite.Height;
            //SetupNormal(WorldTransform, Projection);
            SetupColorMatrix(ColorMatrix.Identity, WorldTransform, Projection);
            PrimitiveBatch.Begin(PrimitiveType.TriangleStrip, texture: sprite.Texture, blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transform: WorldTransform, projection: Projection, effect: Shader);
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(pos1 + side * width * widthMod / 2, 0), Color.White, new Vector2(-offset / sprite.Width, 1)));
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(pos1 - side * width * widthMod / 2, 0), Color.White, new Vector2(-offset / sprite.Width, 0)));
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(pos2 + side * width * widthMod / 2, 0), Color.White, new Vector2((dist * lengthMod - offset) / sprite.Width, 1)));
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(pos2 - side * width * widthMod / 2, 0), Color.White, new Vector2((dist * lengthMod - offset) / sprite.Width, 0)));
            PrimitiveBatch.End();
        }

        public void DrawBeamLine(SpriteReference sprite, Vector2 pos1, Vector2 pos2, float widthMod, float lengthMod, float offset, float start, float end)
        {
            var delta = pos2 - pos1;
            var dist = delta.Length();
            var side = (delta / dist).TurnLeft();
            var width = sprite.Height;

            var mid1 = pos1 + delta * MathHelper.Clamp(start, 0, 1);
            var mid2 = pos1 + delta * MathHelper.Clamp(end, 0, 1);

            var tex1 = (dist * lengthMod * MathHelper.Clamp(start, 0, 1) - offset) / sprite.Width;
            var tex2 = (dist * lengthMod * MathHelper.Clamp(end, 0, 1) - offset) / sprite.Width;

            //SetupNormal(WorldTransform, Projection);
            SetupColorMatrix(ColorMatrix.Identity, WorldTransform, Projection);
            PrimitiveBatch.Begin(PrimitiveType.TriangleStrip, texture: sprite.Texture, blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transform: WorldTransform, projection: Projection, effect: Shader);
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(mid1 + side * width * widthMod / 2, 0), Color.White, new Vector2(tex1, 1)));
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(mid1 - side * width * widthMod / 2, 0), Color.White, new Vector2(tex1, 0)));
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(mid2 + side * width * widthMod / 2, 0), Color.White, new Vector2(tex2, 1)));
            PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(mid2 - side * width * widthMod / 2, 0), Color.White, new Vector2(tex2, 0)));
            PrimitiveBatch.End();
        }

        public void DrawBeamCurve(SpriteReference sprite, Func<float, Vector2> curve, int precision, Func<float, float> thickness, float lengthMod, float offset, float start, float end)
        {
            List<Vector2> points = new List<Vector2>();
            List<float> lengths = new List<float>();
            List<Vector2> pivots = new List<Vector2>();

            LineSet line = new LineSet();

            for(int i = 0; i <= precision; i++)
            {
                line.AddPoint(curve((float)i / precision));
            }

            line.GetBeam(start, end, points, pivots, lengths);

            var dist = line.TotalDistance;
            var width = sprite.Height;

            

            SetupColorMatrix(ColorMatrix.Identity, WorldTransform, Projection);
            PrimitiveBatch.Begin(PrimitiveType.TriangleStrip, texture: sprite.Texture, blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transform: WorldTransform, projection: Projection, effect: Shader);

            for(int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var side = pivots[i];
                var len = lengths[i];
                var slide = len / dist;
                var tex = (dist * lengthMod * slide - offset) / sprite.Width;
                var widthMod = thickness(slide);

                PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(point + side * width * widthMod / 2, 0), Color.White, new Vector2(tex, 1)));
                PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(point - side * width * widthMod / 2, 0), Color.White, new Vector2(tex, 0)));
            }

            PrimitiveBatch.End();
        }

        public void DrawMissileCurve(SpriteReference sprite, Func<float, Vector2> curve, int precision, Func<float, float> thickness, float start, float end)
        {
            List<Vector2> points = new List<Vector2>();
            List<float> lengths = new List<float>();
            List<Vector2> pivots = new List<Vector2>();

            LineSet line = new LineSet();

            for (int i = 0; i <= precision; i++)
            {
                line.AddPoint(curve((float)i / precision));
            }

            line.GetBeam(start, end, points, pivots, lengths);

            var dist = line.TotalDistance;
            var width = sprite.Height;

            SetupColorMatrix(ColorMatrix.Identity, WorldTransform, Projection);
            PrimitiveBatch.Begin(PrimitiveType.TriangleStrip, texture: sprite.Texture, blendState: NonPremultiplied, rasterizerState: RasterizerState.CullNone, samplerState: SamplerState.PointWrap, transform: WorldTransform, projection: Projection, effect: Shader);

            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var side = pivots[i];
                var len = lengths[i];
                var slide = len / dist;
                var tex = (slide - start) / (end - start);
                var widthMod = thickness(slide);

                PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(point + side * width * widthMod / 2, 0), Color.White, new Vector2(tex, 1)));
                PrimitiveBatch.AddVertex(new VertexPositionColorTexture(new Vector3(point - side * width * widthMod / 2, 0), Color.White, new Vector2(tex, 0)));
            }

            PrimitiveBatch.End();
        }
    }
}
