﻿using Microsoft.Xna.Framework;
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
        SpriteReference[] FontSprites = new SpriteReference[FontSpritesAmount];

        FrameCounter FPS = new FrameCounter();
        FrameCounter GFPS = new FrameCounter();

        public Game()
        {
            Graphics = new GraphicsDeviceManager(this);
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
                yield return twinState;
                previous = next;
                textBuffer.Clear();
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

            RenderTarget2D pixel = new RenderTarget2D(GraphicsDevice, 1, 1);
            GraphicsDevice.SetRenderTarget(pixel);
            GraphicsDevice.Clear(Color.White);
            GraphicsDevice.SetRenderTarget(null);
            Pixel = pixel;

            LoadFont();

            Scene = new SceneGame(this);

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
                FontSprites[i].ShouldLoad = true;
                int fontIndex = i;
                FontSprites[i].SetLoadFunction(() => LoadFontPart(FontSprites[fontIndex], fontIndex));
            }

            FontUtil.CharInfo[' '] = new CharInfo(0, 4, true);
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

        public void DrawText(string str, Vector2 drawpos, Alignment alignment, TextParameters parameters)
        {
            parameters = parameters.Copy();
            int lineoffset = 0;
            int totalindex = 0;
            str = FontUtil.FitString(str, parameters);

            foreach (string line in str.Split('\n'))
            {
                if (lineoffset + parameters.LineSeperator > parameters.MaxHeight)
                    break;
                int textwidth = FontUtil.GetStringWidth(line, parameters);
                int offset = (parameters.MaxWidth ?? 0) - textwidth;
                switch (alignment)
                {
                    case (Alignment.Left):
                        offset = 0;
                        break;
                    case (Alignment.Center):
                        offset /= 2;
                        break;
                }
                DrawTextLine(line, drawpos + new Vector2(offset, lineoffset), totalindex, parameters);
                totalindex += line.Length;
                lineoffset += parameters.LineSeperator;
            }
        }

        private void DrawTextLine(string str, Vector2 drawpos, int totalindex, TextParameters parameters)
        {
            int pos = 0;

            foreach (char chr in str)
            {
                if (totalindex > parameters.DialogIndex)
                    break;
                char chrTrue = chr;
                switch (chr)
                {
                    case (FORMAT_BOLD):
                        parameters.Bold = !parameters.Bold;
                        break;
                    case (FORMAT_UNDERLINE):
                        parameters.Underline = !parameters.Underline;
                        break;
                    case (FORMAT_SUBSCRIPT):
                        parameters.ScriptOffset += 8;
                        break;
                    case (FORMAT_SUPERSCRIPT):
                        parameters.ScriptOffset -= 8;
                        break;
                    case (FORMAT_ICON):
                        break;
                    default:
                        //chrTrue = FontUtil.GetSimilarChar(chr,FontUtil.GibberishStandard);
                        break;
                }

                Texture2D tex = FontSprites[chrTrue / FontUtil.CharsPerPage].Texture;
                int index = chrTrue % FontUtil.CharsPerPage;
                int offset = FontUtil.GetCharOffset(chrTrue);
                int width = FontUtil.GetCharWidth(chrTrue);

                var color = parameters.Color(totalindex);
                var border = parameters.Border(totalindex);
                var charOffset = parameters.Offset(totalindex);

                if (border.A > 0)
                { //Only draw outline if it's actually non-transparent
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset - 1, parameters.ScriptOffset + 0), FontUtil.GetCharRect(index), border);
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset, parameters.ScriptOffset + 1), FontUtil.GetCharRect(index), border);
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset, parameters.ScriptOffset - 1), FontUtil.GetCharRect(index), border);
                    if (parameters.Bold)
                    {
                        SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset + 2, parameters.ScriptOffset + 0), FontUtil.GetCharRect(index), border);
                        SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset + 1, parameters.ScriptOffset + 1), FontUtil.GetCharRect(index), border);
                        SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset + 1, parameters.ScriptOffset - 1), FontUtil.GetCharRect(index), border);
                    }
                    else
                    {
                        SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset + 1, parameters.ScriptOffset), FontUtil.GetCharRect(index), border);
                    }
                }

                SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset, parameters.ScriptOffset), FontUtil.GetCharRect(index), color);
                if (parameters.Bold)
                    SpriteBatch.Draw(tex, drawpos + charOffset + new Vector2(pos - offset + 1, parameters.ScriptOffset), FontUtil.GetCharRect(index), color);

                if (chr == FORMAT_ICON)
                {
                    //parameters.Icons[parameters.IconIndex].Draw(drawpos + charOffset + new Vector2(pos - offset, parameters.ScriptOffset) + new Vector2(8, 8));
                    //parameters.IconIndex++;
                }

                pos += width + parameters.CharSeperator + (parameters.Bold ? 1 : 0);
                totalindex++;
            }
        }
    }
}
