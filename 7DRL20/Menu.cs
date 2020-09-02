using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoguelikeEngine
{
    abstract class Menu
    {
        public int Ticks;
        public virtual bool ShouldClose
        {
            get;
            set;
        }
        public RenderTarget2D RenderTarget;
        public virtual int Width
        {
            get;
            set;
        }
        public virtual int Height
        {
            get;
            set;
        }
        public Matrix Projection;

        public void Close()
        {
            ShouldClose = true;
        }

        public virtual void Update(Scene scene)
        {
            Ticks++;
        }

        public virtual void HandleInput(Scene scene)
        {
            //NOOP
        }

        public virtual bool IsMouseOver(int x, int y)
        {
            return false;
        }

        public int GetStringHeight(string str, TextParameters parameters)
        {
            return FontUtil.GetStringHeight(str, parameters);
        }

        protected void DrawLabelledUI(Scene scene, SpriteReference sprite, Rectangle rectInterior, string label)
        {
            Rectangle rectExterior = new Rectangle(rectInterior.X, rectInterior.Y - 20, rectInterior.Width, 16);
            scene.DrawUI(sprite, rectInterior, Color.White);
            if (!string.IsNullOrWhiteSpace(label))
            {
                scene.DrawUI(sprite, rectExterior, Color.White);
                scene.DrawText(label, new Vector2(rectExterior.X, rectExterior.Y), Alignment.Center, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true).SetConstraints(rectExterior.Width, rectExterior.Height));
            }
        }

        public virtual void PreDraw(Scene scene)
        {
            Projection = Matrix.CreateOrthographicOffCenter(0, Width, Height, 0, 0, -1);
            if (Width > 0 && Height > 0)
            {
                if (RenderTarget == null || RenderTarget.IsContentLost || RenderTarget.Width != Width || RenderTarget.Height != Height)
                    RenderTarget = new RenderTarget2D(scene.GraphicsDevice, Width, Height);
                scene.GraphicsDevice.SetRenderTarget(RenderTarget);
            }
        }

        public abstract void Draw(Scene scene);
    }
}
