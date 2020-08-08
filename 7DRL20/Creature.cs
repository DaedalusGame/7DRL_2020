using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.Events;
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

        public int ID;
        public string Name;
        public SpriteReference Sprite;
        public Stat Resistance;
        public Stat DamageRate;

        public double Priority;

        public Element(string name, SpriteReference sprite)
        {
            ID = AllElements.Count;
            Name = name;
            Sprite = sprite;
            Priority = ID;
            Resistance = new ElementStat(this, $"{Name} Resistance", 0, 4, 0, SpriteLoader.Instance.AddSprite("content/stat_element_defense"));
            DamageRate = new ElementStat(this, $"{Name} Damage Rate", 1, 4, 0.5, SpriteLoader.Instance.AddSprite("content/stat_element_rate"));
            AllElements.Add(this);
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

        public static Element Bludgeon = new Element("Bludgeon", SpriteLoader.Instance.AddSprite("content/element_blunt"));
        public static Element Slash = new Element("Slash", SpriteLoader.Instance.AddSprite("content/element_slice"));
        public static Element Pierce = new Element("Pierce", SpriteLoader.Instance.AddSprite("content/element_pierce"));

        public static Element Fire = new Element("Fire", SpriteLoader.Instance.AddSprite("content/element_fire"));
        public static Element Ice = new Element("Ice", SpriteLoader.Instance.AddSprite("content/element_ice"));
        public static Element Thunder = new Element("Thunder", SpriteLoader.Instance.AddSprite("content/element_thunder"));
        public static Element Water = new Element("Water", SpriteLoader.Instance.AddSprite("content/element_water"));
        public static Element Wind = new Element("Wind", SpriteLoader.Instance.AddSprite("content/element_wind"));
        public static Element Earth = new Element("Earth", SpriteLoader.Instance.AddSprite("content/element_earth"));
        public static Element Holy = new Element("Holy", SpriteLoader.Instance.AddSprite("content/element_holy"));
        public static Element Dark = new Element("Dark", SpriteLoader.Instance.AddSprite("content/element_dark"));

        //Status Elements
        public static Element Bleed = new Element("Bleed", SpriteLoader.Instance.AddSprite("content/element_blood"));
        public static Element Poison = new Element("Poison", SpriteLoader.Instance.AddSprite("content/element_poison"));

        //Combination Elements
        public static Element Light = new ElementCombined("Light", SpriteLoader.Instance.AddSprite("content/element_light"), new Dictionary<Element, double>()
        {
            { Thunder, 0.7 },
            { Fire, 0.7 }
        });
        public static Element Hellfire = new ElementCombined("Hellfire", SpriteLoader.Instance.AddSprite("content/element_hellfire"), new Dictionary<Element, double>()
        {
            { Holy, 0.7 },
            { Fire, 0.7 }
        });
        public static Element Drought = new ElementCombined("Drought", SpriteLoader.Instance.AddSprite("content/element_drought"), new Dictionary<Element, double>()
        {
            { Water, 1.0 },
            { Ice, 1.0 }
        });
        public static Element BlackFlame = new ElementCombined("Black Flame", SpriteLoader.Instance.AddSprite("content/element_black_flame"), new Dictionary<Element, double>()
        {
            { Fire, 0.5 },
            { Dark, 1.5 }
        });
        public static Element RedDevil = new ElementCombined("Red Devil", SpriteLoader.Instance.AddSprite("content/element_red_devil"), new Dictionary<Element, double>()
        {
            { Fire, 1.5 },
            { Dark, 0.5 }
        });
        public static Element Darkness = new ElementCombined("Darkness", SpriteLoader.Instance.AddSprite("content/element_darkness"), new Dictionary<Element, double>()
        {
            { Ice, 0.8 },
            { Dark, 0.8 }
        });
        public static Element Emperor = new ElementCombined("Emperor", SpriteLoader.Instance.AddSprite("content/element_emperor"), new Dictionary<Element, double>()
        {
            { Thunder, 1.5 },
            { Dark, 0.5 }
        });
        public static Element HeavenThunder = new ElementCombined("Heavenly Thunder", SpriteLoader.Instance.AddSprite("content/element_heaven_thunder"), new Dictionary<Element, double>()
        {
            { Thunder, 1.5 },
            { Holy, 0.5 }
        });
        public static Element ThunderPole = new ElementCombined("Thunder Pole", SpriteLoader.Instance.AddSprite("content/element_thunderpole"), new Dictionary<Element, double>()
        {
            { Thunder, 0.7 },
            { Earth, 0.7 }
        });
        public static Element Steam = new ElementCombined("Steam", SpriteLoader.Instance.AddSprite("content/element_steam"), new Dictionary<Element, double>()
        {
            { Water, 0.6 },
            { Fire, 0.4 }
        });
        public static Element Sand = new ElementCombined("Sand", SpriteLoader.Instance.AddSprite("content/element_sand"), new Dictionary<Element, double>()
        {
            { Earth, 0.5 },
            { Wind, 0.5 }
        });
        public static Element Mud = new ElementCombined("Mud", SpriteLoader.Instance.AddSprite("content/element_mud"), new Dictionary<Element, double>()
        {
            { Water, 0.5 },
            { Earth, 0.5 }
        });
        public static Element Permafrost = new ElementCombined("Permafrost", SpriteLoader.Instance.AddSprite("content/element_permafrost"), new Dictionary<Element, double>()
        {
            { Ice, 0.6 },
            { Earth, 0.6 }
        });
        public static Element Storm = new ElementCombined("Storm", SpriteLoader.Instance.AddSprite("content/element_storm"), new Dictionary<Element, double>()
        {
            { Water, 0.6 },
            { Wind, 0.6 }
        });
        public static Element Magma = new ElementCombined("Magma", SpriteLoader.Instance.AddSprite("content/element_magma"), new Dictionary<Element, double>()
        {
            { Earth, 0.4 },
            { Fire, 0.8 }
        });
        public static Element Metal = new ElementCombined("Metal", SpriteLoader.Instance.AddSprite("content/element_metal"), new Dictionary<Element, double>()
        {
            { Earth, 0.6 },
            { Holy, 0.4 }
        });
        public static Element Inferno = new ElementCombined("Inferno", SpriteLoader.Instance.AddSprite("content/element_inferno"), new Dictionary<Element, double>()
        {
            { Wind, 0.5 },
            { Fire, 0.5 }
        });
        public static Element Blizzard = new ElementCombined("Blizzard", SpriteLoader.Instance.AddSprite("content/element_blizzard"), new Dictionary<Element, double>()
        {
            { Wind, 0.5 },
            { Ice, 0.5 }
        });
        public static Element Arcane = new ElementCombined("Arcane", SpriteLoader.Instance.AddSprite("content/element_arcane"), new Dictionary<Element, double>()
        {
            { Dark, 0.5 },
            { Holy, 0.5 }
        });
        public static Element Origin = new ElementCombined("Origin", SpriteLoader.Instance.AddSprite("content/element_origin"), new Dictionary<Element, double>()
        {
            { BlackFlame, 0.2 },
            { Darkness, 0.2 },
            { RedDevil, 0.2 },
            { Light, 0.2 },
            { HeavenThunder, 0.2 },
            { Drought, 0.2 },
        });
        public static Element Chaos = new ElementRandom("Chaos", SpriteLoader.Instance.AddSprite("content/element_demon"), new List<Element>()
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
        public static Element TheEnd = new ElementCombined("The End", SpriteLoader.Instance.AddSprite("content/element_the_end"), new Dictionary<Element, double>()
        {
            { Dark, 2.0 },
            { Holy, 0.3 }
        });

        public static Element Healing = new Element("Healing", SpriteLoader.Instance.AddSprite("content/element_healing"));

        public static Element[] PhysicalElements = new Element[] { Bludgeon, Slash, Pierce };
        public static Element[] MagicalElements = new Element[] { Fire, Ice, Thunder, Water, Wind, Earth, Holy, Dark };
    }

    class ElementCombined : Element
    {
        Dictionary<Element, double> Composites = new Dictionary<Element, double>();

        public ElementCombined(string name, SpriteReference sprite, Dictionary<Element, double> composites) : base(name, sprite)
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

        public ElementRandom(string name, SpriteReference sprite, List<Element> composites, double total) : base(name, sprite)
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

        public int ID;
        public string Name;
        public double DefaultStat;
        public SpriteReference Sprite;

        public double Priority;
        public virtual double EffectivePriority => Priority;

        public Stat(string name, double defaultStat, double priority, SpriteReference sprite)
        {
            ID = AllStats.Count;
            Name = name;
            DefaultStat = defaultStat;
            Sprite = sprite;
            Priority = priority;
            AllStats.Add(this);
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual void DrawIcon(Scene scene, Vector2 pos)
        {
            scene.DrawSprite(Sprite, 0, pos, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }

        public static Stat HP = new Stat("HP", 0, 0, SpriteLoader.Instance.AddSprite("content/stat_hp"));
        public static Stat Attack = new Stat("Attack", 0, 1, SpriteLoader.Instance.AddSprite("content/stat_attack"));
        public static Stat Defense = new Stat("Defense", 0, 2, SpriteLoader.Instance.AddSprite("content/stat_defense"));
        public static Stat AlchemyPower = new Stat("Alchemy Power", 0, 5, SpriteLoader.Instance.AddSprite("content/stat_alchemy"));
        public static Stat Speed = new Stat("Speed", 1, 8, SpriteLoader.Instance.AddSprite("content/stat_speed"));

        public static Stat DamageRate = new Stat("Damage Rate", 1, 3, SpriteLoader.Instance.AddSprite("content/stat_damage_rate"));

        public static Stat MiningLevel = new Stat("Mining Level", 0, 6, SpriteLoader.Instance.AddSprite("content/stat_mining_level"));
        public static Stat MiningSpeed = new Stat("Mining Speed", 1, 7, SpriteLoader.Instance.AddSprite("content/stat_mining_speed"));

        public static Stat Cooldown = new Stat("Cooldown", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_cooldown"));
        public static Stat Warmup = new Stat("Warmup", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_warmup"));

        public static Stat Blood = new Stat("Blood", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_blood"));

        public static Stat SlimeHP = new Stat("SlimeHP", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_slime_hp"));
        public static Stat SlimeAttack = new Stat("SlimeAttack", 0, -1, SpriteLoader.Instance.AddSprite("content/stat_slime_attack"));

        public static Flag SwapItem = new Flag("Swap Item", true, SpriteLoader.Instance.AddSprite("content/stat_swap_enabled"));
        public static Flag EquipItem = new Flag("Equip Item", true, SpriteLoader.Instance.AddSprite("content/stat_equip_enabled"));
        public static Flag UnequipItem = new Flag("Unequip Item", true, SpriteLoader.Instance.AddSprite("content/stat_unequip_enabled"));
        public static Flag LightningRod = new Flag("Lightning Rod", true, SpriteLoader.Instance.AddSprite("content/stat_lightning_rod"));
        public static Flag Insulation = new Flag("Insulation", true, SpriteLoader.Instance.AddSprite("content/stat_insulation"));

        public static Stat[] Stats = new Stat[] { HP, Attack, Defense, AlchemyPower, DamageRate };
    }

    class ElementStat : Stat
    {
        Element Element;

        public double SubPriority;
        public override double EffectivePriority => Priority + (Element.Priority + SubPriority) / Element.AllElements.Count;

        public ElementStat(Element element, string name, double defaultStat, double priority, double subPriority, SpriteReference sprite) : base(name, defaultStat, priority, sprite)
        {
            Element = element;
            SubPriority = subPriority;
        }

        public override void DrawIcon(Scene scene, Vector2 pos)
        {
            scene.DrawSprite(Element.Sprite, 0, pos, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            scene.DrawSprite(Sprite, 0, pos, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
        }
    }

    class Flag : Stat
    {
        public bool DefaultValue;
        
        public Flag(string name, bool defaultValue, SpriteReference sprite) : base(name, 0, -1, sprite)
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

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(BodyColor * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(Body, facingOffset + frameOffset, creature.VisualPosition(), mirror, Color.White, 0);
            scene.PopSpriteBatch();
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(HeadColor * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(Head, facingOffset + frameOffset, creature.VisualPosition(), mirror, Color.White, 0);
            scene.PopSpriteBatch();
        }
    }

    abstract class Creature : IEffectHolder, IGameObject, IHasPosition
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

            public override bool Done => Creature.Frame >= Frame;

            public override void Update()
            {
                //NOOP
            }
        }

        public SceneGame World { get; set; }
        public double DrawOrder => VisualPosition().Y;
        public bool Destroyed { get; set; }

        public ActionQueue ActionQueue => World.ActionQueue;

        public ReusableID ObjectID {
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
        public Map Map => Tile?.Map;
        public IEnumerable<Tile> Tiles => Mask.Select(point => Tile.GetNeighbor(point.X, point.Y));

        public string Name;
        public string Description;
        public Facing Facing = Facing.South;

        public double Experience;

        public Item EquipMainhand => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Mainhand)?.Item;
        public Item EquipOffhand => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Offhand)?.Item;
        public Item EquipBody => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Body)?.Item;

        public double TurnSpeed => 1;
        public double TurnBuildup { get; set; }
        public bool TurnReady => TurnBuildup > 1;
        public bool RemoveFromQueue => Destroyed;
        public Wait CurrentAction = Wait.NoWait;
        public Wait CurrentPopups => PopupManager.Wait;

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
        public Wait DeadWait = Wait.NoWait;
        public TurnTaker Control;

        Vector2 IHasPosition.VisualPosition => VisualPosition();
        public virtual Vector2 VisualTarget => VisualPosition() + new Vector2(8, 8);

        public Creature(SceneGame world)
        {
            World = world;
            World.ToAdd.Enqueue(this);
            ObjectID = EffectManager.NewID(this);
            VisualFacing = () => Facing;
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
        }

        public void CheckDead(int dx, int dy)
        {
            if(CurrentHP <= 0 && !Dead)
            {
                Dead = true;
                World.Wait.Add(this.OnDeath(new DeathEvent(this)));
                Scheduler.Instance.Run(RoutineDie(dx, dy));
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
            var cloud = Map.AddCloud(map => new CloudSmoke(World, map));
            cloud.Add(Tile, 25);
            foreach (var statusEffect in this.GetStatusEffects())
                statusEffect.Update();
            return Wait.NoWait;
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

        public IEnumerable<Wait> RoutineAttack(int dx, int dy, Func<Creature, IEffectHolder, Attack> attackGenerator)
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
                        Attack(creature, dx, dy, attackGenerator);
                        waitForDamage.Add(creature.CurrentAction);
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

        public IEnumerable<Wait> RoutineHit(int dx, int dy, Attack attack)
        {
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos + new Vector2(dx * 8, dy * 8), pos, LerpHelper.Linear, 10);
            VisualPose = Static(CreaturePose.Stand);
            yield return new WaitFrames(this, 10);
            yield return CurrentPopups;
            CheckDead(dx, dy);
        }

        public virtual IEnumerable<Wait> RoutineDie(int dx, int dy)
        {
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos, pos + new Vector2(dx * 8, dy * 8), LerpHelper.Linear, 20);
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
                foreach (var element in attacker.GetElements())
                {
                    attack.Elements.Add(element.Key, element.Value);
                }
            }
            return attack;
        }

        public Attack Attack(Creature target, int dx, int dy, Func<Creature, IEffectHolder, Attack> attackGenerator)
        {
            Attack attack = attackGenerator(this, target);
            var waitAttack = Scheduler.Instance.RunAndWait(attack.RoutineStart());
            var waitHit = Scheduler.Instance.RunAndWait(target.RoutineHit(dx, dy, attack));
            target.CurrentAction = new WaitAll(new[] { waitAttack, waitHit });
            return attack;
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

            Effect.Apply(new EffectStat(this, Stat.HP, 1000));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));
        }
    }
}
