using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    public enum Alignment
    {
        Left,
        Center,
        Right,
    }

    public delegate Color TextColorFunction(int dialogIndex);
    public delegate Vector2 TextOffsetFunction(int dialogIndex);

    class TextParameters
    {
        public bool Bold;
        public TextColorFunction Color = (index) => Microsoft.Xna.Framework.Color.Black;
        public TextColorFunction Border = (index) => Microsoft.Xna.Framework.Color.Transparent;
        public TextOffsetFunction Offset = (index) => Vector2.Zero;
        public int DialogIndex = int.MaxValue;
        public int? MaxWidth = null;
        public int? MaxHeight = null;
        public int CharSeperator = 1;
        public int LineSeperator = 16;
        public int ScriptOffset = 0;
        internal bool Underline;

        public TextParameters SetBold(bool bold)
        {
            Bold = bold;
            return this;
        }

        public TextParameters SetUnderline(bool underline)
        {
            Underline = underline;
            return this;
        }

        public TextParameters SetColor(Color color, Color border)
        {
            Color = (index) => color;
            Border = (index) => border;
            return this;
        }

        public TextParameters SetColor(TextColorFunction color, TextColorFunction border)
        {
            Color = color;
            Border = border;
            return this;
        }

        public TextParameters SetOffset(TextOffsetFunction offset)
        {
            Offset = offset;
            return this;
        }

        public TextParameters SetDialogIndex(int dialogIndex)
        {
            DialogIndex = dialogIndex;
            return this;
        }

        public TextParameters SetConstraints(int maxWidth, int maxHeight)
        {
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            return this;
        }

        public TextParameters SetConstraints(Rectangle rect)
        {
            return SetConstraints(rect.Width, rect.Height);
        }

        public TextParameters SetSeperators(int charSeperator, int lineSeperator)
        {
            CharSeperator = charSeperator;
            LineSeperator = lineSeperator;
            return this;
        }

        public void Format(FormatToken format)
        {
            switch (format)
            {
                case (FormatToken.Bold):
                    Bold = !Bold;
                    break;
                case (FormatToken.Underline):
                    Underline = !Underline;
                    break;
                case (FormatToken.Subscript):
                    ScriptOffset += 8;
                    break;
                case (FormatToken.Superscript):
                    ScriptOffset -= 8;
                    break;
            }
        }

        public void Format(FormatCode code)
        {
            if (code is FormatCodeColor color)
            {
                if (color.Color != null)
                    Color = color.Color;
                if (color.Border != null)
                    Border = color.Border;
            }
        }

        public TextParameters Copy()
        {
            return new TextParameters()
            {
                Bold = Bold,
                Color = Color,
                Border = Border,
                Offset = Offset,
                DialogIndex = DialogIndex,
                MaxWidth = MaxWidth,
                MaxHeight = MaxHeight,
                CharSeperator = CharSeperator,
                LineSeperator = LineSeperator,
                ScriptOffset = ScriptOffset,
                Underline = Underline,
            };
        }
    }

    struct CharInfo
    {
        public int Offset;
        public int Width;
        public bool Predefined;

        public CharInfo(int offset, int width, bool predefined)
        {
            Offset = offset;
            Width = width;
            Predefined = predefined;
        }
    }

    class FormatCode
    {
        public int Width;

        public FormatCode(int width)
        {
            Width = width;
        }
    }

    class FormatCodeColor : FormatCode
    {
        public TextColorFunction Color;
        public TextColorFunction Border;

        public FormatCodeColor(TextColorFunction color, TextColorFunction border) : base(0)
        {
            Color = color;
            Border = border;
        }
    }

    class FormatCodeItemIcon : FormatCodeIcon
    {
        public int ObjectID;

        public FormatCodeItemIcon(int objectID)
        {
            ObjectID = objectID;
        }

        public override void Draw(Scene scene, Vector2 pos)
        {
            var holder = EffectManager.GetHolder(ObjectID);
            if (holder is Item item && scene is SceneGame sceneGame)
            {
                item.DrawIcon(sceneGame, pos + new Vector2(8, 8));
            }
        }
    }

    class FormatCodeSymbolIcon : FormatCodeIcon
    {
        public Symbol Symbol;

        public FormatCodeSymbolIcon(Symbol symbol)
        {
            Symbol = symbol;
        }

        public override void Draw(Scene scene, Vector2 pos)
        {
            Symbol.DrawIcon(scene, pos, 1);
        }
    }

    class FormatCodeBar : FormatCodeIcon
    {
        public Symbol Symbol;
        public float Slide;

        public FormatCodeBar(Symbol symbol, float slide)
        {
            Symbol = symbol;
            Slide = slide;
        }

        public override void Draw(Scene scene, Vector2 pos)
        {
            Symbol.DrawIcon(scene, pos, Slide);
        }
    }

    abstract class FormatCodeIcon : FormatCode
    {
        public FormatCodeIcon() : base(16)
        {
        }

        public abstract void Draw(Scene scene, Vector2 pos);
    }

    abstract class CharacterMachine
    {
        public abstract bool Done
        {
            get;
        }

        public abstract void Add(char chr);

        public abstract void Reset();

        public abstract FormatCode GetResult();

        public abstract string GetReplacement();
    }

    abstract class ColorMachine : CharacterMachine
    {
        protected StringBuilder colorBuffer = new StringBuilder(sizeof(int) / sizeof(char));

        public override bool Done => colorBuffer.Length >= sizeof(int) / sizeof(char);

        public override void Add(char chr)
        {
            colorBuffer.Append(chr);
        }

        public override void Reset()
        {
            colorBuffer.Clear();
        }

        public override string GetReplacement()
        {
            return new string(Game.FORMAT_BOLD, 1);
        }
    }

    class ColorFontMachine : ColorMachine
    {
        public override FormatCode GetResult()
        {
            Color color = colorBuffer.ToColor();
            return new FormatCodeColor(index => color, null);
        }
    }

    class ColorBorderMachine : ColorMachine
    {
        public override FormatCode GetResult()
        {
            Color color = colorBuffer.ToColor();
            return new FormatCodeColor(null, index => color);
        }
    }

    class IconMachine : CharacterMachine
    {
        protected StringBuilder symbolBuffer = new StringBuilder(sizeof(int) / sizeof(char));

        public override bool Done => symbolBuffer.Length >= sizeof(int) / sizeof(char);

        public override void Add(char chr)
        {
            symbolBuffer.Append(chr);
        }

        public override void Reset()
        {
            symbolBuffer.Clear();
        }

        public override FormatCode GetResult()
        {
            int objectID = symbolBuffer.ToInt32();
            return new FormatCodeItemIcon(objectID);
        }

        public override string GetReplacement()
        {
            return new string(Game.FORMAT_ICON, 1);
        }
    }

    class StatIconMachine : IconMachine
    {
        public override FormatCode GetResult()
        {
            int objectID = symbolBuffer.ToInt32();
            return new FormatCodeSymbolIcon(Stat.AllStats[objectID].Symbol);
        }

        public override string GetReplacement()
        {
            return new string(Game.FORMAT_ICON, 1);
        }
    }

    class ElementIconMachine : IconMachine
    {
        public override FormatCode GetResult()
        {
            int objectID = symbolBuffer.ToInt32();
            return new FormatCodeSymbolIcon(Element.AllElements[objectID].Symbol);
        }

        public override string GetReplacement()
        {
            return new string(Game.FORMAT_ICON, 1);
        }
    }

    class SymbolMachine : IconMachine
    {
        public override FormatCode GetResult()
        {
            int objectID = symbolBuffer.ToInt32();
            return new FormatCodeSymbolIcon(Symbol.AllSymbols[objectID]);
        }

        public override string GetReplacement()
        {
            return new string(Game.FORMAT_ICON, 1);
        }
    }

    class BarMachine : IconMachine
    {
        public override bool Done => symbolBuffer.Length >= sizeof(int) / sizeof(char) + sizeof(float) / sizeof(char);

        public override FormatCode GetResult()
        {
            int objectID = symbolBuffer.ToInt32();
            symbolBuffer.Remove(0, sizeof(int) / sizeof(char));
            float slide = symbolBuffer.ToSingle();
            return new FormatCodeBar(Symbol.AllSymbols[objectID], slide);
        }

        public override string GetReplacement()
        {
            return new string(Game.FORMAT_ICON, 1);
        }
    }

    enum FormatToken
    {
        Bold,
        Underline,
        Subscript,
        Superscript,
        Space,
        Newline,
    }

    class FontUtil
    {
        public class Gibberish
        {
            public Dictionary<int, List<char>> ByWidth;
            public Func<char, bool> CharSelection;

            public Gibberish(Func<char, bool> charSelection)
            {
                CharSelection = charSelection;
            }

            public char GetSimilar(char chr)
            {
                if (ByWidth == null)
                    ByWidth = Enumerable.Range(0, CharInfo.Length).Where(x => CharSelection((char)x)).GroupBy(x => CharInfo[x].Width).ToDictionary(x => x.Key, x => x.Select(y => (char)y).ToList());
                int width = GetCharWidth(chr);
                if (ByWidth.ContainsKey(width))
                    return ByWidth[width].Pick(Random);
                return chr;
            }
        }

        public const int CharWidth = 16;
        public const int CharHeight = 16;
        public const int CharsPerRow = 32;
        public const int CharsPerColumn = 32;
        public const int CharsPerPage = CharsPerColumn * CharsPerRow;

        public static CharInfo[] CharInfo = new CharInfo[65536];
        public static Gibberish GibberishStandard = new Gibberish(x => x > ' ' && x <= '~');
        public static Gibberish GibberishAlpha = new Gibberish(x => (x > ' ' && x <= '~') || (x > 160 && x <= 832));
        public static Gibberish GibberishQuery = new Gibberish(x => (x > 5120 && x <= 5760 - 128));
        public static Gibberish GibberishAlquimy = new Gibberish(x => (x > 40960 && x <= 40960 + 1024 + 128) || (x > 40960 + 1024 + 256 && x <= 40960 + 1024 + 256 + 256));
        public static Gibberish GibberishRune = new Gibberish(x => (x >= 6144 + 32 && x <= 6144 + 120 - 1) || (x >= 6144 + 128 && x <= 6144 + 128 + 32 + 10));
        public static Random Random = new Random();

        public static Dictionary<char, FormatCode> DynamicFormat = new Dictionary<char, FormatCode>();
        
        public static FormatCode GetFormatCode(char chr)
        {
            if (DynamicFormat.ContainsKey(chr))
                return DynamicFormat[chr];
            return null;
        }

        public static char GetSimilarChar(char chr, Gibberish gibberish)
        {
            if (Random.Next(2) > 0)
                return chr;
            return gibberish.GetSimilar(chr);
        }

        public static Rectangle GetCharRect(int index)
        {
            int x = index % CharsPerRow;
            int y = index / CharsPerRow;

            return new Rectangle(x * CharWidth, y * CharHeight, CharWidth, CharHeight);
        }

        public static int GetCharWidth(char chr)
        {
            if (DynamicFormat.ContainsKey(chr))
                return DynamicFormat[chr].Width;
            return CharInfo[chr].Width;
        }

        public static int GetCharOffset(char chr)
        {
            return CharInfo[chr].Offset;
        }

        public static void RegisterChar(Color[] blah, int width, int height, char chr, int index)
        {
            Rectangle rect = GetCharRect(index);

            int left = rect.Width - 1;
            int right = 0;
            bool empty = true;

            for (int x = rect.Left; x < rect.Right; x++)
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    if (blah[y * width + x].A > 0)
                    {
                        left = Math.Min(left, x - rect.Left);
                        right = Math.Max(right, x - rect.Left);
                        empty = false;
                    }
                }

            if (!CharInfo[chr].Predefined)
                CharInfo[chr] = new CharInfo(left, empty ? 0 : right - left + 1, false);
        }

        private static string CachedString;
        private static TextParameters CachedParameters;
        private static int CachedWidth;
        private static int CachedHeight;
        private static List<object> CachedTokens;

        static CharacterMachine ColorFontMachine = new ColorFontMachine();
        static CharacterMachine ColorBorderMachine = new ColorBorderMachine();
        static CharacterMachine IconMachine = new IconMachine();
        static CharacterMachine StatIconMachine = new StatIconMachine();
        static CharacterMachine ElementIconMachine = new ElementIconMachine();
        static CharacterMachine SymbolMachine = new SymbolMachine();
        static CharacterMachine BarMachine = new BarMachine();

        public static int GetStringWidth(string str, TextParameters parameters)
        {
            if (str != CachedString)
                SetupString(str, Vector2.Zero, Alignment.Center, parameters, null);
            return CachedWidth;
        }

        public static int GetStringHeight(string str, TextParameters parameters)
        {
            if (str != CachedString)
                SetupString(str, Vector2.Zero, Alignment.Center, parameters, null);
            return CachedHeight;
        }

        public delegate void DrawChar(char chr, Vector2 drawpos, TextParameters parameters);

        public static void SetupString(string str, Vector2 drawpos, Alignment alignment, TextParameters parameters, Game game)
        {
            bool cache = false;
            if (str != CachedString)
                cache = true;

            if (cache)
            {
                CachedString = str;
                CachedParameters = parameters;

                CachedWidth = 0;
                CachedHeight = 0;
            }

            parameters = CachedParameters.Copy();

            int getWidth(object token)
            {
                if (token is FormatToken format)
                {
                    if (format == FormatToken.Space)
                        return 4;
                    return 0;
                }
                if (token is string s)
                    return GetWordLength(s, parameters);
                if (token is FormatCode code)
                    return code.Width;
                return 0;
            }

            int width = 0;
            int maxwidth = parameters.MaxWidth ?? int.MaxValue;

            int y = 0;
            int offset;
            if (cache)
                CachedTokens = Tokenize(str);
            var tokens = new Stack<object>(CachedTokens.Reverse<object>());
            List<object> line = new List<object>();

            if (cache)
                CachedHeight = CharHeight;

            TextParameters lineParameters = parameters.Copy();

            int index = 0;

            while(tokens.Count > 0)
            {
                var token = tokens.Pop();
                int tokenWidth = getWidth(token);
                if (tokenWidth > maxwidth)
                {
                    if (token is string s && s.Length > 1 && maxwidth > 0)
                    {
                        var split = SplitWord(ref s, parameters.Copy(), maxwidth - width);
                        tokens.Push(s);
                        token = split;
                    }
                    else
                    {
                        break;
                    }
                }

                bool isNewLine = false;

                if (token is FormatCode code)
                {
                    parameters.Format(code);
                }
                else if(token is FormatToken format)
                {
                    parameters.Format(format);

                    if (format == FormatToken.Newline)
                        isNewLine = true;
                }

                if (width + tokenWidth > maxwidth || isNewLine)
                {
                    offset = (parameters.MaxWidth ?? 0) - width;
                    switch (alignment)
                    {
                        case (Alignment.Left):
                            offset = 0;
                            break;
                        case (Alignment.Center):
                            offset /= 2;
                            break;
                    }
                    if (game != null)
                        DrawLine(line, ref index, lineParameters, drawpos + new Vector2(offset, y), game);
                    line.Clear();
                    //Newline
                    y += parameters.LineSeperator;
                    if (cache)
                    {
                        CachedWidth = Math.Max(CachedWidth, width);
                        CachedHeight += parameters.LineSeperator;
                    }
                    width = 0;
                    if (!isNewLine)
                        tokens.Push(token);
                    continue;
                }

                width += tokenWidth;
                line.Add(token);
            }

            offset = (parameters.MaxWidth ?? 0) - width;
            switch (alignment)
            {
                case (Alignment.Left):
                    offset = 0;
                    break;
                case (Alignment.Center):
                    offset /= 2;
                    break;
            }
            if (game != null)
                DrawLine(line, ref index, lineParameters, drawpos + new Vector2(offset, y), game);
            if (cache)
                CachedWidth = Math.Max(CachedWidth, width);
            line.Clear();
        }

        private static void DrawLine(IEnumerable<object> tokens, ref int index, TextParameters parameters, Vector2 drawpos, Game game)
        {
            int x = 0;
            foreach(var token in tokens)
            {
                if(token is string str)
                {
                    foreach(var chr in str)
                    {
                        int width = GetCharWidth(chr) + parameters.CharSeperator + (parameters.Bold ? 1 : 0);
                        game.DrawChar(chr, index, drawpos + new Vector2(x, 0), parameters);
                        DrawCharLine(width, 16, index, parameters, drawpos + new Vector2(x, 0), game);
                        x += width;
                        index++;
                    }
                }
                if(token is FormatToken format)
                {
                    int width = format.GetWidth();
                    parameters.Format(format);
                    DrawCharLine(width, 16, index, parameters, drawpos + new Vector2(x, 0), game);
                    x += width;
                    index++;
                }
                if (token is FormatCode code)
                {
                    int width = code.Width;
                    parameters.Format(code);
                    
                    if (code is FormatCodeIcon icon)
                    {
                        icon.Draw(game.Scene, drawpos + new Vector2(x, 0));
                    }

                    DrawCharLine(width, 16, index, parameters, drawpos + new Vector2(x, 0), game);
                    x += width;
                    index++;
                }
            }
        }

        private static void DrawCharLine(int width, int yoffset, int i, TextParameters parameters, Vector2 drawpos, Game game)
        {
            var color = parameters.Color(i);
            var border = parameters.Border(i);
            var charOffset = parameters.Offset(i);

            game.Scene.SpriteBatch.Draw(game.Pixel, drawpos + charOffset + new Vector2(0, 15), new Rectangle(0, 0, width, 3), border);
            game.Scene.SpriteBatch.Draw(game.Pixel, drawpos + charOffset + new Vector2(0, 16), new Rectangle(0, 0, width, 1), color);
        }

        private static string SplitWord(ref string str, TextParameters parameters, int maxWidth)
        {
            int width = 0;
            StringBuilder builder = new StringBuilder();
            foreach (var chr in str)
            {
                int charWidth = GetCharWidth(chr) + parameters.CharSeperator + (parameters.Bold ? 1 : 0);
                if(width + charWidth > maxWidth)
                {
                    break;
                }
                builder.Append(chr);
                width += charWidth;
            }
            str = str.Remove(0, builder.Length);
            return builder.ToString();
        }

        private static int GetWordLength(string str, TextParameters parameters)
        {
            int width = 0;
            foreach (var chr in str)
            {
                width += GetCharWidth(chr) + parameters.CharSeperator + (parameters.Bold ? 1 : 0);
            }
            return width;
        }

        private static List<object> Tokenize(string str)
        {
            StringBuilder builder = new StringBuilder();
            List<object> tokens = new List<object>();
            CharacterMachine machine = null;

            void pushString()
            {
                if (builder.Length > 0)
                    tokens.Add(builder.ToString());
                builder.Clear();
            }

            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];

                if (machine != null)
                {
                    machine.Add(chr);
                    if (machine.Done)
                    {
                        tokens.Add(machine.GetResult());
                        machine = null;
                    }
                }
                else
                {
                    switch (chr)
                    {
                        case (Game.FORMAT_BOLD):
                            pushString();
                            tokens.Add(FormatToken.Bold);
                            break;
                        case (Game.FORMAT_UNDERLINE):
                            pushString();
                            tokens.Add(FormatToken.Underline);
                            break;
                        case (Game.FORMAT_SUBSCRIPT):
                            pushString();
                            tokens.Add(FormatToken.Subscript);
                            break;
                        case (Game.FORMAT_SUPERSCRIPT):
                            pushString();
                            tokens.Add(FormatToken.Superscript);
                            break;
                        case (' '):
                            pushString();
                            tokens.Add(FormatToken.Space);
                            break;
                        case ('\n'):
                            pushString();
                            tokens.Add(FormatToken.Newline);
                            break;
                        case (Game.FORMAT_COLOR):
                            pushString();
                            machine = ColorFontMachine;
                            machine.Reset();
                            break;
                        case (Game.FORMAT_BORDER):
                            pushString();
                            machine = ColorBorderMachine;
                            machine.Reset();
                            break;
                        case (Game.FORMAT_ICON):
                            pushString();
                            machine = IconMachine;
                            machine.Reset();
                            break;
                        case (Game.FORMAT_STAT_ICON):
                            pushString();
                            machine = StatIconMachine;
                            machine.Reset();
                            break;
                        case (Game.FORMAT_ELEMENT_ICON):
                            pushString();
                            machine = ElementIconMachine;
                            machine.Reset();
                            break;
                        case (Game.FORMAT_SYMBOL):
                            pushString();
                            machine = SymbolMachine;
                            machine.Reset();
                            break;
                        case (Game.FORMAT_BAR):
                            pushString();
                            machine = BarMachine;
                            machine.Reset();
                            break;
                        default:
                            builder.Append(chr);
                            break;
                    }
                }
            }

            pushString();

            return tokens;
        }
    }
}
