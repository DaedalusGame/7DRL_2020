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
            Parts[part].Effects.Add(effect);
        }

        public void AddHeadEffect(Effect effect)
        {
            effect.Apply();
            Parts[ToolBlade.Blade].Effects.Add(effect);
            Parts[ToolAdze.Head].Effects.Add(effect);
            Parts[ToolPlate.Composite].Effects.Add(effect);
            Parts[ToolPlate.Trim].Effects.Add(effect);
        }

        public void AddHandleEffect(Effect effect)
        {
            effect.Apply();
            Parts[ToolBlade.Guard].Effects.Add(effect);
            Parts[ToolBlade.Handle].Effects.Add(effect);
            Parts[ToolAdze.Binding].Effects.Add(effect);
            Parts[ToolAdze.Handle].Effects.Add(effect);
            Parts[ToolPlate.Core].Effects.Add(effect);
        }

        public void AddFullEffect(Effect effect)
        {
            effect.Apply();
            Parts[ToolBlade.Blade].Effects.Add(effect);
            Parts[ToolBlade.Guard].Effects.Add(effect);
            Parts[ToolBlade.Handle].Effects.Add(effect);
            Parts[ToolAdze.Head].Effects.Add(effect);
            Parts[ToolAdze.Binding].Effects.Add(effect);
            Parts[ToolAdze.Handle].Effects.Add(effect);
            Parts[ToolPlate.Core].Effects.Add(effect);
            Parts[ToolPlate.Composite].Effects.Add(effect);
            Parts[ToolPlate.Trim].Effects.Add(effect);
        }

        public void AddBladeEffect(Effect effect)
        {
            effect.Apply();
            foreach(var part in ToolBlade.Parts)
                Parts[part].Effects.Add(effect);
        }

        public void AddAdzeEffect(Effect effect)
        {
            effect.Apply();
            foreach (var part in ToolAdze.Parts)
                Parts[part].Effects.Add(effect);
        }

        public void AddPlateEffect(Effect effect)
        {
            effect.Apply();
            foreach (var part in ToolPlate.Parts)
                Parts[part].Effects.Add(effect);
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
        public static Material Terrax = new Terrax();

        public static Material[] Alloys = new Material[] { Triberium, Terrax };
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
            AddFullEffect(new Trait(this, "Splintering", "Deals some damage to surrounding enemies."));
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
            AddFullEffect(new OnStartAttack(this, attack =>
            {
                var isUndead = attack.Defender.HasStatusEffect(x => x is Undead);
                if (isUndead)
                {
                    attack.Damage *= 1.5f;
                }
            }));
            AddFullEffect(new Trait(this, "Holy", "Extra damage to undead."));
        }
    }

    class Tiberium : Material
    {
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

            Random random = new Random();
            AddFullEffect(new EffectStat(this, Stat.Attack, 20));
            AddFullEffect(new Trait(this, "Unstable", "Causes random explosions."));
            AddFullEffect(new OnAttack(this, attack =>
            {
                var attacker = attack.Attacker;
                if (random.NextDouble() < 0.3 && attack.Defender is Creature defender)
                {
                    new FireExplosion(defender.World, new Vector2(defender.X * 16 + 8, defender.Y * 18 + 8), Vector2.Zero, 15);
                    //attacker.TakeDamage(5, Element.Fire);
                    //defender.TakeDamage(5, Element.Fire);
                }
            }));
            AddFullEffect(new OnMine(this, (mine) =>
            {
                if (mine.Success && random.NextDouble() < 0.3 && mine.Mineable is Tile tile)
                {
                    new FireExplosion(mine.Miner.World, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8), Vector2.Zero, 15);
                    //mine.Miner.TakeDamage(5, Element.Fire);
                }
            }));
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 5));
            AddAdzeEffect(new Trait(this, "Softy", "Breaking rock restores some HP."));
            AddAdzeEffect(new OnMine(this, (mine) =>
            {
                if (mine.Mineable is Tile tile)
                {
                    if (mine.RequiredMiningLevel <= 1 && mine.Success)
                        mine.Miner.Heal(5);
                }
            }));
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 5));
            AddEffect(ToolAdze.Head, new Trait(this, "Fragile", "Cracks nearby rock."));
            AddEffect(ToolAdze.Head, new OnMine(this, (mine) =>
            {
                if(mine.Mineable is Tile tile)
                {
                    if (mine.ReactionLevel > 0 || !mine.Success)
                        return;
                    foreach(var neighbor in tile.GetAdjacentNeighbors().OfType<IMineable>())
                    {
                        if(MineEvent.Random.NextDouble() < 0.7)
                        {
                            MineEvent fracture = new MineEvent(mine.Miner, mine.Pickaxe)
                            {
                                ReactionLevel = mine.ReactionLevel + 1
                            };
                            neighbor.Mine(fracture);
                            fracture.Start();
                        }
                    }
                }
            }));
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

            AddEffect(ToolAdze.Head, new Trait(this, "Crumbling", "Destroys lower level rock faster."));
            AddEffect(ToolAdze.Head, new Trait(this, "Pulverizing", "No mining drops."));
            AddEffect(ToolAdze.Head, new OnStartMine(this, (mine) =>
            {
                var miningLevel = mine.Miner.GetStat(Stat.MiningLevel);
                double delta = miningLevel - mine.ReactionLevel;
                mine.Speed *= (1 + 0.15 * Math.Max(delta,0));
                mine.LootFunction = (miner) => { };
            }));

            AddFullEffect(new EffectStat(this, Stat.Attack, 15));
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

            AddFullEffect(new EffectStat.Randomized(this, Stat.Attack, -5, 20));
            AddFullEffect(new EffectStatPercent.Randomized(this, Stat.MiningSpeed, -0.5, 2.0));
            AddFullEffect(new Trait(this, "Alien", "Randomize stats."));

            AddHandleEffect(new EffectStatPercent(this, Element.Fire.DamageRate, -0.10));

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.Attack, 0.2));
            AddHandleEffect(new EffectStatPercent(this, Stat.MiningSpeed, -0.4));

            AddFullEffect(new EffectStat(this, Element.Bludgeon.Resistance, 5));

            AddHeadEffect(new Trait(this, "Sharp", "Causes bleeding."));
            AddHeadEffect(new OnStartAttack(this, attack =>
            {
                attack.StatusEffects.Add(new Bleeding() { Buildup = 0.3, Duration = new Slider(20) });
            }));
            AddFullEffect(new Trait(this, "Stiff", "Reduce damage taken."));
            AddFullEffect(new OnStartDefend(this, attack =>
            {
                attack.Defender.AddStatusEffect(new DefenseUp() { Buildup = 0.4, Duration = new Slider(10) });
            }));
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.Attack, -0.4));
            AddHandleEffect(new EffectStatPercent(this, Stat.Defense, +0.2));

            Random random = new Random();
            AddFullEffect(new Trait(this, "Fuming", "Sometimes produces smoke cloud."));
            AddFullEffect(new OnAttack(this, attack =>
            {
                var subject = attack.Defender;
                if(random.NextDouble() < 0.2 && subject is Creature creature)
                new Smoke(creature.World, new Vector2(creature.X * 16 + 8, creature.Y * 18 + 8), Vector2.Zero, 15);
            }));
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

            AddFullEffect(new EffectStat(this, Stat.Attack, 10));
            AddHandleEffect(new EffectStatPercent(this, Stat.HP, -0.2));

            AddFullEffect(new EffectStat(this, Element.Pierce.Resistance, 5));
            AddFullEffect(new EffectStatPercent(this, Poison.Element.DamageRate, -0.3));

            AddFullEffect(new Trait(this, "Poxic", "Sometimes turns enemies into slime."));
            AddFullEffect(new OnStartAttack(this, attack =>
            {
                attack.StatusEffects.Add(new Slimed(attack.Attacker) { Buildup = 0.4 });
            }));
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
            AddFullEffect(new EffectStat(this, Stat.Attack, 30));
            //AddHeadEffect(new Trait(this, "Slaughtering", "More drops, but no experience."));
            AddHeadEffect(new Trait(this, "Slaughtering", "Sometimes steal life on attack."));
            AddHeadEffect(new OnAttack(this, attack =>
            {
                var totalDamage = attack.FinalDamage.Sum(dmg => dmg.Value);
                if (random.NextDouble() < 0.5 && totalDamage > 0)
                {
                    attack.Attacker.Heal(totalDamage * 0.2);
                }
            }));
        }

        public override void MakeAlloy(Dictionary<Material, int> materials)
        {
            int karmesine = materials.ContainsKey(Karmesine) ? materials[Karmesine] : 0;
            int ovium = materials.ContainsKey(Ovium) ? materials[Ovium] : 0;
            int jauxum = materials.ContainsKey(Jauxum) ? materials[Jauxum] : 0;

            int terrax = Math.Min(Math.Min(karmesine, ovium), jauxum);

            if(terrax > 0)
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
}
