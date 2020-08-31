using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    [SerializeInfo("trait")]
    class EffectTrait : Effect, IEffectContainer
    {
        public IEffectHolder Holder;
        public Trait Trait;
        public int Level;

        public override double VisualPriority => 1000;

        public EffectTrait()
        {
        }

        public EffectTrait(IEffectHolder holder, Trait trait, int level = 1)
        {
            Holder = holder;
            Trait = trait;
            Level = level;
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public override bool StatEquals(Effect other)
        {
            return other is EffectTrait trait && trait.Trait == Trait;
        }

        public override int GetStatHashCode()
        {
            return Trait.GetHashCode();
        }

        public override void AddStatBlock(ref string statBlock, IEnumerable<Effect> equalityGroup)
        {
            int totalLevel = equalityGroup.OfType<EffectTrait>().Sum(trait => trait.Level);
            statBlock += $"{Game.FORMAT_BOLD}{Game.FormatColor(Trait.Color)}{Trait.Name}{Game.FormatColor(Color.White)}{Game.FORMAT_BOLD} Lv{totalLevel}\n";
            string description = string.Join(string.Empty, Trait.Description.Split('\n').Select(str => $"- {str}\n"));
            statBlock += description;
        }

        public IEnumerable<T> GetSubEffects<T>() where T : Effect
        {
            return Trait.GetEffects<T>();
        }

        [Construct]
        public static EffectTrait Construct(Context context)
        {
            return new EffectTrait();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            json["trait"] = Trait.ID;
            json["level"] = Level;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            Trait = Trait.GetTrait(json["trait"].Value<string>());
            Level = json["level"].Value<int>();
        }
    }
}
