using Microsoft.Xna.Framework;
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

    public class TextParameters
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
                item.DrawIcon(sceneGame, pos + new Vector2(8,8));
            }
        }
    }

    class FormatCodeElementIcon : FormatCodeIcon
    {
        public Element Element;

        public FormatCodeElementIcon(Element element)
        {
            Element = element;
        }

        public override void Draw(Scene scene, Vector2 pos)
        {
            scene.DrawSprite(Element.Sprite, 0, pos, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
    }

    class FormatCodeStatIcon : FormatCodeIcon
    {
        public Stat Stat;

        public FormatCodeStatIcon(Stat stat)
        {
            Stat = stat;
        }

        public override void Draw(Scene scene, Vector2 pos)
        {
            scene.DrawSprite(Stat.Sprite, 0, pos, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
    }


    abstract class FormatCodeIcon : FormatCode
    {
        public FormatCodeIcon() : base(16)
        {
        }

        public abstract void Draw(Scene scene, Vector2 pos);
    }

    class FontUtil
    {
        enum FormatState
        {
            None,
            Color,
            Border,
            Icon,
            ElementIcon,
            StatIcon,
        }

        public class Gibberish
        {
            public Dictionary<int, List<char>> ByWidth;
            public Func<char, bool> CharSelection;

            public Gibberish(Func<char,bool> charSelection)
            {
                CharSelection = charSelection;
            }

            public char GetSimilar(char chr)
            {
                if(ByWidth == null)
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
        public static Gibberish GibberishQuery = new Gibberish(x => (x > 5120 && x <= 5760-128));
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

        public static int GetStringLines(string str)
        {
            return (str.Count(x => x == '\n') + 1);
        }

        public static int GetStringWidth(string str, TextParameters parameters)
        {
            parameters = parameters.Copy();
            int maxn = 0;
            foreach (string line in str.Split('\n'))
            {
                int n = 0;

                foreach (char chr in line)
                {
                    int width = GetCharWidth(chr);
                    n += width;
                    if (width > 0)
                        n += parameters.CharSeperator + (parameters.Bold ? 1 : 0);

                    switch (chr)
                    {
                        case (Game.FORMAT_BOLD):
                            parameters.Bold = !parameters.Bold;
                            break;
                    }
                }

                if (n > maxn)
                    maxn = n;
            }

            return maxn;
        }

        public static int GetStringHeight(string str)
        {
            return GetStringLines(str) * CharHeight;
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
        
        public static string FormatText(string str)
        {
            StringBuilder builder = new StringBuilder();
            FormatState state = FormatState.None;
            DynamicFormat.Clear();
            char dynamicCode = Game.FORMAT_DYNAMIC_BEGIN;

            int indexObjectID = 0;
            byte[] bufferObjectID = new byte[sizeof(Int32)];

            int indexColor = 0;
            int[] bufferColor = new int[4];

            foreach (char c in str)
            {
                switch (state)
                {
                    case FormatState.None:
                        switch (c)
                        {
                            case (Game.FORMAT_COLOR):
                                state = FormatState.Color;
                                indexColor = 0;
                                break;
                            case (Game.FORMAT_BORDER):
                                state = FormatState.Border;
                                indexColor = 0;
                                break;
                            case (Game.FORMAT_ICON):
                                state = FormatState.Icon;
                                indexObjectID = 0;
                                break;
                            case (Game.FORMAT_ELEMENT_ICON):
                                state = FormatState.ElementIcon;
                                break;
                            case (Game.FORMAT_STAT_ICON):
                                state = FormatState.StatIcon;
                                break;
                            default:
                                builder.Append(c);
                                break;
                        }
                        break;
                    case FormatState.Color:
                    case FormatState.Border:
                        bufferColor[indexColor] = c;
                        indexColor++;
                        if (indexColor >= bufferColor.Length)
                        {
                            Color color = new Color(bufferColor[0], bufferColor[1], bufferColor[2], bufferColor[3]);
                            switch (state)
                            {
                                case FormatState.Color:
                                    builder.Append(dynamicCode);
                                    DynamicFormat.Add(dynamicCode++, new FormatCodeColor(index => color, null));
                                    break;
                                case FormatState.Border:
                                    builder.Append(dynamicCode);
                                    DynamicFormat.Add(dynamicCode++, new FormatCodeColor(null, index => color));
                                    break;
                            }
                            state = FormatState.None;
                        }
                        break;
                    case FormatState.Icon:
                        BitConverter.GetBytes(c).CopyTo(bufferObjectID, indexObjectID);
                        indexObjectID += sizeof(char);
                        if (indexObjectID >= bufferObjectID.Length)
                        {
                            int objectID = BitConverter.ToInt32(bufferObjectID, 0);
                            builder.Append(dynamicCode);
                            DynamicFormat.Add(dynamicCode++, new FormatCodeItemIcon(objectID));
                            state = FormatState.None;
                        }
                        break;
                    case FormatState.ElementIcon:
                        int elementID = (int)c;
                        Element element = Element.AllElements[elementID];
                        builder.Append(dynamicCode);
                        DynamicFormat.Add(dynamicCode++, new FormatCodeElementIcon(element));
                        state = FormatState.None;
                        break;
                    case FormatState.StatIcon:
                        int statID = (int)c;
                        Stat stat = Stat.AllStats[statID];
                        builder.Append(dynamicCode);
                        DynamicFormat.Add(dynamicCode++, new FormatCodeStatIcon(stat));
                        state = FormatState.None;
                        break;
                }
            }

            return builder.ToString();
        }

        public static string StripFormat(string str)
        {
            StringBuilder builder = new StringBuilder();
            FormatState state = FormatState.None;

            int indexColor = 0;
            int indexObjectID = 0;

            foreach (char c in str)
            {
                switch (state)
                {
                    case FormatState.None:
                        switch (c)
                        {
                            case (Game.FORMAT_COLOR):
                                state = FormatState.Color;
                                indexColor = 0;
                                break;
                            case (Game.FORMAT_BORDER):
                                state = FormatState.Border;
                                indexColor = 0;
                                break;
                            case (Game.FORMAT_ICON):
                                state = FormatState.Icon;
                                indexObjectID = 0;
                                break;
                            case (Game.FORMAT_ELEMENT_ICON):
                                state = FormatState.ElementIcon;
                                break;
                            case (Game.FORMAT_STAT_ICON):
                                state = FormatState.StatIcon;
                                break;
                            default:
                                builder.Append(c);
                                break;
                        }
                        break;
                    case FormatState.Color:
                    case FormatState.Border:
                        indexColor++;
                        if (indexColor >= 4)
                        {
                            builder.Append(Game.FORMAT_BOLD);
                            state = FormatState.None;
                        }
                        break;
                    case FormatState.Icon:
                        indexObjectID += sizeof(char);
                        if (indexObjectID >= sizeof(int))
                        {
                            builder.Append(Game.FORMAT_ICON);
                            state = FormatState.None;
                        }
                        break;
                    case FormatState.ElementIcon:
                        builder.Append(Game.FORMAT_ICON);
                        state = FormatState.None;
                        break;
                    case FormatState.StatIcon:
                        builder.Append(Game.FORMAT_ICON);
                        state = FormatState.None;
                        break;
                }
            }

            return builder.ToString();
        }

        public static string FitString(string str, TextParameters parameters)
        {
            parameters = parameters.Copy();
            int maxwidth = parameters.MaxWidth ?? int.MaxValue;
            int lastspot = 0;
            int idealspot = 0;
            int width = 0;
            string newstr = "";

            for (int i = 0; i < str.Length; i++)
            {
                char chr = str[i];
                var charWidth = GetCharWidth(chr) + parameters.CharSeperator + (parameters.Bold ? 1 : 0);
                if (charWidth > maxwidth)
                    return "";
                width += charWidth;

                switch (chr)
                {
                    case (Game.FORMAT_BOLD):
                        parameters.Bold = !parameters.Bold;
                        break;
                }

                if (chr == ' ')
                {
                    idealspot = i + 1;
                }

                if (chr == '\n')
                {
                    width = 0;
                }

                if (width > maxwidth)
                {
                    if (idealspot == lastspot)
                        idealspot = i;
                    string substr = str.Substring(lastspot, idealspot - lastspot);
                    newstr += substr.Trim() + "\n";
                    lastspot = idealspot;
                    i = idealspot - 1;
                    width = 0;
                }
            }

            newstr += str.Substring(lastspot, str.Length - lastspot);

            return newstr;
        }
    }
}
