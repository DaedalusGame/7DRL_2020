using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Material : IEffectHolder
    {
        public class Part
        {
            public string Sprite;
            public List<Effect> Effects = new List<Effect>();

            public Part(string sprite)
            {
                Sprite = sprite;
            }

            public void AddEffect(Effect effect)
            {
                effect.Apply();
                Effects.Add(effect);
            }

            public IEnumerable<Effect> GetEffects()
            {
                return Effects.GetAndClean(effect => effect.Removed);
            }

            public static implicit operator Part(string sprite) => new Part(sprite); 
        }

        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public string Name;
        public string Description;

        public bool MeltingRequired = true;
        public double MeltingTemperature = double.PositiveInfinity;
        public double FuelTemperature = 0;

        public Part BladeBlade = "cut";
        public Part BladeGuard = "guard";
        public Part BladeHandle = "handle";

        public ColorMatrix ColorTransform = ColorMatrix.Identity;

        public Material(string name, string description)
        {
            ObjectID = EffectManager.NewID(this);
            Name = name;
            Description = description;
        }

        public void AddHeadEffect(Effect effect)
        {
            effect.Apply();
            BladeBlade.Effects.Add(effect);
        }

        public void AddHandleEffect(Effect effect)
        {
            effect.Apply();
            BladeGuard.Effects.Add(effect);
            BladeHandle.Effects.Add(effect);
        }

        public void AddFullEffect(Effect effect)
        {
            effect.Apply();
            BladeBlade.Effects.Add(effect);
            BladeGuard.Effects.Add(effect);
            BladeHandle.Effects.Add(effect);
        }

        public static Material None = new Material("None", string.Empty);
        public static Material Wood = new Wood();
        public static Material Bone = new Bone();
        public static Material Dilithium = new Dilithium(); //Fulminating Silver
        public static Material Tiberium = new Tiberium();
        public static Material Basalt = new Basalt();
        public static Material Triberium = new Triberium();
        public static Material Meteorite = new Meteorite();
        public static Material Obsidiorite = new Obsidiorite();
        public static Material Karmesine = new Karmesine();
        public static Material Ovium = new Ovium();
        public static Material Jauxum = new Jauxum();
    }

    class Wood : Material
    {
        public Wood() : base("Wood", string.Empty)
        {
            MeltingRequired = false;
            FuelTemperature = 100;
            ColorTransform = ColorMatrix.Tint(new Color(177, 135, 103));
            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Bone : Material
    {
        public Bone() : base("Bone", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColor(new Color(100, 92, 66), new Color(255, 255, 255));
            BladeBlade = "cleave";
            BladeGuard = "boneguard";
            BladeHandle = "bone";
            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Dilithium : Material
    {
        public Dilithium() : base("Dilithium", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(35*2, 86*2, 79*2), new Color(234, 252, 253));
            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
            AddFullEffect(new OnStartAttack(this, attack =>
            {
                //if (attack.Defender.IsUndead)
                {
                    attack.Damage *= 1.5f;
                }
            }));
        }
    }

    class Tiberium : Material
    {
        public Tiberium() : base("Tiberium", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(92, 156, 65), new Color(238, 251, 77));
            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Basalt : Material
    {
        public Basalt() : base("Basalt", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColor(new Color(89, 89, 89), new Color(239, 236, 233));

            BladeBlade = "cleave";
            BladeGuard = "binding";

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Triberium : Material
    {
        public Triberium() : base("Triberium", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(92, 156, 65), new Color(187, 253, 204));
            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Meteorite : Material
    {
        public Meteorite() : base("Meteorite", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(69, 75, 54), new Color(157, 167, 143));

            BladeBlade = "rip";

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Obsidiorite : Material
    {
        public Obsidiorite() : base("Obsidiorite", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(69 / 2, 54 / 2, 75 / 2), new Color(157, 143, 167));

            BladeBlade = "disembowel";
            BladeGuard = "binding";

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Karmesine : Material
    {
        public Karmesine() : base("Karmesine", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(198, 77, 55), new Color(242, 214, 208));

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Ovium : Material
    {
        public Ovium() : base("Ovium", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(94, 101, 170), new Color(215, 227, 253));

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Jauxum : Material
    {
        public Jauxum() : base("Jauxum", string.Empty)
        {
            ColorTransform = ColorMatrix.TwoColorLight(new Color(105, 142, 64), new Color(208, 251, 121));

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }
}
