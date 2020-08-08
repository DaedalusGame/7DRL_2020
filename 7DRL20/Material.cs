using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class ComboList<K, V>
    {
        List<V> ListAll = new List<V>();
        List<V> ListAny = new List<V>();
        MultiDict<K, V> Internal = new MultiDict<K, V>();

        public IEnumerable<V> Values => ListAny;

        public IEnumerable<V> this[K key]
        {
            get
            {
                return Internal.GetOrEmpty(key).Concat(ListAny);
            }
        }

        public void Add(V value)
        {
            ListAll.Add(value);
            ListAny.Add(value);
        }

        public void Add(IEnumerable<K> keys, V value)
        {
            ListAll.Add(value);
            foreach (var key in keys)
                Internal.Add(key, value);
        }
    }

    class Material : IEffectHolder
    {
        public class Part
        {
            private static IEnumerable<EquipSlot> AllSlots => (EquipSlot[])Enum.GetValues(typeof(EquipSlot));

            public string Sprite;
            public ComboList<EquipSlot, Effect> Effects = new ComboList<EquipSlot, Effect>();

            public Part(string sprite)
            {
                Sprite = sprite;
            }

            public void AddEffect(Effect effect)
            {
                Effects.Add(effect);
            }

            public void AddEffect(IEnumerable<EquipSlot> slots, Effect effect)
            {
                Effects.Add(slots, effect);
            }

            public IEnumerable<Effect> GetEffects(EquipSlot slot)
            {
                return Effects[slot];
            }

            public IEnumerable<Effect> GetEffects()
            {
                return Effects.Values;
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
        public double Priority;

        public bool MeltingRequired = true;
        public double MeltingTemperature = double.PositiveInfinity;
        public double FuelTemperature = 0;

        public bool CanMelt => !double.IsInfinity(MeltingTemperature);

        public Dictionary<PartType, Part> Parts = new Dictionary<PartType, Part>()
        {
            { ToolBlade.Blade, "cut" },
            { ToolBlade.Guard, "guard" },
            { ToolBlade.Handle, "handle" },
            { ToolAdze.Head, "pick" },
            { ToolAdze.Binding, "binding" },
            { ToolAdze.Handle, "handle" },
            { ToolPlate.Core, "layer1" },
            { ToolPlate.Composite, "layer2" },
            { ToolPlate.Trim, "layer3" },
        };

        public ColorMatrix ColorTransform = ColorMatrix.Identity;

        public Material(string name, string description)
        {
            ObjectID = EffectManager.NewID(this);
            Name = name;
            Description = description;
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public void AddEffect(PartType part, Effect effect)
        {
            effect.Apply();
            Parts[part].AddEffect(effect);
        }

        public void AddEffects(IEnumerable<PartType> parts, Effect effect)
        {
            foreach (var part in parts)
                Parts[part].AddEffect(effect);
        }

        public void AddEffects(IEnumerable<PartType> parts, Effect effect, IEnumerable<EquipSlot> slots)
        {
            foreach (var part in parts)
                Parts[part].AddEffect(slots, effect);
        }

        public void AddHeadEffect(Effect effect)
        {
            Parts[ToolBlade.Blade].AddEffect(effect);
            Parts[ToolAdze.Head].AddEffect(effect);
            Parts[ToolPlate.Composite].AddEffect(effect);
            Parts[ToolPlate.Trim].AddEffect(effect);
        }

        public void AddHandleEffect(Effect effect)
        {
            effect.Apply();
            Parts[ToolBlade.Guard].AddEffect(effect);
            Parts[ToolBlade.Handle].AddEffect(effect);
            Parts[ToolAdze.Binding].AddEffect(effect);
            Parts[ToolAdze.Handle].AddEffect(effect);
            Parts[ToolPlate.Core].AddEffect(effect);
        }

        public void AddFullEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
            AddEffects(ToolAdze.Parts, effect);
            AddEffects(ToolPlate.Parts, effect);
        }

        public void AddOffensiveEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
            AddEffects(ToolAdze.Parts, effect);
        }

        public void AddOffensiveHeadEffect(Effect effect)
        {
            effect.Apply();
            Parts[ToolBlade.Blade].AddEffect(effect);
            Parts[ToolAdze.Head].AddEffect(effect);
        }

        public void AddBladeEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
        }

        public void AddAdzeEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolAdze.Parts, effect);
        }

        public void AddPlateEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolPlate.Parts, effect);
        }

        public void AddArmorEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolPlate.Parts, effect, new[] { EquipSlot.Body });
        }

        public void AddShieldEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolPlate.Parts, effect, new[] { EquipSlot.Offhand });
        }

        public void AddOffensiveToolEffect(Effect effect)
        {
            effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
            AddEffects(ToolAdze.Parts, effect);
        }

        public virtual void MakeAlloy(Dictionary<Material, int> materials)
        {
            //NOOP;
        }

        public static Material None = new Material("None", string.Empty);
        public static Material Wood = new Wood();
        public static Material Coal = new Coal();
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
        public static Material Ardite = new Ardite();
        public static Material Cobalt = new Cobalt();
        public static Material Manyullyn = new Manyullyn();
        public static Material Terrax = new Terrax();
        public static Material Aurorium = new Aurorium();
        public static Material Violium = new Violium();
        public static Material Astrium = new Astrium();
        public static Material Ignitz = new Ignitz();
        public static Material Tritonite = new Tritonite();


        public static Material[] Alloys = new Material[] { Triberium, Terrax, Manyullyn, Violium, Astrium, Ignitz, Tritonite };
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

    class Coal : Material
    {
        public Coal() : base("Coal", string.Empty)
        {
            FuelTemperature = 1000;
            ColorTransform = ColorMatrix.Tint(new Color(103, 103, 103));
        }
    }

    class Bone : Material
    {
        public Bone() : base("Bone", string.Empty)
        {
            MeltingRequired = false;
            ColorTransform = ColorMatrix.TwoColor(new Color(100, 92, 66), new Color(255, 255, 255));
            Parts[ToolBlade.Blade] = "cleave";
            Parts[ToolBlade.Guard] = "boneguard";
            Parts[ToolBlade.Handle] = "bone";
            Parts[ToolAdze.Head] = "reap";
            Parts[ToolAdze.Binding] = "grip";

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Bludgeon, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Slash, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddOffensiveToolEffect(new EffectTrait(this, Trait.Splintering));
            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddAdzeEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.05));
        }
    }

    class Dilithium : Material
    {
        public Dilithium() : base("Dilithium", string.Empty)
        {
            FuelTemperature = 2000;
            MeltingTemperature = 75;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(35*2, 86*2, 79*2), new Color(234, 252, 253));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            //Weapons
            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddOffensiveToolEffect(new EffectTrait(this, Trait.Holy));

            //Plate
            AddPlateEffect(new EffectStatPercent(this, Element.Holy.DamageRate, -0.10));
            AddPlateEffect(new EffectStat(this, Stat.Defense, 3));
            AddShieldEffect(new EffectTrait(this, Trait.Spotlight));
        }
    }

    class Tiberium : Material
    {
        Random Random = new Random();

        public Tiberium() : base("Tiberium", string.Empty)
        {
            MeltingTemperature = 260;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(92, 156, 65), new Color(238, 251, 77));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.2));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddHandleEffect(new EffectStatPercent(this, Element.Thunder.DamageRate, -0.20));

            AddPlateEffect(new EffectStatPercent(this, Element.Thunder.DamageRate, -0.20));
            AddPlateEffect(new EffectStatPercent(this, Element.Fire.DamageRate, +0.40));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 20));
            AddFullEffect(new EffectTrait(this, Trait.Unstable));
        }
    }

    class Basalt : Material
    {
        public Basalt() : base("Basalt", string.Empty)
        {
            MeltingTemperature = 500;
            ColorTransform = ColorMatrix.TwoColor(new Color(89, 89, 89), new Color(239, 236, 233));

            Parts[ToolBlade.Blade] = "cleave";
            Parts[ToolBlade.Guard] = "binding";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.1));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Bludgeon, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 5));
            AddAdzeEffect(new EffectTrait(this, Trait.Softy));

            AddPlateEffect(new EffectTrait(this, Trait.FrothingBlast));
        }
    }

    class Triberium : Material
    {
        public Triberium() : base("Triberium", string.Empty)
        {
            Priority = 2;
            MeltingTemperature = 760;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(92, 156, 65), new Color(187, 253, 204));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.2));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 5));
            AddEffect(ToolAdze.Head, new EffectTrait(this, Trait.Fragile));
        }

        public override void MakeAlloy(Dictionary<Material, int> materials)
        {
            int basalt = materials.ContainsKey(Basalt) ? materials[Basalt] : 0;
            int tiberium = materials.ContainsKey(Tiberium) ? materials[Tiberium] : 0;
            int dilithium = materials.ContainsKey(Dilithium) ? materials[Dilithium] : 0;
            int minor = basalt + dilithium;
            int triberium = Math.Min(minor, tiberium);

            if (triberium > 0)
            {
                var result = new[] { basalt, dilithium }.ProportionalSplit(triberium);

                materials[Basalt] -= result[0];
                materials[Dilithium] -= result[1];
                materials[Tiberium] -= triberium;
                if (materials.ContainsKey(Triberium))
                    materials[Triberium] += triberium * 1;
                else
                    materials.Add(Triberium, triberium * 1);
            }
        }
    }

    class Meteorite : Material
    {
        public Meteorite() : base("Meteorite", string.Empty)
        {
            MeltingTemperature = 600;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(69, 75, 54), new Color(157, 167, 143));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.5));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.3));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddEffect(ToolAdze.Head, new EffectTrait(this, Trait.Crumbling));
            AddEffect(ToolAdze.Head, new EffectTrait(this, Trait.Pulverizing));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 15));

            AddShieldEffect(new EffectTrait(this, Trait.MeteorBash));
        }
    }

    class Obsidiorite : Material
    {
        public Obsidiorite() : base("Obsidiorite", string.Empty)
        {
            MeltingTemperature = 1100;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(69 / 2, 54 / 2, 75 / 2), new Color(157, 143, 167));

            Parts[ToolBlade.Blade] = "disembowel";
            Parts[ToolBlade.Guard] = "binding";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 0.7));
            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Pierce, 0.3));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent.Randomized(this, Stat.MiningSpeed, -0.45, 0.9));

            AddFullEffect(new EffectTrait(this, Trait.Alien));

            AddHandleEffect(new EffectStatPercent(this, Element.Fire.DamageRate, -0.10));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Karmesine : Material
    {
        public Karmesine() : base("Karmesine", string.Empty)
        {
            MeltingTemperature = 800;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(198, 77, 55), new Color(242, 214, 208));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.7));

            AddOffensiveEffect(new EffectStat(this, Stat.Attack, 10));
            AddOffensiveEffect(new EffectStatPercent(this, Stat.Attack, 0.2));
            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, -0.4));

            AddPlateEffect(new EffectStat(this, Stat.Defense, 10));
            AddFullEffect(new EffectStat(this, Element.Bludgeon.Resistance, 5));

            AddShieldEffect(new EffectStat(this, Stat.Defense, 5));
            AddShieldEffect(new EffectStat(this, Element.Bludgeon.Resistance, 10));

            AddOffensiveEffect(new EffectTrait(this, Trait.Sharp));
            AddArmorEffect(new EffectTrait(this, Trait.Stiff));
            AddShieldEffect(new EffectTrait(this, Trait.BloodShield));
        }
    }

    class Ovium : Material
    {
        public Ovium() : base("Ovium", string.Empty)
        {
            MeltingTemperature = 700;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(94, 101, 170), new Color(215, 227, 253));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.5));

            AddFullEffect(new EffectStat(this, Element.Slash.Resistance, 5));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.Attack, -0.4));
            AddHandleEffect(new EffectStatPercent(this, Stat.Defense, +0.2));

            
            AddFullEffect(new EffectTrait(this, Trait.Fuming));
        }
    }

    class Jauxum : Material
    {
        public Jauxum() : base("Jauxum", string.Empty)
        {
            MeltingTemperature = 550;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(105, 142, 64), new Color(208, 251, 121));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.HP, -0.2));

            AddFullEffect(new EffectStat(this, Element.Pierce.Resistance, 5));
            AddFullEffect(new EffectStatPercent(this, Element.Poison.DamageRate, -0.3));

            AddBladeEffect(new EffectTrait(this, Trait.Poxic));
            AddAdzeEffect(new EffectTrait(this, Trait.SlimeEater));
            AddPlateEffect(new EffectTrait(this, Trait.SludgeArmor));
        }
    }

    class Ardite : Material
    {
        public Ardite() : base("Ardite", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(215, 92, 11), new Color(252, 196, 112));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Cobalt : Material
    {
        public Cobalt() : base("Cobalt", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(61, 106, 143), new Color(63, 233, 233));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Manyullyn : Material
    {
        public Manyullyn() : base("Manyullyn", string.Empty)
        {
            Priority = 3;
            MeltingTemperature = 1900;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(96, 57, 174), new Color(222, 118, 248));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "reap";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));
            AddHandleEffect(new EffectStatPercent(this, Stat.Defense, 0.3));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }

        public override void MakeAlloy(Dictionary<Material, int> materials)
        {
            int ardite = materials.ContainsKey(Ardite) ? materials[Ardite] : 0;
            int cobalt = materials.ContainsKey(Cobalt) ? materials[Cobalt] : 0;

            int manyullyn = Math.Min(ardite, cobalt);

            if (manyullyn > 0)
            {
                materials[Ardite] -= manyullyn;
                materials[Cobalt] -= manyullyn;
                if (materials.ContainsKey(Manyullyn))
                    materials[Manyullyn] += manyullyn * 2;
                else
                    materials.Add(Manyullyn, manyullyn * 2);
            }
        }
    }

    class Terrax : Material
    {
        public Terrax() : base("Terrax", string.Empty)
        {
            Priority = 3;
            MeltingTemperature = 1900;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(82, 96, 101), new Color(254, 250, 222));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "reap";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));
            AddHandleEffect(new EffectStatPercent(this, Stat.Defense, 0.3));

            Random random = new Random();
            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
            //AddHeadEffect(new Trait(this, "Slaughtering", "More drops, but no experience."));
            AddHeadEffect(new EffectTrait(this, Trait.Slaughtering));

        }

        public override void MakeAlloy(Dictionary<Material, int> materials)
        {
            int karmesine = materials.ContainsKey(Karmesine) ? materials[Karmesine] : 0;
            int ovium = materials.ContainsKey(Ovium) ? materials[Ovium] : 0;
            int jauxum = materials.ContainsKey(Jauxum) ? materials[Jauxum] : 0;

            int terrax = Math.Min(Math.Min(karmesine, ovium), jauxum);

            if (terrax > 0)
            {
                materials[Karmesine] -= terrax;
                materials[Ovium] -= terrax;
                materials[Jauxum] -= terrax;
                if (materials.ContainsKey(Terrax))
                    materials[Terrax] += terrax * 3;
                else
                    materials.Add(Terrax, terrax * 3);
            }
        }
    }

    class Aurorium : Material
    {
        public Aurorium() : base("Aurorium", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(191, 51, 86), new Color(243, 209, 218));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Violium : Material
    {
        public Violium() : base("Violium", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(58, 50, 80), new Color(128, 168, 198));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Astrium : Material
    {
        public Astrium() : base("Astrium", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(112, 46, 81), new Color(179, 197, 225));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Ignitz : Material
    {
        public Ignitz() : base("Ignitz", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(188, 95, 92), new Color(255, 186, 26)) * ColorMatrix.Scale(1.3f);

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Tritonite : Material
    {
        public Tritonite() : base("Tritonite", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(23, 29, 96), new Color(85, 190, 196)) * ColorMatrix.Scale(1.2f);

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }
}
