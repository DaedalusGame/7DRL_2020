using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Menus
{
    class NameInput : Menu
    {
        string Name;
        string Text;
        string OldString;
        public string NewString;
        public bool HasResult => OldString != NewString.Trim();

        public Vector2 Position;

        public NameInput(string name, string text, Vector2 position, int width, string oldString)
        {
            OldString = oldString;
            NewString = OldString;
            Name = name;
            Text = text;
            Position = position;
            Width = width;
        }

        public override int Height
        {
            get
            {
                return 16 * 3;
            }
            set
            {
                //NOOP
            }
        }

        public override void HandleInput(Scene scene)
        {
            scene.InputState.AddText(ref NewString);
            if (scene.InputState.IsKeyPressed(Keys.Enter))
                Close();
            if (scene.InputState.IsKeyPressed(Keys.Escape))
            {
                NewString = OldString;
                Close();
            }
            base.HandleInput(scene);
        }

        public override bool IsMouseOver(int x, int y)
        {
            return new Rectangle((int)Position.X - Width / 2, (int)Position.Y - Height / 2, Width, Height).Contains(x, y);
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);
            scene.PushSpriteBatch(blendState: scene.NonPremultiplied, samplerState: SamplerState.PointWrap, projection: Projection);
            scene.GraphicsDevice.Clear(Color.TransparentBlack);
            scene.DrawText(Text, new Vector2(8, 4), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(Width - 16 - 16, int.MaxValue));
            scene.DrawText($"{NewString}{Game.FormatBlinkingCursor(Ticks, 40)}", new Vector2(8, 4 + 16), Alignment.Center, new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(Width - 16 - 16, int.MaxValue));
            scene.PopSpriteBatch();
        }

        public override void Draw(Scene scene)
        {
            SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int x = (int)Position.X - Width / 2;
            int y = (int)Position.Y - Height / 2;
            float openCoeff = Math.Min(Ticks / 7f, 1f);
            float openResize = MathHelper.Lerp(-0.5f, 0.0f, openCoeff);
            Rectangle rect = new Rectangle(x, y, Width, Height);
            rect.Inflate(rect.Width * openResize, rect.Height * openResize);
            if (openCoeff > 0)
                DrawLabelledUI(scene, textbox, rect, openCoeff >= 1 ? Name : string.Empty);
            if (openCoeff >= 1)
            {
                scene.SpriteBatch.Draw(RenderTarget, new Rectangle(x, y, RenderTarget.Width, RenderTarget.Height), RenderTarget.Bounds, Color.White);
            }
        }
    }

    class InfoBox : Menu
    {
        public Func<string> Name;
        public Func<string> Text;

        public Vector2 Position;
        public int Scroll;

        public InfoBox(Func<string> name, Func<string> text, Vector2 position, int width, int height)
        {
            Name = name;
            Text = text;
            Position = position;
            Width = width;
            Height = height;
        }

        public override bool IsMouseOver(int x, int y)
        {
            return new Rectangle((int)Position.X - Width / 2, (int)Position.Y - Height / 2, Width, Height).Contains(x, y);
        }

        public override void HandleInput(Scene scene)
        {
            TextParameters parameters = new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(Width - 16 - 16, int.MaxValue);
            int textHeight = FontUtil.GetStringHeight(Text(), parameters);
            if (scene.InputState.IsKeyPressed(Keys.Enter))
                Close();
            if (scene.InputState.IsKeyPressed(Keys.Escape))
                Close();
            if (scene.InputState.IsKeyPressed(Keys.W, 10, 1))
                Scroll -= 3;
            if (scene.InputState.IsKeyPressed(Keys.S, 10, 1))
                Scroll += 3;
            if (scene.InputState.IsMouseWheelUp())
                Scroll -= 6;
            if (scene.InputState.IsMouseWheelDown())
                Scroll += 6;
            Scroll = MathHelper.Clamp(Scroll, 0, textHeight - Height + 8);
            base.HandleInput(scene);
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);
            scene.PushSpriteBatch(blendState: scene.NonPremultiplied, samplerState: SamplerState.PointWrap, projection: Projection);
            scene.GraphicsDevice.Clear(Color.TransparentBlack);
            scene.DrawText(Text(), new Vector2(8, 4 - Scroll), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black).SetConstraints(Width - 16 - 16, int.MaxValue));
            scene.PopSpriteBatch();
        }

        public override void Draw(Scene scene)
        {
            SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int x = (int)Position.X - Width / 2;
            int y = (int)Position.Y - Height / 2;
            float openCoeff = Math.Min(Ticks / 7f, 1f);
            float openResize = MathHelper.Lerp(-0.5f, 0.0f, openCoeff);
            Rectangle rect = new Rectangle(x, y, Width, Height);
            rect.Inflate(rect.Width * openResize, rect.Height * openResize);
            if (openCoeff > 0)
                DrawLabelledUI(scene, textbox, rect, openCoeff >= 1 ? Name() : string.Empty);
            if (openCoeff >= 1)
            {
                scene.SpriteBatch.Draw(RenderTarget, new Rectangle(x, y, RenderTarget.Width, RenderTarget.Height), RenderTarget.Bounds, Color.White);
                //scene.DrawText(Text(), new Vector2(x+8, y+4), Alignment.Left, new TextParameters().SetColor(Color.White,Color.Black).SetConstraints(Width - 16 - 16, Height-8));
            }
        }
    }

    abstract class MenuAct : Menu
    {
        public string Name;

        public abstract int SelectionCount
        {
            get;
        }
        public int Selection;
        public int Scroll;
        public int ScrollHeight;

        public int DefaultSelection = -1;

        public Vector2 Position;

        public override int Height
        {
            get { return ScrollHeight * LineHeight; }
            set { }
        }
        public virtual int LineHeight => 16;

        public MenuAct(string name, Vector2 position, int width, int scrollHeight)
        {
            Name = name;
            Position = position;
            Width = width;
            ScrollHeight = scrollHeight;
        }

        public override bool IsMouseOver(int x, int y)
        {
            return new Rectangle((int)Position.X - Width / 2, (int)Position.Y - Height / 2, Width, Height).Contains(x, y);
        }

        public abstract void Select(int selection);

        public override void HandleInput(Scene scene)
        {
            if (scene.InputState.IsKeyPressed(Keys.Enter) && Selection < SelectionCount)
                Select(Selection);
            if (scene.InputState.IsKeyPressed(Keys.Escape) && DefaultSelection >= 0)
                Select(DefaultSelection);
            if (scene.InputState.IsKeyPressed(Keys.W, 15, 5))
                Selection--;
            if (scene.InputState.IsKeyPressed(Keys.S, 15, 5))
                Selection++;
            Selection = SelectionCount <= 0 ? 0 : (Selection + SelectionCount) % SelectionCount;
            Scroll = MathHelper.Clamp(Scroll, Math.Max(0, Selection * LineHeight - Height + LineHeight), Math.Min(Selection * LineHeight, SelectionCount * LineHeight - Height));
            base.HandleInput(scene);
        }

        public override void PreDraw(Scene scene)
        {
            base.PreDraw(scene);

            scene.PushSpriteBatch(blendState: scene.NonPremultiplied, samplerState: SamplerState.PointWrap, projection: Projection);
            scene.GraphicsDevice.Clear(Color.TransparentBlack);
            for (int i = 0; i < SelectionCount; i++)
            {
                DrawLine(scene, new Vector2(0, i * LineHeight - Scroll), i);
            }
            scene.PopSpriteBatch();
        }

        public override void Draw(Scene scene)
        {
            SpriteReference textbox = SpriteLoader.Instance.AddSprite("content/ui_box");
            int x = (int)Position.X - Width / 2;
            int y = (int)Position.Y - Height / 2;
            float openCoeff = Math.Min(Ticks / 7f, 1f);
            float openResize = MathHelper.Lerp(-0.5f, 0.0f, openCoeff);
            Rectangle rect = new Rectangle(x, y, Width, Height);
            rect.Inflate(rect.Width * openResize, rect.Height * openResize);
            if (openCoeff > 0)
                DrawLabelledUI(scene, textbox, rect, openCoeff >= 1 ? Name : string.Empty);
            if (openCoeff >= 1)
                scene.SpriteBatch.Draw(RenderTarget, rect, RenderTarget.Bounds, Color.White);
        }

        public abstract void DrawLine(Scene scene, Vector2 linePos, int e);
    }

    class ActAction
    {
        public virtual string Name
        {
            get;
            set;
        }
        public virtual string Description
        {
            get;
            set;
        }
        public Action Action = () => { };
        public Func<bool> Enabled = () => true;

        public ActAction(string name, string description, Action action, Func<bool> enabled = null)
        {
            Name = name;
            Description = description;
            Action = action;
            if (action != null)
                Action = action;
            if (enabled != null)
                Enabled = enabled;
        }
    }

    class MenuTextSelection : MenuAct
    {
        List<ActAction> Actions = new List<ActAction>();

        public override int SelectionCount => Actions.Count;
        public Alignment Alignment = Alignment.Left;

        public MenuTextSelection(string name, Vector2 position, int width, int scrollHeight) : base(name, position, width, scrollHeight)
        {
        }

        public void Add(ActAction action)
        {
            Actions.Add(action);
        }

        public void AddDefault(ActAction action)
        {
            DefaultSelection = Actions.Count;
            Add(action);
        }

        public override void Select(int selection)
        {
            if (Actions[selection].Enabled())
                Actions[selection].Action();
        }

        public override void DrawLine(Scene scene, Vector2 linePos, int e)
        {
            ActAction action = Actions[e];
            SpriteReference cursor = SpriteLoader.Instance.AddSprite("content/cursor");
            if (Selection == e)
                scene.SpriteBatch.Draw(cursor.Texture, linePos + new Vector2(0, LineHeight / 2 - cursor.Height / 2), cursor.GetFrameRect(0), Color.White);
            Color color = Color.White;
            if (!action.Enabled())
                color = Color.Gray;
            scene.DrawText(action.Name, linePos + new Vector2(16, 0), Alignment, new TextParameters().SetConstraints(Width - 32, 16).SetBold(true).SetColor(color, Color.Black));
        }
    }
}
