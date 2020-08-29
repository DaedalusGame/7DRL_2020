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
            public ComboList<EquipSlot, Effect> ItemEffects = new ComboList<EquipSlot, Effect>();

            public Part(string sprite)
            {
                Sprite = sprite;
            }

            public void AddItemEffect(Effect effect)
            {
                ItemEffects.Add(effect);
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

            public IEnumerable<Effect> GetItemEffects()
            {
                return ItemEffects.Values;
            }

            public static implicit operator Part(string sprite) => new Part(sprite); 
        }

        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public string ID;
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
            { ToolArrow.Tip, "tip" },
            { ToolArrow.Limb, "shaft" },
            { ToolArrow.Fletching, "fletching" },
        };
        public HashSet<PartType> ValidParts;

        public ColorMatrix ColorTransform = ColorMatrix.Identity;

        public Material(string id, string name, string description)
        {
            ID = id;
            Materials.Add(ID, this);
            ObjectID = EffectManager.SetID(this);
            Name = name;
            Description = description;
            ValidParts = Parts.Keys.ToHashSet();
            ValidParts.Remove(ToolArrow.Fletching);
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public void AddDurability(double durability, double handleMod, double extraBonus)
        {
            foreach(var part in Parts)
            {
                part.Value.AddItemEffect(new EffectStat(this, Stat.Durability, durability * part.Key.DurabilityMod));
                if(part.Key.Shape == PartShape.Handle)
                    part.Value.AddItemEffect(new EffectStatMultiply(this, Stat.Durability, handleMod));
                if (part.Key.Shape == PartShape.Extra)
                    part.Value.AddItemEffect(new EffectStatPercent(this, Stat.Durability, extraBonus));
            }
        }

        public void AddEffect(PartType part, Effect effect)
        {
            //effect.Apply();
            Parts[part].AddEffect(effect);
        }

        public void AddItemEffect(PartType part, Effect effect)
        {
            //effect.Apply();
            Parts[part].AddItemEffect(effect);
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
            //effect.Apply();
            Parts[ToolBlade.Blade].AddEffect(effect);
            Parts[ToolAdze.Head].AddEffect(effect);
            Parts[ToolPlate.Composite].AddEffect(effect);
            Parts[ToolPlate.Trim].AddEffect(effect);
        }

        public void AddHandleEffect(Effect effect)
        {
            //effect.Apply();
            Parts[ToolBlade.Guard].AddEffect(effect);
            Parts[ToolBlade.Handle].AddEffect(effect);
            Parts[ToolAdze.Binding].AddEffect(effect);
            Parts[ToolAdze.Handle].AddEffect(effect);
            Parts[ToolPlate.Core].AddEffect(effect);
        }

        public void AddFullEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
            AddEffects(ToolAdze.Parts, effect);
            AddEffects(ToolPlate.Parts, effect);
        }

        public void AddOffensiveEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
            AddEffects(ToolAdze.Parts, effect);
        }

        public void AddOffensiveHeadEffect(Effect effect)
        {
            //effect.Apply();
            Parts[ToolBlade.Blade].AddEffect(effect);
            Parts[ToolAdze.Head].AddEffect(effect);
        }

        public void AddBladeEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
        }

        public void AddAdzeEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolAdze.Parts, effect);
        }

        public void AddPlateEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolPlate.Parts, effect);
        }

        public void AddArmorEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolPlate.Parts, effect, new[] { EquipSlot.Body });
        }

        public void AddShieldEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolPlate.Parts, effect, new[] { EquipSlot.Offhand });
        }

        public void AddOffensiveToolEffect(Effect effect)
        {
            //effect.Apply();
            AddEffects(ToolBlade.Parts, effect);
            AddEffects(ToolAdze.Parts, effect);
        }

        public virtual void MakeAlloy(Dictionary<Material, int> materials)
        {
            //NOOP;
        }

        public bool IsPartValid(PartType partType)
        {
            return ValidParts.Contains(partType);
        }

        public static Material GetMaterial(string id)
        {
            return Materials.GetOrDefault(id, None);
        }

        public static Material None = new Material("none", "None", string.Empty);
        public static Material Wood = new Wood();
        public static Material Coal = new Coal();
        public static Material Bone = new Bone();
        public static Material Feather = new Feather();

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
        public Wood() : base("wood", "Wood", string.Empty)
        {
            MeltingRequired = false;
            FuelTemperature = 100;
            ColorTransform = ColorMatrix.Tint(new Color(177, 135, 103));

            AddDurability(50, 1.5, 0);

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
        }
    }

    class Feather : Material
    {
        public Feather() : base("feather", "Feather", string.Empty)
        {
            MeltingRequired = false;
            ColorTransform = ColorMatrix.Tint(new Color(255, 255, 255));

            ValidParts = new HashSet<PartType>()
            {
                ToolArrow.Fletching,
            };
        }
    }

    class Coal : Material
    {
        public Coal() : base("coal", "Coal", string.Empty)
        {
            FuelTemperature = 1000;
            ColorTransform = ColorMatrix.Tint(new Color(103, 103, 103));
        }
    }

    class Bone : Material
    {
        public Bone() : base("bone", "Bone", string.Empty)
        {
            MeltingRequired = false;
            ColorTransform = ColorMatrix.TwoColor(new Color(100, 92, 66), new Color(255, 255, 255));
            Parts[ToolBlade.Blade] = "cleave";
            Parts[ToolBlade.Guard] = "boneguard";
            Parts[ToolBlade.Handle] = "bone";
            Parts[ToolAdze.Head] = "reap";
            Parts[ToolAdze.Binding] = "grip";
            Parts[ToolArrow.Tip] = "small";

            AddDurability(80, 1.5, 0);

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Bludgeon, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Slash, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddOffensiveToolEffect(new EffectTrait(this, Trait.Splintering));
            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddAdzeEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.05));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Dark, 0.5));

            AddArmorEffect(new EffectTrait(this, Trait.Undead));
        }
    }

    class Dilithium : Material
    {
        public Dilithium() : base("dilithium", "Dilithium", string.Empty)
        {
            FuelTemperature = 2000;
            MeltingTemperature = 75;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(35*2, 86*2, 79*2), new Color(234, 252, 253));
            Parts[ToolArrow.Tip] = "small";

            AddDurability(50, 0.5, 0.1);

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            //Weapons
            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddOffensiveToolEffect(new EffectTrait(this, Trait.Holy));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Holy, 0.5));

            //Plate
            AddPlateEffect(new EffectStatPercent(this, Element.Holy.DamageRate, -0.10));
            AddPlateEffect(new EffectStat(this, Stat.Defense, 3));
            AddShieldEffect(new EffectTrait(this, Trait.Spotlight));
        }
    }

    class Tiberium : Material
    {
        Random Random = new Random();

        public Tiberium() : base("tiberium", "Tiberium", string.Empty)
        {
            MeltingTemperature = 260;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(92, 156, 65), new Color(238, 251, 77));
            Parts[ToolArrow.Tip] = "bomb";

            AddDurability(50, 0.5, 0.1);

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.2));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddHandleEffect(new EffectStatPercent(this, Element.Thunder.DamageRate, -0.20));

            AddPlateEffect(new EffectStatPercent(this, Element.Thunder.DamageRate, -0.20));
            AddPlateEffect(new EffectStatPercent(this, Element.Fire.DamageRate, +0.40));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Bludgeon, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 20));
            AddFullEffect(new EffectTrait(this, Trait.Unstable));
            AddEffect(ToolArrow.Tip, new EffectTrait(this, Trait.Discharge));
            AddEffect(ToolArrow.Limb, new EffectTrait(this, Trait.Charged));
            //Shield: Reactive - Explode on hit, deals damage in facing direction
            //Arrow: Charge - Arcs to nearby enemies in flight
            //Arrow: Discharge - Explodes into lightning on impact
        }
    }

    class Basalt : Material
    {
        public Basalt() : base("basalt", "Basalt", string.Empty)
        {
            MeltingTemperature = 500;
            ColorTransform = ColorMatrix.TwoColor(new Color(89, 89, 89), new Color(239, 236, 233));

            AddDurability(50, 0.5, 0.1);

            Parts[ToolBlade.Blade] = "cleave";
            Parts[ToolBlade.Guard] = "binding";
            Parts[ToolAdze.Head] = "sledge";
            Parts[ToolArrow.Tip] = "bomb";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.1));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Bludgeon, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 5));
            AddAdzeEffect(new EffectTrait(this, Trait.Softy));

            AddPlateEffect(new EffectTrait(this, Trait.FrothingBlast));
        }
    }

    class Triberium : Material
    {
        public Triberium() : base("triberium", "Triberium", string.Empty)
        {
            Priority = 2;
            MeltingTemperature = 760;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(92, 156, 65), new Color(187, 253, 204));

            AddDurability(100, 1.0, 0);

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.2));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 5));
            AddEffect(ToolAdze.Head, new EffectTrait(this, Trait.Fragile));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Wind, 0.5));
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
        public Meteorite() : base("meteorite", "Meteorite", string.Empty)
        {
            MeltingTemperature = 600;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(69, 75, 54), new Color(157, 167, 143));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddDurability(500, 0.6, 0.1);

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.5));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.3));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 15));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 1.0));

            //Sword: Geddon - Extra damage to Man-made targets
            AddEffect(ToolAdze.Head, new EffectTrait(this, Trait.Crumbling));
            AddEffect(ToolAdze.Head, new EffectTrait(this, Trait.Pulverizing));
            AddShieldEffect(new EffectTrait(this, Trait.MeteorBash));
            //Armor: 
            //Arrow:
        }
    }

    class Obsidiorite : Material
    {
        public Obsidiorite() : base("obsidiorite", "Obsidiorite", string.Empty)
        {
            MeltingTemperature = 1100;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(69 / 2, 54 / 2, 75 / 2), new Color(157, 143, 167));

            Parts[ToolBlade.Blade] = "disembowel";
            Parts[ToolBlade.Guard] = "binding";
            Parts[ToolAdze.Head] = "sledge";

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 0.7));
            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Pierce, 0.3));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 1));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.5));

            AddFullEffect(new EffectTrait(this, Trait.Alien));

            AddHandleEffect(new EffectStatPercent(this, Element.Fire.DamageRate, -0.10));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 1.0));
        }
    }

    class Karmesine : Material
    {
        public Karmesine() : base("karmesine", "Karmesine", string.Empty)
        {
            MeltingTemperature = 800;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(198, 77, 55), new Color(242, 214, 208));
            Parts[ToolArrow.Tip] = "fork";

            AddDurability(100, 1, 0);

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.7));

            AddOffensiveEffect(new EffectStat(this, Stat.Attack, 10));
            AddOffensiveEffect(new EffectStatPercent(this, Stat.Attack, 0.2));
            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, -0.4));

            AddPlateEffect(new EffectStat(this, Stat.Defense, 10));
            AddFullEffect(new EffectStat(this, Element.Bludgeon.Resistance, 5));

            AddShieldEffect(new EffectStat(this, Stat.Defense, 5));
            AddShieldEffect(new EffectStat(this, Element.Bludgeon.Resistance, 10));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 1.0));

            AddOffensiveEffect(new EffectTrait(this, Trait.Sharp));
            AddArmorEffect(new EffectTrait(this, Trait.Stiff));
            AddShieldEffect(new EffectTrait(this, Trait.BloodShield));
            //Arrow: Heartripper - Bleed on impact. Deal damage to enemies behind impact location.
        }
    }

    class Ovium : Material
    {
        public Ovium() : base("ovium", "Ovium", string.Empty)
        {
            MeltingTemperature = 700;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(94, 101, 170), new Color(215, 227, 253));
            Parts[ToolArrow.Tip] = "bomb";

            AddDurability(100, 1, 0);

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.5));

            AddFullEffect(new EffectStat(this, Element.Slash.Resistance, 5));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Bludgeon, 1.0));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.Attack, -0.4));
            AddHandleEffect(new EffectStatPercent(this, Stat.Defense, +0.2));

            
            AddFullEffect(new EffectTrait(this, Trait.Fuming));
            //Adze: Steam Injection - Extra ore drop for ores that melt at low temperatures
            //Armor: 
            //Shield: 
            //Arrow: Smoke Bolt - Create smoke cloud on impact. Create poison/acid smoke in certain cases.
        }
    }

    class Jauxum : Material
    {
        public Jauxum() : base("jauxum", "Jauxum", string.Empty)
        {
            MeltingTemperature = 550;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(105, 142, 64), new Color(208, 251, 121));
            Parts[ToolArrow.Tip] = "small";

            AddDurability(100, 1, 0);

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 2));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.HP, -0.2));

            AddFullEffect(new EffectStat(this, Element.Pierce.Resistance, 5));
            AddFullEffect(new EffectStatPercent(this, Element.Poison.DamageRate, -0.3));

            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolArrow.Tip, new EffectElement(this, Element.Poison, 0.5));

            AddBladeEffect(new EffectTrait(this, Trait.Poxic));
            AddAdzeEffect(new EffectTrait(this, Trait.SlimeEater));
            AddPlateEffect(new EffectTrait(this, Trait.SludgeArmor));
            //Shield: 
            //Arrow: Poison Bolt - Poison on impact
        }
    }

    class Ardite : Material
    {
        public Ardite() : base("ardite", "Ardite", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(215, 92, 11), new Color(252, 196, 112));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Cobalt : Material
    {
        public Cobalt() : base("cobalt", "Cobalt", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(61, 106, 143), new Color(63, 233, 233));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Manyullyn : Material
    {
        public Manyullyn() : base("manyullyn", "Manyullyn", string.Empty)
        {
            Priority = 3;
            MeltingTemperature = 1900;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(96, 57, 174), new Color(222, 118, 248));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "reap";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

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
        public Terrax() : base("terrax", "Terrax", string.Empty)
        {
            Priority = 3;
            MeltingTemperature = 1900;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(82, 96, 101), new Color(254, 250, 222));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "reap";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Pierce, 0.5));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 0.5));

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
        public Aurorium() : base("aurorium", "Aurorium", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(191, 51, 86), new Color(243, 209, 218));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";
            Parts[ToolArrow.Tip] = "small";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Violium : Material
    {
        public Violium() : base("violium", "Violium", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(58, 50, 80), new Color(128, 168, 198));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Astrium : Material
    {
        public Astrium() : base("astrium", "Astrium", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(112, 46, 81), new Color(179, 197, 225));

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Ignitz : Material
    {
        public Ignitz() : base("ignitz", "Ignitz", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(188, 95, 92), new Color(255, 186, 26)) * ColorMatrix.Scale(1.3f);

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";
            Parts[ToolArrow.Tip] = "bomb";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }

    class Tritonite : Material
    {
        public Tritonite() : base("tritonite", "Tritonite", string.Empty)
        {
            MeltingTemperature = 2000;
            ColorTransform = ColorMatrix.TwoColorLight(new Color(23, 29, 96), new Color(85, 190, 196)) * ColorMatrix.Scale(1.2f);

            Parts[ToolBlade.Blade] = "rip";
            Parts[ToolAdze.Head] = "sledge";
            Parts[ToolArrow.Tip] = "fork";

            AddEffect(ToolAdze.Head, new EffectStat(this, Stat.MiningLevel, 3));
            AddEffect(ToolAdze.Head, new EffectStatPercent(this, Stat.MiningSpeed, 0.3));

            AddItemEffect(ToolBlade.Blade, new EffectElement(this, Element.Slash, 1.0));
            AddItemEffect(ToolAdze.Head, new EffectElement(this, Element.Bludgeon, 1.0));

            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, 0.15));

            AddOffensiveToolEffect(new EffectStat(this, Stat.Attack, 30));
        }
    }
}
