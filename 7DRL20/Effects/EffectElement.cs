using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("element")]
    class EffectElement : Effect
    {
        public IEffectHolder Holder;
        public Element Element
        {
            get;
            set;
        }
        public virtual double Percentage
        {
            get;
            set;
        }

        public override double VisualPriority => -10;

        public EffectElement()
        {
        }

        public EffectElement(IEffectHolder holder, Element element, double percentage)
        {
            Holder = holder;
            Element = element;
            Percentage = percentage;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override void Remove()
        {
            base.Remove();
        }

        public override string ToString()
        {
            return $"{Element} {Percentage * 100:+0;-#}% ({Holder})";
        }

        public override bool StatEquals(Effect other)
        {
            return other is EffectElement element && element.Element == Element;
        }

        public override int GetStatHashCode()
        {
            return Element.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            var total = equalityGroup.OfType<EffectElement>().Sum(element => element.Percentage);
            statBlock += $"{Game.FormatElement(Element)} {Game.FORMAT_BOLD}{Element}{Game.FORMAT_BOLD} {((int)Math.Round(total * 100)).ToString("0;-#")}%\n";
        }

        [Construct]
        public static EffectElement Construct(Context context)
        {
            return new EffectElement();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["element"] = Element.Id;
            json["percentage"] = Percentage;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Element = Element.GetElement(json["element"].Value<string>());
            Percentage = json["percentage"].Value<double>();
        }
    }
}
