using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class AdvancedColor
    {
        public static AdvancedColor Empty = new AdvancedColor(new Color[0], 1);

        List<Color> Colors = new List<Color>();
        float Time;
        
        public AdvancedColor(JToken json)
        {
            ReadJson(json);
        }

        public AdvancedColor(IEnumerable<Color> colors, float time)
        {
            Colors.AddRange(colors);
            Time = time;
        }

        public Color GetColor(float time)
        {
            float timeSlide = (time % Time) / Time;
            if (Colors.Count == 0)
                return Color.TransparentBlack;
            if (Colors.Count == 1)
                return Colors[0];

            int count = Colors.Count - 1;
            float slide = timeSlide % (1.0f / count);
            int index = Math.Min((int)Math.Floor(timeSlide * count), count);

            return Color.Lerp(Colors[index], Colors[index + 1], slide);
        }

        public JToken WriteJson()
        {
            JObject json = new JObject();
            JArray colorsJson = new JArray();
            foreach(var color in Colors)
            {
                colorsJson.Add(Util.WriteColor(color));
            }
            json["colors"] = colorsJson;
            json["time"] = Time;
            return json;
        }

        public void ReadJson(JToken json)
        {
            JArray colorsJson = json["colors"] as JArray;
            foreach (var colorJson in colorsJson)
            {
                Colors.Add(Util.ReadColor(colorJson));
            }
            Time = json["time"].Value<float>();
        }
    }
}
