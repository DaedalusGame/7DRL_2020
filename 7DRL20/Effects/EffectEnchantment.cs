using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Effects
{
    class EffectEnchantment : Effect, IEffectContainer
    {
        public IEffectHolder Holder;
        public List<Effect> Effects = new List<Effect>();

        public EffectEnchantment()
        {
        }

        public EffectEnchantment(IEffectHolder holder)
        {
            Holder = holder;
        }

        public void Add(Effect effect)
        {
            effect.Type = EffectType.Transient;
            Effects.Add(effect);
        }

        public void AddApply(Effect effect)
        {
            effect.Apply();
            Add(effect);
        }

        public override void Apply()
        {
            EffectManager.AddEffect(Holder, this);
        }

        public virtual IEnumerable<T> GetSubEffects<T>() where T : Effect
        {
            return Effects.GetAndClean(x => x.Removed).SplitEffects<T>();
        }

        [Construct("enchantment")]
        public static EffectEnchantment Construct(Context context)
        {
            return new EffectEnchantment();
        }

        public override JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["holder"] = Serializer.GetHolderID(Holder);
            JArray effectsArray = new JArray();
            foreach(var effect in Effects.Where(x => !x.Removed))
            {
                var effectJson = effect.WriteJson();
                effectsArray.Add(effectJson);
            }
            json["effects"] = effectsArray;
            return json;
        }

        public override void ReadJson(JToken json, Context context)
        {
            Holder = Serializer.GetHolder<IEffectHolder>(json["holder"], context);
            foreach(var effectJson in json["effects"])
            {
                var effect = context.CreateEffect(effectJson);
                if (effect != null)
                {
                    effect.Apply();
                    Add(effect);
                }
            }
        }

        public class LevelUp : EffectEnchantment
        {
            public int Level;

            static bool RecursionLock;

            public LevelUp()
            {
            }

            public LevelUp(IEffectHolder holder, int level) : base(holder)
            {
                Level = level;
            }

            [Construct("levelup")]
            public static LevelUp Construct(Context context)
            {
                return new LevelUp();
            }

            public override IEnumerable<T> GetSubEffects<T>()
            {
                if (!RecursionLock && SafeGetLevel() >= Level)
                    return base.GetSubEffects<T>();
                else
                    return Enumerable.Empty<T>();
            }

            private double SafeGetLevel()
            {
                try
                {
                    RecursionLock = true; //For the express purpose of getting the Level, do not recurse into LevelUp effects.
                    return Holder.GetStat(Stat.Level);
                }
                finally
                {
                    RecursionLock = false; //Finally block guarantees that this will reset.
                }
            }

            public override JToken WriteJson()
            {
                JToken json = base.WriteJson();
                json["level"] = Level;
                return json;
            }

            public override void ReadJson(JToken json, Context context)
            {
                base.ReadJson(json, context);
                Level = json["level"].Value<int>();
            }
        }
    }
}
