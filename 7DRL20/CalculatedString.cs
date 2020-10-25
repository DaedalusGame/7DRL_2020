using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    delegate object[] GetParametersDelegate();

    abstract class CalculatedString
    {
        public abstract string Get();

        public static implicit operator string(CalculatedString s) => s.Get();
        public static implicit operator CalculatedString(string s) => new StaticString(s);
    }

    class StaticString : CalculatedString
    {
        string String;

        public StaticString(string str) : base()
        {
            String = str;
        }

        public override string Get()
        {
            return String;
        }
    }

    class DynamicString : CalculatedString
    {
        Func<string> String;

        public DynamicString(Func<string> str) : base()
        {
            String = str;
        }

        public override string Get()
        {
            return String();
        }
    }

    class FormattedString : CalculatedString
    {
        string Format;
        GetParametersDelegate GetParameters;

        public FormattedString(string format, GetParametersDelegate getParameters)
        {
            Format = format;
            GetParameters = getParameters;
        }

        public override string Get()
        {
            return string.Format(Format, GetParameters());
        }
    }
}
