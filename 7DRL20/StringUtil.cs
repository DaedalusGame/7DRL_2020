using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    static class StringUtil
    {
        public static int GetWidth(this FormatToken token)
        {
            switch(token)
            {
                case (FormatToken.Space):
                    return 4;
                default:
                    return 0;
            }
        }

        public static short ToInt16(this StringBuilder builder)
        {
            byte[] buffer = new byte[sizeof(Int16)];
            Encoding.Unicode.GetBytes(builder.ToString(), 0, sizeof(Int16) / sizeof(char), buffer, 0);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static int ToInt32(this StringBuilder builder)
        {
            byte[] buffer = new byte[sizeof(Int32)];
            Encoding.Unicode.GetBytes(builder.ToString(), 0, sizeof(Int32) / sizeof(char), buffer, 0);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static float ToSingle(this StringBuilder builder)
        {
            byte[] buffer = new byte[sizeof(Single)];
            Encoding.Unicode.GetBytes(builder.ToString(), 0, sizeof(Single) / sizeof(char), buffer, 0);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static Color ToColor(this StringBuilder builder)
        {
            unchecked
            {
                return new Color((uint)builder.ToInt32());
            }
        }

        public static string ToFormatString(short value)
        {
            return Encoding.Unicode.GetString(BitConverter.GetBytes(value));
        }

        public static string ToFormatString(int value)
        {
            return Encoding.Unicode.GetString(BitConverter.GetBytes(value));
        }

        public static string ToFormatString(float value)
        {
            return Encoding.Unicode.GetString(BitConverter.GetBytes(value));
        }

        public static string ToFormatString(Color color)
        {
            unchecked
            {
                return ToFormatString((int)color.PackedValue);
            }
        }
    }
}
