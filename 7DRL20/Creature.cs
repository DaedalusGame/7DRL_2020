using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.Events;
using RoguelikeEngine.Traits;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class Element
    {
        public static List<Element> AllElements = new List<Element>();

        public static void Init()
        {
            var sorted = AllElements.OrderBy(element => element.Priority);
            int index = 0;
            foreach(Element element in sorted)
            {
                element.Priority = index++;
            }
        }

        public int Index;
        public string ID;
        public string Name;
        public Symbol Symbol;
        public Stat Resistance;
        public Stat DamageRate;

        public double Priority;

        public string FormatString => $"{Game.FormatElement(this)}{Name}";

        public Element(string id, string name, Symbol symbol)
        {
            Index = AllElements.Count;
            ID = id;
            Name = name;
            Symbol = symbol;
            Priority = Index;
            Resistance = new ElementStat(this, "resistance", $"{Name} Resistance", 0, 4, 0, SpriteLoader.Instance.AddSprite("content/stat_element_defense"));
            DamageRate = new ElementStat(this, "damage_rate", $"{Name} Damage Rate", 1, 4, 0.5, SpriteLoader.Instance.AddSprite("content/stat_element_rate"))
            {
                Format = Stat.FormatRate,
            };
            AllElements.Add(this);
        }

        public Element(string id, string name, SpriteReference sprite) : this(id, name, new Symbol(sprite))
        {
        }

        public virtual bool CanSplit()
        {
            return false;
        }

        public virtual IDictionary<Element, double> Split()
        {
            return null;
        }

        public override string ToString()
        {
            return Name;
        }

        public static Element GetElement(string id)
        {
            return AllElements.Find(element => element.ID == id);
        }

        public static Element Bludgeon = new Element("bludgeon", "Bludgeon", SpriteLoader.Instance.AddSprite("content/element_blunt"));
        public static Element Slash = new Element("slash", "Slash", SpriteLoader.Instance.AddSprite("content/element_slice"));
        public static Element Pierce = new Element("pierce", "Pierce", SpriteLoader.Instance.AddSprite("content/element_pierce"));

        public static Element Fire = new Element("fire", "Fire", SpriteLoader.Instance.AddSprite("content/element_fire"));
        public static Element Ice = new Element("ice", "Ice", SpriteLoader.Instance.AddSprite("content/element_ice"));
        public static Element Thunder = new Element("thunder", "Thunder", SpriteLoader.Instance.AddSprite("content/element_thunder"));
        public static Element Water = new Element("water", "Water", SpriteLoader.Instance.AddSprite("content/element_water"));
        public static Element Wind = new Element("wind", "Wind", SpriteLoader.Instance.AddSprite("content/element_wind"));
        public static Element Earth = new Element("earth", "Earth", SpriteLoader.Instance.AddSprite("content/element_earth"));
        public static Element Holy = new Element("holy", "Holy", SpriteLoader.Instance.AddSprite("content/element_holy"));
        public static Element Dark = new Element("dark", "Dark", SpriteLoader.Instance.AddSprite("content/element_dark"));

        //Status Elements
        public static Element Bleed = new Element("bleed", "Bleed", SpriteLoader.Instance.AddSprite("content/element_blood"));
        public static Element Poison = new Element("poison", "Poison", SpriteLoader.Instance.AddSprite("content/element_poison"));
        public static Element Acid = new Element("acid", "Acid", SpriteLoader.Instance.AddSprite("content/element_acid"));

        //Combination Elements
        public static Element Light = new ElementCombined("light", "Light", SpriteLoader.Instance.AddSprite("content/element_light"), new Dictionary<Element, double>()
        {
            { Thunder, 0.7 },
            { Fire, 0.7 }
        });
        public static Element Hellfire = new ElementCombined("hellfire", "Hellfire", SpriteLoader.Instance.AddSprite("content/element_hellfire"), new Dictionary<Element, double>()
        {
            { Holy, 0.7 },
            { Fire, 0.7 }
        });
        public static Element Drought = new ElementCombined("drought", "Drought", SpriteLoader.Instance.AddSprite("content/element_drought"), new Dictionary<Element, double>()
        {
            { Water, 1.0 },
            { Ice, 1.0 }
        });
        public static Element BlackFlame = new ElementCombined("black_flame", "Black Flame", SpriteLoader.Instance.AddSprite("content/element_black_flame"), new Dictionary<Element, double>()
        {
            { Fire, 0.5 },
            { Dark, 1.5 }
        });
        public static Element RedDevil = new ElementCombined("red_devil", "Red Devil", SpriteLoader.Instance.AddSprite("content/element_red_devil"), new Dictionary<Element, double>()
        {
            { Fire, 1.5 },
            { Dark, 0.5 }
        });
        public static Element Darkness = new ElementCombined("darkness", "Darkness", SpriteLoader.Instance.AddSprite("content/element_darkness"), new Dictionary<Element, double>()
        {
            { Ice, 0.8 },
            { Dark, 0.8 }
        });
        public static Element Emperor = new ElementCombined("emperor", "Emperor", SpriteLoader.Instance.AddSprite("content/element_emperor"), new Dictionary<Element, double>()
        {
            { Thunder, 1.5 },
            { Dark, 0.5 }
        });
        public static Element HeavenThunder = new ElementCombined("heavenly_thunder", "Heavenly Thunder", SpriteLoader.Instance.AddSprite("content/element_heaven_thunder"), new Dictionary<Element, double>()
        {
            { Thunder, 1.5 },
            { Holy, 0.5 }
        });
        public static Element ThunderPole = new ElementCombined("thunder_pole", "Thunder Pole", SpriteLoader.Instance.AddSprite("content/element_thunderpole"), new Dictionary<Element, double>()
        {
            { Thunder, 0.7 },
            { Earth, 0.7 }
        });
        public static Element Steam = new ElementCombined("steam", "Steam", SpriteLoader.Instance.AddSprite("content/element_steam"), new Dictionary<Element, double>()
        {
            { Water, 0.6 },
            { Fire, 0.4 }
        });
        public static Element Sand = new ElementCombined("sand", "Sand", SpriteLoader.Instance.AddSprite("content/element_sand"), new Dictionary<Element, double>()
        {
            { Earth, 0.5 },
            { Wind, 0.5 }
        });
        public static Element Mud = new ElementCombined("mud", "Mud", SpriteLoader.Instance.AddSprite("content/element_mud"), new Dictionary<Element, double>()
        {
            { Water, 0.5 },
            { Earth, 0.5 }
        });
        public static Element Permafrost = new ElementCombined("permafrost", "Permafrost", SpriteLoader.Instance.AddSprite("content/element_permafrost"), new Dictionary<Element, double>()
        {
            { Ice, 0.6 },
            { Earth, 0.6 }
        });
        public static Element Storm = new ElementCombined("storm", "Storm", SpriteLoader.Instance.AddSprite("content/element_storm"), new Dictionary<Element, double>()
        {
            { Water, 0.6 },
            { Wind, 0.6 }
        });
        public static Element Magma = new ElementCombined("magma", "Magma", SpriteLoader.Instance.AddSprite("content/element_magma"), new Dictionary<Element, double>()
        {
            { Earth, 0.4 },
            { Fire, 0.8 }
        });
        public static Element Metal = new ElementCombined("metal", "Metal", SpriteLoader.Instance.AddSprite("content/element_metal"), new Dictionary<Element, double>()
        {
            { Earth, 0.6 },
            { Holy, 0.4 }
        });
        public static Element Inferno = new ElementCombined("inferno", "Inferno", SpriteLoader.Instance.AddSprite("content/element_inferno"), new Dictionary<Element, double>()
        {
            { Wind, 0.5 },
            { Fire, 0.5 }
        });
        public static Element Blizzard = new ElementCombined("blizzard", "Blizzard", SpriteLoader.Instance.AddSprite("content/element_blizzard"), new Dictionary<Element, double>()
        {
            { Wind, 0.5 },
            { Ice, 0.5 }
        });
        public static Element Arcane = new ElementCombined("arcane", "Arcane", SpriteLoader.Instance.AddSprite("content/element_arcane"), new Dictionary<Element, double>()
        {
            { Dark, 0.5 },
            { Holy, 0.5 }
        });
        public static Element Origin = new ElementCombined("origin", "Origin", SpriteLoader.Instance.AddSprite("content/element_origin"), new Dictionary<Element, double>()
        {
            { BlackFlame, 0.2 },
            { Darkness, 0.2 },
            { RedDevil, 0.2 },
            { Light, 0.2 },
            { HeavenThunder, 0.2 },
            { Drought, 0.2 },
        });
        public static Element Chaos = new ElementRandom("chaos", "Chaos", SpriteLoader.Instance.AddSprite("content/element_demon"), new List<Element>()
        {
            Fire,
            Ice,
            Water,
            Thunder,
            Earth,
            Wind,
            Dark,
            Holy
        },1);
        public static Element TheEnd = new ElementCombined("the_end", "The End", SpriteLoader.Instance.AddSprite("content/element_the_end"), new Dictionary<Element, double>()
        {
            { Dark, 2.0 },
            { Holy, 0.3 }
        });

        public static Element Healing = new Element("healing", "Healing", SpriteLoader.Instance.AddSprite("content/element_healing"));

        public static Element[] PhysicalElements = new Element[] { Bludgeon, Slash, Pierce };
        public static Element[] MagicalElements = new Element[] { Fire, Ice, Thunder, Water, Wind, Earth, Holy, Dark };
        public static Element[] PrimalElements = new Element[] { Fire, Ice, Thunder, Acid, Poison };
    }

    class ElementCombined : Element
    {
        Dictionary<Element, double> Composites = new Dictionary<Element, double>();

        public ElementCombined(string id, string name, SpriteReference sprite, Dictionary<Element, double> composites) : base(id, name, sprite)
        {
            Composites = composites;
        }

        public override bool CanSplit()
        {
            return true;
        }

        public override IDictionary<Element, double> Split()
        {
            return Composites;
        }
    }

    class ElementRandom : Element
    {
        Random Random = new Random();
        List<Element> Composites = new List<Element>();
        double Total;

        public ElementRandom(string id, string name, SpriteReference sprite, List<Element> composites, double total) : base(id, name, sprite)
        {
            Composites = composites;
            Total = total;
        }

        public override bool CanSplit()
        {
            return true;
        }

        public override IDictionary<Element, double> Split()
        {
            var pick = Composites.Shuffle(Random).Take(Random.Next(1, Composites.Count));
            var rates = pick.ToDictionary(x => x, x => Random.Next(10) + 1);
            var total = rates.Sum(x => x.Value);
            return rates.ToDictionary(x => x.Key, x => x.Value * Total / total);
        }
    }

    class Stat
    {
        public static List<Stat> AllStats = new List<Stat>();

        public static void Init()
        {
            var sorted = AllStats.OrderBy(stat => stat.EffectivePriority);
            int index = 0;
            foreach (Stat stat in sorted)
            {
                stat.Priority = index++;
            }
        }

        public int Index;
        public string ID;
        public string Name;
        public bool Hidden;
        public double DefaultStat;
        public Symbol Symbol;
        public Func<Creature, double, string> Format = (creature, value) => value.ToString();

        public string FormatString => $"{Game.FormatStat(this)}{Name}";

        public double Priority;
        public virtual double EffectivePriority => Priority;

        public Stat(string id, string name, double defaultStat, double priority, Symbol symbol)
        {
            Index = AllStats.Count;
            ID = id;
            Name = name;
            DefaultStat = defaultStat;
            Symbol = symbol;
            Priority = priority;
            AllStats.Add(this);
        }

        public Stat(string id, string name, double defaultStat, double priority, SpriteReference sprite) : this(id, name, defaultStat, priority, new Symbol(sprite))
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public static Stat GetStat(string id)
        {
            return AllStats.Find(stat => stat.ID == id);
        }

        public static string FormatHP(Creature creature, double value)
        {
            return $"{creature.CurrentHP}/{value}";
        }

        public static string FormatRate(Creature creature, double value)
        {
            if (value < 0)
                return $"x{-value} Invert";
            else
                return $"x{value}";
        }

        public static Stat HP = new Stat("hp", "HP", 0, 0, SpriteLoader.Instance.AddSprite("content/stat_hp"))
        {
            Format = FormatHP,
        };
        public static Stat Attack = new Stat("attack", "Attack", 0, 1, SpriteLoader.Instance.AddSprite("content/stat_attack"));
        public static Stat Defense = new Stat("defense", "Defense", 0, 2, SpriteLoader.Instance.AddSprite("content/stat_defense"));
        public static Stat DamageRate = new Stat("damage_rate", "Damage Rate", 1, 3, SpriteLoader.Instance.AddSprite("content/stat_damage_rate"))
        {
            Format = FormatRate,
        };
        public static Stat AlchemyPower = new Stat("alchemy", "Alchemy Power", 0, 5, SpriteLoader.Instance.AddSprite("content/stat_alchemy"));
        public static Stat MiningLevel = new Stat("mining_level", "Mining Level", 0, 6, SpriteLoader.Instance.AddSprite("content/stat_mining_level"));
        public static Stat MiningSpeed = new Stat("mining_speed", "Mining Speed", 1, 7, SpriteLoader.Instance.AddSprite("content/stat_mining_speed"));
        public static Stat Speed = new Stat("speed", "Speed", 1, 8, SpriteLoader.Instance.AddSprite("content/stat_speed"));

        public static Stat ArrowAttack = new Stat("arrow_attack", "Arrow Attack", 0, 10, SpriteLoader.Instance.AddSprite("content/stat_arrow_attack"));
        public static Stat ArrowRange = new Stat("arrow_range", "Arrow Range", 0, 11, SpriteLoader.Instance.AddSprite("content/stat_arrow_range"));
        public static Stat ArrowVolley = new Stat("arrow_volley", "Arrow Volley", 1, 12, SpriteLoader.Instance.AddSprite("content/stat_arrow_volley"));

        public static Stat Durability = new Stat("durability", "Durability", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_durability")) { Hidden = true };
        public static Stat Blood = new Stat("blood", "Blood", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_blood"));

        public static Stat SlimeHP = new Stat("slime_hp", "Slime HP", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_slime_hp"));
        public static Stat SlimeAttack = new Stat("slime_attack", "Slime Attack", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_slime_attack"));
        
        public static Flag SwapItem = new Flag("swap_item", "Swap Item", true, SpriteLoader.Instance.AddSprite("content/stat_swap_enabled"));
        public static Flag EquipItem = new Flag("equip_item", "Equip Item", true, SpriteLoader.Instance.AddSprite("content/stat_equip_enabled"));
        public static Flag UnequipItem = new Flag("unequip_item", "Unequip Item", true, SpriteLoader.Instance.AddSprite("content/stat_unequip_enabled"));
        public static Flag LightningRod = new Flag("lightning_rod", "Lightning Rod", true, SpriteLoader.Instance.AddSprite("content/stat_lightning_rod"));
        public static Flag Insulation = new Flag("insulation", "Insulation", true, SpriteLoader.Instance.AddSprite("content/stat_insulation"));

        public static Stat[] Stats = new Stat[] { HP, Attack, Defense, AlchemyPower, DamageRate };
    }

    class ElementStat : Stat
    {
        Element Element;

        public double SubPriority;
        public override double EffectivePriority => Priority + (Element.Priority + SubPriority) / Element.AllElements.Count;

        public ElementStat(Element element, string id, string name, double defaultStat, double priority, double subPriority, Symbol symbol) : base($"{id}_{element.ID}", name, defaultStat, priority, symbol)
        {
            Element = element;
            SubPriority = subPriority;
        }

        public ElementStat(Element element, string id, string name, double defaultStat, double priority, double subPriority, SpriteReference sprite) : this(element, id, name, defaultStat, priority, subPriority, new SymbolElement(sprite, element))
        {
        }
    }

    class Flag : Stat
    {
        public bool DefaultValue;
        
        public Flag(string id, string name, bool defaultValue, SpriteReference sprite) : base(id, name, 0, -1, sprite)
        {
            DefaultValue = defaultValue;
        }
    }

    class Mask : IEnumerable<Point>
    {
        HashSet<Point> PointLookup = new HashSet<Point>();
        List<Point> PointList = new List<Point>();

        public void Add(Point point)
        {
            PointLookup.Add(point);
            PointList.Add(point);
        }

        public void Remove(Point point)
        {
            PointLookup.Remove(point);
            PointList.Remove(point);
        }

        public bool Contains(Point point)
        {
            return PointLookup.Contains(point);
        }

        public Rectangle GetRectangle()
        {
            return GetRectangle(0, 0);
        }

        public Rectangle GetRectangle(int xOff, int yOff)
        {
            int xMin = 0;
            int xMax = 0;
            int yMin = 0;
            int yMax = 0;
            foreach(Point point in PointList)
            {
                if (point.X < xMin)
                    xMin = point.X;
                if (point.Y < yMin)
                    yMin = point.Y;
                if (point.X > xMax)
                    xMax = point.X;
                if (point.Y > yMax)
                    yMax = point.Y;
            }
            return new Rectangle(xOff + xMin, yOff + yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        public IEnumerable<Point> GetFrontier()
        {
            HashSet<Point> frontier = new HashSet<Point>();

            foreach(Point point in PointList)
            {
                frontier.Add(new Point(point.X + 1, point.Y));
                frontier.Add(new Point(point.X - 1, point.Y));
                frontier.Add(new Point(point.X, point.Y + 1));
                frontier.Add(new Point(point.X, point.Y - 1));
            }

            return frontier.Except(PointLookup);
        }

        public IEnumerable<Point> GetFullFrontier()
        {
            HashSet<Point> frontier = new HashSet<Point>();

            foreach (Point point in PointList)
            {
                frontier.Add(new Point(point.X + 1, point.Y));
                frontier.Add(new Point(point.X - 1, point.Y));
                frontier.Add(new Point(point.X, point.Y + 1));
                frontier.Add(new Point(point.X, point.Y - 1));
                frontier.Add(new Point(point.X + 1, point.Y + 1));
                frontier.Add(new Point(point.X - 1, point.Y - 1));
                frontier.Add(new Point(point.X - 1, point.Y + 1));
                frontier.Add(new Point(point.X + 1, point.Y - 1));
            }

            return frontier.Except(PointLookup);
        }

        public IEnumerable<Point> GetFrontier(int dx, int dy)
        {
            HashSet<Point> frontier = new HashSet<Point>();

            foreach (Point point in PointList)
            {
                frontier.Add(new Point(point.X + dx, point.Y + dy));
            }

            return frontier.Except(PointLookup);
        }

        public IEnumerator<Point> GetEnumerator()
        {
            return PointList.GetEnumerator();
        }

        public Vector2 GetRandomPixel(Random random)
        {
            Point point = PointList.Pick(random);
            float x = (point.X + random.NextFloat()) * 16;
            float y = (point.Y + random.NextFloat()) * 16;
            return new Vector2(x,y);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        
    }

    enum Facing
    {
        North,
        East,
        South,
        West,
    }

    public enum CreaturePose
    {
        Stand,
        Walk,
        Attack,
        Cast,
    }

    abstract class CreatureRender
    {
        public abstract void Draw(SceneGame scene, Creature creature);
    }

    class CreaturePaperdollRender : CreatureRender
    {
        public SpriteReference Body;
        public SpriteReference Head;
        public ColorMatrix BodyColor = ColorMatrix.Identity;
        public ColorMatrix HeadColor = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 0;
                    break;
                case (Facing.East):
                    facingOffset = 5;
                    break;
                case (Facing.South):
                    facingOffset = 10;
                    break;
                case (Facing.West):
                    facingOffset = 5;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
            }

            int frameOffset = 0;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Stand):
                    frameOffset = 1;
                    break;
                case (CreaturePose.Walk):
                    double lerp = LerpHelper.ForwardReverse(0, 2, (creature.Frame / 50.0) % 1);
                    frameOffset = (int)Math.Round(lerp);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 3;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 4;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(BodyColor * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(Body, facingOffset + frameOffset, creature.VisualPosition(), mirror, Color.White, 0);
            scene.PopSpriteBatch();
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(HeadColor * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(Head, facingOffset + frameOffset, creature.VisualPosition(), mirror, Color.White, 0);
            scene.PopSpriteBatch();
        }
    }

    delegate Attack AttackDelegate(Creature attacker, IEffectHolder defender);

    [SerializeInfo]
    abstract class Creature : IEffectHolder, IGameObject, IHasPosition, IJsonSerializable
    {
        public class WaitFrames : Wait
        {
            public Creature Creature;
            public int Frame;

            public WaitFrames(Creature creature, int frame)
            {
                Creature = creature;
                Frame = creature.Frame + frame;
            }

            public override bool Done => Creature.Destroyed || Creature.Frame >= Frame;

            public override void Update()
            {
                //NOOP
            }
        }

        public SceneGame World { get; set; }
        public double DrawOrder => VisualPosition().Y;
        public bool Destroyed { get; set; }

        public ActionQueue ActionQueue => World.ActionQueue;

        public ReusableID ObjectID
        {
            get;
            private set;
        }
        public Guid GlobalID
        {
            get;
            private set;
        }
        public Mask Mask = new Mask();
        public string EffectsString => string.Join(",\n", GetEffects<Effect>().Select(x => x.ToString()));
        public string StatString => string.Join(",\n", this.GetStats().Select(stat => $"{stat.Key} {stat.Value}"));

        public int X => Tile.X;
        public int Y => Tile.Y;
        public Tile Tile
        {
            get
            {
                var tiles = EffectManager.GetEffects<OnTile.Primary>(this);
                if (tiles.Any())
                    return tiles.First().Tile;
                return null;
            }
        }
        public Map Map
        {
            get
            {
                return Tile?.Map;
            }
            set
            {
                //NOOP
            }
        }
        public IEnumerable<Tile> Tiles => Mask.Select(point => Tile.GetNeighbor(point.X, point.Y));

        public string Name;
        public string Description;
        public Facing Facing = Facing.South;

        public double Experience;

        public Item EquipMainhand => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Mainhand)?.Item;
        public Item EquipOffhand => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Offhand)?.Item;
        public Item EquipBody => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Body)?.Item;
        public Item EquipQuiver => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Quiver)?.Item;

        public double TurnSpeed => 1;
        public double TurnBuildup { get; set; }
        public bool TurnReady => TurnBuildup > 1;
        public bool RemoveFromQueue => Destroyed;

        public WaitGameObject CurrentActions;
        public WaitGameObject CurrentHits;
        public Wait CurrentPopups => PopupManager.Wait;
        public Wait DeadWait = Wait.NoWait;

        public CreatureRender Render;
        public Func<Facing> VisualFacing = () => Facing.South;
        public Func<CreaturePose> VisualPose = () => CreaturePose.Walk;
        public Func<Vector2> VisualPosition = () => Vector2.Zero;
        public Func<Vector2> VisualCamera = () => Vector2.Zero;
        public Func<ColorMatrix> VisualColor = () => ColorMatrix.Identity;

        public int Frame;

        public bool Walking = false;

        public double CurrentHP => Math.Max(0,this.GetStat(Stat.HP) - this.GetTotalDamage());
        public bool Dead;
        
        public TurnTaker Control;

        Vector2 IHasPosition.VisualPosition => VisualPosition();
        public virtual Vector2 VisualTarget => VisualPosition() + new Vector2(8, 8);

        public Creature(SceneGame world)
        {
            World = world;
            World.ToAdd.Enqueue(this);
            ObjectID = EffectManager.SetID(this);
            GlobalID = EffectManager.SetGlobalID(this);
            VisualFacing = () => Facing;
            CurrentActions = new WaitGameObject(this);
            CurrentHits = new WaitGameObject(this);
        }

        public void OnDestroy()
        {
            this.ClearEffects();
            EffectManager.DeleteHolder(this);
        }

        public Func<T> Flick<T>(Func<T> on, Func<T> off, int time)
        {
            int startTime = Frame;
            return () =>
            {
                if (Frame - startTime < time)
                    return on();
                else
                    return off();
            };
        }

        public Func<T> Flash<T>(Func<T> on, Func<T> off, int periodOn, int periodOff)
        {
            int startTime = Frame;
            return () =>
            {
                if ((Frame - startTime) % (periodOn + periodOff) < periodOn)
                    return on();
                else
                    return off();
            };
        }

        public Func<ColorMatrix> SoftFlash(ColorMatrix start, ColorMatrix end, LerpHelper.Delegate lerp, int period)
        {
            int startTime = Frame;
            return () =>
            {
                double slide = (double)((Frame - startTime) % period) / period;
                return ColorMatrix.Lerp(start, end, (float)lerp(0,1,slide));
            };
        }

        public Func<float> Slide(float start, float end, LerpHelper.Delegate lerp, int time)
        {
            int startTime = Frame;
            return () =>
            {
                float slide = Math.Min(1, (Frame - startTime) / (float)time);
                return (float)lerp(start, end, slide);
            };
        }

        public Func<Color> Slide(Color start, Color end, LerpHelper.Delegate lerp, int time)
        {
            int startTime = Frame;
            return () =>
            {
                float slide = Math.Min(1, (Frame - startTime) / (float)time);
                return Color.Lerp(start,end,(float)lerp(0, 1, slide));
            };
        }

        public Func<Vector2> Slide(Vector2 start, Vector2 end, LerpHelper.Delegate lerp, int time)
        {
            int startTime = Frame;
            return () =>
            {
                float slide = Math.Min(1, (Frame - startTime) / (float)time);
                return Vector2.Lerp(start, end, (float)lerp(0,1,slide));
            };
        }

        public Func<Vector2> SlideJump(Vector2 start, Vector2 end, float height, LerpHelper.Delegate lerp, int time)
        {
            int startTime = Frame;
            return () =>
            {
                float slide = Math.Min(1, (Frame - startTime) / (float)time);
                var jumpOffset = Vector2.Lerp(new Vector2(0, 0), new Vector2(0, -height), (float)Math.Sin(slide * MathHelper.Pi));
                return Vector2.Lerp(start, end, (float)lerp(0, 1, slide)) + jumpOffset;
            };
        }

        public Func<T> Static<T>(T value)
        {
            return () => value;
        }

        public Func<ColorMatrix> Static(Color value)
        {
            return () => ColorMatrix.Tint(value);
        }

        public Func<CreaturePose> FlickPose(CreaturePose flickPose, CreaturePose restPose, int time)
        {
            int startTime = Frame;
            return () =>
            {
                if (Frame - startTime < time)
                    return flickPose;
                else
                    return restPose;
            };
        }

        public virtual void Update()
        {
            Frame++;
            CurrentActions.Update();
        }

        public void CheckDead(Vector2 dir)
        {
            if(CurrentHP <= 0 && !Dead && !Destroyed)
            {
                Dead = true;
                World.Wait.Add(this.OnDeath(new DeathEvent(this)));
                Scheduler.Instance.Run(RoutineDie(dir));
            }
        }

        public void AddControlTurn()
        {
            ActionQueue queue = World.ActionQueue;
            Control = new TurnTakerCreatureControl(queue, this);
            queue.Add(Control);
            AddNormalTurn();
        }

        public void AddNormalTurn()
        {
            ActionQueue queue = World.ActionQueue;
            queue.Add(new TurnTakerCreatureNormal(queue, this));
        }

        public virtual Wait StartTurn(Turn turn)
        {
            return Wait.NoWait;
        }

        public virtual Wait TakeTurn(Turn turn)
        {
            Wait wait = Wait.NoWait;
            if (Dead)
                return wait;

            return Wait.NoWait;
        }

        public virtual Wait EndTurn(Turn turn)
        {
            return Wait.NoWait;
        }

        public virtual Wait NormalTurn(Turn turn)
        {
            
            foreach (var statusEffect in this.GetStatusEffects())
                statusEffect.Update();
            return this.OnTurn(new TurnEvent(turn, this));
        }

        IEnumerable<Point> Zero = new Point[] { Point.Zero };

        private void SetMask(Tile primary)
        {
            primary.AddPrimary(this);
            foreach (var point in Mask.Except(Zero))
            {
                Tile neighbor = primary.GetNeighbor(point.X, point.Y);
                if (neighbor != null)
                    neighbor.Add(this);
            }
        }

        private void UnsetMask()
        {
            foreach (var effect in GetEffects<OnTile>())
                effect.Remove();
        }

        public void Resize()
        {
            Tile tile = Tile;
            UnsetMask();
            SetMask(tile);
        }

        public void UpdateVisualPosition()
        {
            VisualPosition = Static(new Vector2(X, Y) * 16);
            VisualCamera = VisualPosition;
        }

        public void MoveTo(Tile tile, int time)
        {
            Tile previousTile = Tile;
            EventBus.PushEvent(new EventMove(this, previousTile, tile));
            UnsetMask();
            if (tile == null)
                return;
            SetMask(tile);
            if (time <= 0)
                VisualPosition = Static(new Vector2(tile.X, tile.Y) * 16);
            else
                VisualPosition = Slide(VisualPosition(), new Vector2(tile.X, tile.Y) * 16, LerpHelper.Linear, time);
            VisualCamera = VisualPosition;
            EventBus.PushEvent(new EventMove.Finish(this, previousTile, tile));
        }

        public void Move(int dx, int dy, int time)
        {
            Tile tile = Tile.GetNeighbor(dx, dy);
            if (tile == null)
                return;
            var frontier = Mask.GetFrontier(dx, dy);
            if (frontier.Select(p => Tile.GetNeighbor(p.X, p.Y)).Any(front => front.Solid || front.Creatures.Any()))
                return;
            MoveTo(tile, time);
        }

        public void ForceMove(int dx, int dy, int time)
        {
            Tile tile = Tile.GetNeighbor(dx, dy);
            if (tile == null)
                return;
            MoveTo(tile, time);
        }

        public IEnumerable<Wait> RoutineMove(int dx, int dy)
        {
            Move(dx, dy, 10);
            VisualPose = FlickPose(CreaturePose.Walk, CreaturePose.Stand, 60);
            yield return new WaitFrames(this, 10);
        }

        public IEnumerable<Wait> RoutineAttack(int dx, int dy, AttackDelegate attackGenerator)
        {
            yield return PopupManager.Wait;
            var frontier = Mask.GetFrontier(dx, dy);
            List<Wait> waitForDamage = new List<Wait>();
            PopupManager.StartCollect();
            foreach(var tile in frontier.Select(o => Tile.GetNeighbor(o.X,o.Y)))
            {
                if (tile is IMineable mineable)
                {
                    waitForDamage.Add(mineable.Mine(new MineEvent(this,EquipMainhand,100)));
                }
                else
                {
                    foreach (var creature in tile.Creatures) {
                        var wait = Attack(creature, new Vector2(dx, dy), attackGenerator);
                        waitForDamage.Add(wait);
                    }
                }
            }
           
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos + new Vector2(dx * 8, dy * 8), pos, LerpHelper.Linear, 10);
            VisualPose = FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
            yield return new WaitFrames(this,10);
            yield return new WaitAll(waitForDamage);
            PopupManager.FinishCollect();
        }

        public IEnumerable<Wait> RoutineShootArrow(int dx, int dy)
        {
            yield return PopupManager.Wait;

            List<Wait> waitForDamage = new List<Wait>();
            PopupManager.StartCollect();
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            Item quiver = EquipQuiver;
            if (quiver is ToolArrow arrow)
            {
                VisualPosition = Static(pos - new Vector2(dx * 8, dy * 8));
                VisualPose = Static(CreaturePose.Attack);

                yield return Scheduler.Instance.RunAndWait(arrow.RoutineShoot(this, dx, dy, waitForDamage));
            
                VisualPosition = Slide(pos - new Vector2(dx * 8, dy * 8), pos, LerpHelper.Linear, 10);
                VisualPose = FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
                yield return new WaitFrames(this, 10);
            }
            yield return new WaitAll(waitForDamage);
            PopupManager.FinishCollect();
        }

        public IEnumerable<Wait> RoutineHit(Vector2 dir)
        {
            if (dir.X != 0 || dir.Y != 0)
            {
                var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
                VisualPosition = Slide(pos + new Vector2(dir.X * 8, dir.Y * 8), pos, LerpHelper.Linear, 10);
                VisualPose = Static(CreaturePose.Stand);
                yield return new WaitFrames(this, 10);
            }
            yield return CurrentPopups;
        }

        public virtual IEnumerable<Wait> RoutineDie(Vector2 dir)
        {
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos, pos + new Vector2(dir.X * 8, dir.Y * 8), LerpHelper.Linear, 20);
            VisualPose = Static(CreaturePose.Stand);
            VisualColor = Flash(() => ColorMatrix.Identity, () => ColorMatrix.Tint(Color.Transparent), 2, 2);
            DeadWait = new WaitTime(60);
            yield return Wait.NoWait;
        }

        public virtual IEnumerable<Wait> RoutineDestroy()
        {
            yield return DeadWait;
            if (Dead && !Destroyed && this != World.Player)
                this.Destroy();
            yield return Wait.NoWait;
        }

        /*public IEnumerable<Wait> RoutineShowPopups()
        {
            var messages = GetEffects<EffectMessage>();
            yield return new WaitFrames(this, 10);
            while (messages.Any())
            {
                var message = messages.First();
                new DamagePopup(World, VisualTarget, message.Text, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true), 60);
                message.Remove();
                messages = GetEffects<EffectMessage>();
                yield return new WaitFrames(this, 30);
            }
        }*/

        public virtual bool IsHostile(Creature other)
        {
            return other is Enemy;
        }

        public static Attack MeleeAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            if (attacker.EquipMainhand == null) //Barehanded Attack
            {
                attack.Elements.Add(Element.Bludgeon, 1.0);
            }
            else
            {
                attack.ExtraEffects.Add(new AttackWeapon(attacker.EquipMainhand));
                foreach (var element in attacker.EquipMainhand.GetElements())
                {
                    attack.Elements.Add(element.Key, element.Value);
                }
            }
            attack.ExtraEffects.Add(new AttackPhysical());
            return attack;
        }

        public Wait Attack(Creature target, Vector2 dir, AttackDelegate attackGenerator)
        {
            Attack attack = attackGenerator(this, target);
            attack.HitDirection = dir;
            var waitAttack = Scheduler.Instance.RunAndWait(attack.RoutineStart());
            var waitHit = Scheduler.Instance.RunAndWait(target.RoutineHit(dir));
            var wait = new WaitAll(new[] { waitAttack, waitHit });
            target.CurrentActions.Add(wait);
            target.CurrentHits.Add(wait);
            return wait;
        }

        public Wait AttackSelf(AttackDelegate attackGenerator)
        {
            return AttackSelf(attackGenerator(this, this));
        }

        public Wait AttackSelf(Attack attack)
        {
            var waitAttack =  Scheduler.Instance.RunAndWait(attack.RoutineStart());
            Scheduler.Instance.RunAndWait(RoutineHit(Vector2.Zero));

            return waitAttack;
        }

        public Wait WaitSome(int frames)
        {
            return new WaitFrames(this, frames);
        }

        public void Pickup(Item item)
        {
            foreach(Item existing in this.GetInventory())
            {
                bool merged = existing.Merge(item);
                if (merged)
                {
                    item.Destroy();
                    return;
                }
            }
            Effect.Apply(new EffectItemInventory(item, this));
        }

        public void Equip(Item item, EquipSlot slot)
        {
            Effect.Apply(new EffectItemEquipped(item, this, slot));
        }

        public void Unequip(Item item)
        {
            foreach(var effect in EffectManager.GetEffects<EffectItemEquipped>(this).Where(stat => stat.Item == item))
            {
                effect.Remove();
            }
        }

        public void Unequip(EquipSlot slot)
        {
            foreach (var effect in EffectManager.GetEffects<EffectItemEquipped>(this).Where(stat => stat.Slot == slot))
            {
                effect.Remove();
            }
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            var list = new List<T>();
            list.AddRange(EffectManager.GetEffects<T>(this));
            foreach (var equip in EffectManager.GetEffects<EffectItemEquipped>(this))
                list.AddRange(equip.Effects.SplitEffects<T>());
            foreach (var statusEffect in EffectManager.GetEffects<EffectStatusEffect>(this))
                list.AddRange(statusEffect.Effects.SplitEffects<T>());
            var tiles = EffectManager.GetEffects<OnTile>(this);
            var tileEffects = tiles.SelectMany(tile => tile.Effects.SplitEffects<T>()).Distinct();
            list.AddRange(tileEffects);
            return list;
        }

        public Wait OnAttack(Attack attack)
        {
            return Scheduler.Instance.RunAndWait(this.PushEvent<Attack, OnAttack>(attack));
        }

        public Wait OnStartAttack(Attack attack)
        {
            return Scheduler.Instance.RunAndWait(this.PushEvent<Attack, OnStartAttack>(attack));
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Game.FormatColor(Color.Yellow)}{Name}{Game.FormatColor(Color.White)}{Game.FORMAT_BOLD}\n";
            tooltip += $"{String.Join(", ", this.GetFamilies().Select(family => family.Name))}\n";
            tooltip += $"{Description}\n";
            tooltip += $"HP {CurrentHP}/{this.GetStat(Stat.HP)}\n";
            foreach(StatusEffect statusEffect in this.GetStatusEffects())
            {
                tooltip += $"{Game.FORMAT_BOLD}{statusEffect.Name}{Game.FORMAT_BOLD} {statusEffect.BuildupTooltip} {statusEffect.DurationText}\n";
                tooltip += $"{statusEffect.Description}\n";
            }
        }

        public void AddStatBlock(ref string description)
        {
            description += $"{String.Join(", ", this.GetFamilies().Select(family => family.Name))}\n";

            var effects = GetEffects<Effect>().Where(x => !(x is IStat));
            var effectGroups = effects.GroupBy(effect => effect, Effect.StatEquality);

            foreach (var stat in Stat.AllStats.OrderBy(stat => stat.Priority))
            {
                var value = this.GetStat(stat);
                if(value != stat.DefaultStat)
                    description += $"{Game.FormatStat(stat)} {stat.Name} {stat.Format(this, value)}\n";
            }
            
            foreach (var group in effectGroups.OrderBy(group => group.Key.VisualPriority))
            {
                group.Key.AddStatBlock(ref description, group);
            }
        }

        public bool ShouldDraw(Map map)
        {
            return Map == map;
        }

        public virtual IEnumerable<DrawPass> GetDrawPasses() 
        {
            yield return DrawPass.Creature;
        }

        public virtual void Draw(SceneGame scene, DrawPass pass)
        {
            if (IsVisible())
                Render.Draw(scene, this);
        }

        public bool IsVisible()
        {
            return Tiles.Any(tile => !tile.Opaque);
        }

        public override string ToString()
        {
            return $"Creature {ObjectID.ID}";
        }

        public bool IsControllable(Creature player)
        {
            return player == this;
        }

        public virtual JToken WriteJson(Context context)
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["objectId"] = Serializer.GetHolderID(this);
            json["name"] = Name;
            json["description"] = Description;
            return json;
        }

        public virtual void ReadJson(JToken json, Context context)
        {
            Guid globalId = Guid.Parse(json["objectId"].Value<string>());
            GlobalID = EffectManager.SetGlobalID(this, globalId);
            Name = json["name"].Value<string>();
            Description = json["description"].Value<string>();
        }

        public void AfterLoad()
        {
            AddControlTurn();
            UpdateVisualPosition();
        }
    }

    class Hero : Creature
    {
        public Hero(SceneGame world) : base(world)
        {
            Name = "You";
            Description = "This is you.";

            Render = new CreaturePaperdollRender()
            {
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_crusader"),
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_helmet_a"),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 1000));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));
        }

        [Construct("hero")]
        public static Hero Construct(Context context)
        {
            return new Hero(context.World);
        }
    }
}
