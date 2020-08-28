using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoguelikeEngine
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    class Game : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager Graphics;
        public SpriteBatch SpriteBatch;

        public Texture2D Pixel;
        public Microsoft.Xna.Framework.Graphics.Effect Shader;

        public Scene Scene;

        public int Frame;

        private IEnumerator<InputTwinState> Input;
        public InputTwinState InputState => Input.Current;

        const int FontSpritesAmount = 64;
        public static SpriteReference[] FontSprites = new SpriteReference[FontSpritesAmount];

        FrameCounter FPS = new FrameCounter();
        FrameCounter GFPS = new FrameCounter();

        public Game()
        {
            Graphics = new GraphicsDeviceManager(this);
            Graphics.PreferredBackBufferWidth = 800;
            Graphics.PreferredBackBufferHeight = 600;
            Graphics.ApplyChanges();
            Content.RootDirectory = "Content";
        }

        public IEnumerator<InputTwinState> InputIterator()
        {
            InputState previous = new InputState();
            InputTwinState twinState = null;
            StringBuilder textBuffer = new StringBuilder();

            Window.TextInput += (sender, e) =>
            {
                textBuffer.Append(e.Character);
            };

            while (true)
            {
                var keyboard = Keyboard.GetState();
                var mouse = Mouse.GetState();
                var gamepad = GamePad.GetState(0);

                InputState next = new InputState(keyboard, mouse, gamepad, textBuffer.ToString());
                twinState = new InputTwinState(previous, next, twinState);
                twinState.HandleRepeats();
                textBuffer.Clear();
                yield return twinState;
                previous = next;
               
            }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Input = InputIterator();

            SpriteLoader.Init(GraphicsDevice);
            Scheduler.Init();
            Serializer.Init();

            RenderTarget2D pixel = new RenderTarget2D(GraphicsDevice, 1, 1);
            GraphicsDevice.SetRenderTarget(pixel);
            GraphicsDevice.Clear(Color.White);
            GraphicsDevice.SetRenderTarget(null);
            Pixel = pixel;

            LoadFont();

            Element.Init();
            Stat.Init();

            Scene = new SceneLoading(this);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            Shader = Content.Load<Microsoft.Xna.Framework.Graphics.Effect>("effects");

            // TODO: use this.Content to load your game content here
        }

        private void LoadFont()
        {
            for (int i = 0; i < FontSpritesAmount; i++)
            {
                FontSprites[i] = SpriteLoader.Instance.AddSprite("content/font/font_" + i + "_0");
                //FontSprites[i].ShouldLoad = true;
                int fontIndex = i;
                FontSprites[i].SetLoadFunction(() => LoadFontPart(FontSprites[fontIndex], fontIndex));
            }

            FontUtil.CharInfo[' '] = new CharInfo(0, 4, true);
            FontUtil.CharInfo[FORMAT_BOLD] = new CharInfo(0, 0, true);
            FontUtil.CharInfo[FORMAT_ITALIC] = new CharInfo(0, 0, true);
            FontUtil.CharInfo[FORMAT_UNDERLINE] = new CharInfo(0, 0, true);
            FontUtil.CharInfo[FORMAT_SUBSCRIPT] = new CharInfo(0, 0, true);
            FontUtil.CharInfo[FORMAT_SUPERSCRIPT] = new CharInfo(0, 0, true);
            FontUtil.CharInfo[FORMAT_BLANK] = new CharInfo(0, 0, true);
            FontUtil.CharInfo[FORMAT_ICON] = new CharInfo(0, 16, true);
            for (char i = FORMAT_DYNAMIC_BEGIN; i < FORMAT_DYNAMIC_END; i++)
                FontUtil.CharInfo[i] = new CharInfo(0, 0, true);
        }

        private void LoadFontPart(SpriteReference sprite, int index)
        {
            Texture2D tex = sprite.Texture;
            Color[] blah = new Color[tex.Width * tex.Height];
            tex.GetData<Color>(0, new Rectangle(0, 0, tex.Width, tex.Height), blah, 0, blah.Length);

            for (int i = 0; i < FontUtil.CharsPerPage; i++)
            {
                FontUtil.RegisterChar(blah, tex.Width, tex.Height, (char)(index * FontUtil.CharsPerPage + i), i);
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            Input.MoveNext();

            SpriteLoader.Instance.Update(gameTime);
            Scheduler.Instance.Update();

            Scene.Update(gameTime);

            GFPS.Update(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            SpriteLoader.Instance.Draw(gameTime);

            GraphicsDevice.Clear(Color.Black);

            Frame++;

            Scene.Draw(gameTime);

            FPS.Update(gameTime);
            SpriteBatch.Begin(blendState: BlendState.NonPremultiplied);
            DrawText($"FPS: {FPS.AverageFramesPerSecond.ToString("f1")}\nGFPS: {GFPS.AverageFramesPerSecond.ToString("f1")}", new Vector2(0, 0), Alignment.Left, new TextParameters().SetColor(Color.White, Color.Black));
            SpriteBatch.End();

            base.Draw(gameTime);
        }

        public const char PRIVATE_ZONE_BEGIN = (char)0xE000;
        public const char PIXEL_TEXT_ZONE_BEGIN = (char)(PRIVATE_ZONE_BEGIN + 0);
        public const char PIXEL_TEXT_SMALL_ZONE_BEGIN = (char)(PRIVATE_ZONE_BEGIN + 128);
        public const char FORMAT_CODES_BEGIN = (char)(PRIVATE_ZONE_BEGIN + 256);
        public const char FORMAT_BOLD = (char)(FORMAT_CODES_BEGIN + 0);
        public const char FORMAT_ITALIC = (char)(FORMAT_CODES_BEGIN + 1);
        public const char FORMAT_UNDERLINE = (char)(FORMAT_CODES_BEGIN + 2);
        public const char FORMAT_SUBSCRIPT = (char)(FORMAT_CODES_BEGIN + 3);
        public const char FORMAT_SUPERSCRIPT = (char)(FORMAT_CODES_BEGIN + 4);
        public const char FORMAT_ICON = (char)(FORMAT_CODES_BEGIN + 5);
        public const char FORMAT_ELEMENT_ICON = (char)(FORMAT_CODES_BEGIN + 6);
        public const char FORMAT_COLOR = (char)(FORMAT_CODES_BEGIN + 7);
        public const char FORMAT_BORDER = (char)(FORMAT_CODES_BEGIN + 8);
        public const char FORMAT_BLANK = (char)(FORMAT_CODES_BEGIN + 9);
        public const char FORMAT_STAT_ICON = (char)(FORMAT_CODES_BEGIN + 10);
        public const char FORMAT_SYMBOL = (char)(FORMAT_CODES_BEGIN + 11);
        public const char FORMAT_BAR = (char)(FORMAT_CODES_BEGIN + 12);
        public const char FORMAT_DYNAMIC_BEGIN = (char)(FORMAT_CODES_BEGIN + 1024);
        public const char FORMAT_DYNAMIC_END = (char)(FORMAT_DYNAMIC_BEGIN + 512);

        public static string ConvertToPixelText(string text)
        {
            StringBuilder convertedText = new StringBuilder();

            foreach (char c in text)
            {
                if (c == ' ' || c == '\n')
                    convertedText.Append(c);
                else if (c > 32 && c < 127)
                    convertedText.Append((char)(PIXEL_TEXT_ZONE_BEGIN + c));
                else
                    convertedText.Append((char)(PIXEL_TEXT_ZONE_BEGIN + '?'));
            }

            return convertedText.ToString();
        }

        public static string ConvertToSmallPixelText(string text)
        {
            StringBuilder convertedText = new StringBuilder();

            foreach (char c in text)
            {
                if (c == ' ' || c == '\n')
                    convertedText.Append(c);
                else if (c > 32 && c < 127)
                    convertedText.Append((char)(PIXEL_TEXT_SMALL_ZONE_BEGIN + c));
                else
                    convertedText.Append((char)(PIXEL_TEXT_SMALL_ZONE_BEGIN + '?'));
            }

            return convertedText.ToString();
        }

        public static string FormatBlinkingCursor(int tick, int frequency)
        {
            bool on = (tick % frequency) < (frequency / 2);
            string format = !on ? $"{FormatColor(Color.Transparent)}{FormatBorder(Color.Transparent)}" : "";
            return $"{format}_";
        }

        public static string FormatIcon(IEffectHolder effectHolder)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_ICON);
            builder.Append(StringUtil.ToFormatString(effectHolder.ObjectID));
            return builder.ToString();
        }

        public static string FormatElement(Element element)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_ELEMENT_ICON);
            builder.Append(StringUtil.ToFormatString(element.ID));
            return builder.ToString();
        }

        public static string FormatStat(Stat stat)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_STAT_ICON);
            builder.Append(StringUtil.ToFormatString(stat.ID));
            return builder.ToString();
        }

        public static string FormatSymbol(Symbol symbol)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_SYMBOL);
            builder.Append(StringUtil.ToFormatString(symbol.ID));
            return builder.ToString();
        }

        public static string FormatBar(Symbol symbol, float slide)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_BAR);
            builder.Append(StringUtil.ToFormatString(symbol.ID));
            builder.Append(StringUtil.ToFormatString(slide));
            return builder.ToString();
        }

        public static string FormatColor(Color color)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_COLOR);
            builder.Append(StringUtil.ToFormatString(color));
            return builder.ToString();
        }

        public static string FormatBorder(Color color)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(FORMAT_BORDER);
            builder.Append(StringUtil.ToFormatString(color));
            return builder.ToString();
        }

        public void DrawChar(char chr, int i, Vector2 drawpos, TextParameters parameters)
        {
            Texture2D tex = Game.FontSprites[chr / FontUtil.CharsPerPage].Texture;

            int index = chr % FontUtil.CharsPerPage;
            int offset = FontUtil.GetCharOffset(chr);
            int width = FontUtil.GetCharWidth(chr);

            var color = parameters.Color(i);
            var border = parameters.Border(i);
            var charOffset = parameters.Offset(i);

            if (border.A > 0)
            { //Only draw outline if it's actually non-transparent
                SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset - 1, parameters.ScriptOffset + 0), FontUtil.GetCharRect(index), border);
                SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset, parameters.ScriptOffset + 1), FontUtil.GetCharRect(index), border);
                SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset, parameters.ScriptOffset - 1), FontUtil.GetCharRect(index), border);
                if (parameters.Bold)
                {
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset + 2, parameters.ScriptOffset + 0), FontUtil.GetCharRect(index), border);
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset + 1, parameters.ScriptOffset + 1), FontUtil.GetCharRect(index), border);
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset + 1, parameters.ScriptOffset - 1), FontUtil.GetCharRect(index), border);
                }
                else
                {
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset + 1, parameters.ScriptOffset), FontUtil.GetCharRect(index), border);
                }
            }

            SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset, parameters.ScriptOffset), FontUtil.GetCharRect(index), color);
            if (parameters.Bold)
                SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(-offset + 1, parameters.ScriptOffset), FontUtil.GetCharRect(index), color);
        }

        public void DrawText(string str, Vector2 drawpos, Alignment alignment, TextParameters parameters)
        {
            FontUtil.SetupString(str, drawpos, alignment, parameters, this);
        }
    }
}
