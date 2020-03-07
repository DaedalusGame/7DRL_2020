using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
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
        public string Name;
        public Stat Resistance;
        public Stat DamageRate;

        public Element(string name)
        {
            Name = name;
            Resistance = new Stat($"{Name} Resistance", 0);
            DamageRate = new Stat($"{Name} Damage Rate", 1);
        }

        public override string ToString()
        {
            return Name;
        }

        public static Element Bludgeon = new Element("Bludgeon");
        public static Element Slash = new Element("Slash");
        public static Element Pierce = new Element("Pierce");

        public static Element Fire = new Element("Fire");
        public static Element Ice = new Element("Ice");
        public static Element Thunder = new Element("Thunder");
        public static Element Water = new Element("Water");
        public static Element Wind = new Element("Wind");
        public static Element Earth = new Element("Earth");
        public static Element Light = new Element("Light");
        public static Element Dark = new Element("Dark");

        public static Element Healing = new Element("Healing");

        public static Element[] PhysicalElements = new Element[] { Bludgeon, Slash, Pierce };
        public static Element[] MagicalElements = new Element[] { Fire, Ice, Thunder, Water, Wind, Earth, Light, Dark };
    }

    class Stat
    {
        public string Name;
        public double DefaultStat;

        public Stat(string name, double defaultStat)
        {
            Name = name;
            DefaultStat = defaultStat;
        }

        public override string ToString()
        {
            return Name;
        }

        public static Stat HP = new Stat("HP", 0);
        public static Stat Attack = new Stat("Attack", 0);
        public static Stat Defense = new Stat("Defense", 0);
        public static Stat AlchemyPower = new Stat("Alchemy Power", 0);

        public static Stat DamageRate = new Stat("Damage Rate", 1);

        public static Stat MiningLevel = new Stat("Mining Level", 0);
        public static Stat MiningSpeed = new Stat("Mining Speed", 1);

        public static Stat[] Stats = new Stat[] { HP, Attack, Defense, AlchemyPower, DamageRate };
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
                scene.SetupColorMatrix(BodyColor * ColorMatrix.Tint(creature.VisualColor()), matrix);
            });
            scene.DrawSprite(Body, facingOffset + frameOffset, creature.VisualPosition(), mirror, creature.VisualColor(), 0);
            scene.PopSpriteBatch();
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(HeadColor * ColorMatrix.Tint(creature.VisualColor()), matrix);
            });
            scene.DrawSprite(Head, facingOffset + frameOffset, creature.VisualPosition(), mirror, creature.VisualColor(), 0);
            scene.PopSpriteBatch();
        }
    }

    abstract class Creature : IEffectHolder, ITurnTaker, IGameObject
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
        public IEnumerable<Tile> Tiles => Mask.Select(point => Tile.GetNeighbor(point.X, point.Y));

        public string Name;
        public string Description;
        public Facing Facing;

        public Item EquipMainhand => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Mainhand)?.Item;
        public Item EquipOffhand => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Offhand)?.Item;
        public Item EquipBody => GetEffects<EffectItemEquipped>().FirstOrDefault(x => x.Slot == EquipSlot.Body)?.Item;

        public double TurnSpeed => 1;
        public double TurnBuildup { get; set; }
        public bool TurnReady => TurnBuildup > 1;
        public bool RemoveFromQueue => Destroyed;
        public Wait CurrentAction = Wait.NoWait;
        public Wait CurrentPopups = Wait.NoWait;

        public CreatureRender Render;
        public Func<Facing> VisualFacing = () => Facing.South;
        public Func<CreaturePose> VisualPose = () => CreaturePose.Walk;
        public Func<Vector2> VisualPosition = () => Vector2.Zero;
        public Func<Vector2> VisualCamera = () => Vector2.Zero;
        public Func<Color> VisualColor = () => Color.White;

        public int Frame;

        public bool Walking = false;

        public double CurrentHP => Math.Max(0,this.GetStat(Stat.HP) - this.GetTotalDamage());
        public bool Dead => CurrentHP <= 0;

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

        public Func<Vector2> Slide(Vector2 start, Vector2 end, LerpHelper.Delegate lerp, int time)
        {
            int startTime = Frame;
            return () =>
            {
                float slide = Math.Min(1, (Frame - startTime) / (float)time);
                return Vector2.Lerp(start, end, (float)lerp(0,1,slide));
            };
        }

        public Func<CreaturePose> Static(CreaturePose pose)
        {
            return () => pose;
        }

        public Func<Color> Static(Color color)
        {
            return () => color;
        }

        public Func<Vector2> Static(Vector2 pos)
        {
            return () => pos;
        }

        public Func<Color> Flash(Color color1, Color color2, int time)
        {
            int startTime = Frame;
            return () =>
            {
                if ((Frame - startTime) % 4 < 2 && (Frame - startTime) < time)
                    return color1;
                else
                    return color2;
            };
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

        public void Update()
        {
            ShowPopups();
            Frame++;
        }

        private Wait ShowPopups()
        {
            if (CurrentPopups.Done)
                CurrentPopups = Scheduler.Instance.RunAndWait(RoutineShowPopups());
            return CurrentPopups;
        }

        public virtual Wait TakeTurn(ActionQueue queue)
        {
            this.ResetTurn();
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

        public void MoveTo(Tile tile)
        {
            UnsetMask();
            if (tile == null)
                return;
            SetMask(tile);
            VisualPosition = Slide(VisualPosition(), new Vector2(tile.X, tile.Y) * 16, LerpHelper.Linear, 10);
            VisualCamera = VisualPosition;
        }

        public void Move(int dx, int dy)
        {
            Tile tile = Tile.GetNeighbor(dx, dy);
            if (tile == null)
                return;
            var frontier = Mask.GetFrontier(dx, dy);
            if (frontier.Select(p => Tile.GetNeighbor(p.X, p.Y)).Any(front => front.Solid || front.Creatures.Any()))
                return;
            MoveTo(tile);
        }

        public IEnumerable<Wait> RoutineMove(int dx, int dy)
        {
            Move(dx, dy);
            VisualPose = FlickPose(CreaturePose.Walk, CreaturePose.Stand, 60);
            yield return new WaitFrames(this, 10);
        }

        public IEnumerable<Wait> RoutineAttack(int dx, int dy, Func<Creature, IEffectHolder, Attack> attackGenerator)
        {
            var frontier = Mask.GetFrontier(dx, dy);
            List<Wait> waitForDamage = new List<Wait>();
            foreach(var tile in frontier.Select(o => Tile.GetNeighbor(o.X,o.Y)))
            {
                if (tile is IMineable mineable)
                {
                    mineable.Mine(new MineEvent(this,EquipMainhand));
                }
                else
                {
                    foreach (var creature in tile.Creatures)
                    {
                        waitForDamage.Add(Attack(creature, dx, dy, attackGenerator));
                    }
                }
            }
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos + new Vector2(dx * 8, dy * 8), pos, LerpHelper.Linear, 10);
            VisualPose = FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
            yield return new WaitFrames(this,10);
            yield return new WaitAll(waitForDamage);
        }

        public IEnumerable<Wait> RoutineHit(int dx, int dy, Attack attack)
        {
            if (Dead)
            {
                yield return Scheduler.Instance.RunAndWait(RoutineDie(dx, dy));
            }
            else
            {
                var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
                VisualPosition = Slide(pos + new Vector2(dx * 8, dy * 8), pos, LerpHelper.Linear, 10);
                VisualPose = Static(CreaturePose.Stand);
                yield return new WaitFrames(this, 10);
                yield return ShowPopups();
            }
        }

        public IEnumerable<Wait> RoutineDie(int dx, int dy)
        {
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos, pos + new Vector2(dx * 8, dy * 8), LerpHelper.Linear, 20);
            VisualPose = Static(CreaturePose.Stand);
            VisualColor = Flash(Color.White, Color.Transparent, 100000);
            yield return ShowPopups();
            yield return new WaitFrames(this, 50);
            VisualColor = Static(Color.Transparent);
            if (Dead && this != World.Player)
                this.Destroy();
        }

        public IEnumerable<Wait> RoutineShowPopups()
        {
            var messages = GetEffects<EffectMessage>();
            yield return new WaitFrames(this, 10);
            while (messages.Any())
            {
                var message = messages.First();
                new DamagePopup(World, VisualPosition() + new Vector2(8, 8), message.Text, new TextParameters().SetColor(Color.White, Color.Black).SetBold(true), 60);
                message.Remove();
                messages = GetEffects<EffectMessage>();
                yield return new WaitFrames(this, 30);
            }
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

        public Wait Attack(Creature target, int dx, int dy, Func<Creature, IEffectHolder, Attack> attackGenerator)
        {
            Attack attack = attackGenerator(this, target);
            attack.Start();
            return target.CurrentAction = Scheduler.Instance.RunAndWait(target.RoutineHit(dx, dy, attack));
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
                list.AddRange(equip.Effects.OfType<T>());
            foreach (var statusEffect in EffectManager.GetEffects<EffectStatusEffect>(this))
                list.AddRange(statusEffect.Effects.OfType<T>());
            var tiles = EffectManager.GetEffects<OnTile>(this);
            var tileEffects = tiles.SelectMany(tile => tile.Effects.OfType<T>()).Distinct();
            list.AddRange(tileEffects);
            return list;
        }

        public void OnAttack(Attack attack)
        {
            foreach(var onAttack in GetEffects<OnAttack>())
            {
                onAttack.Trigger(attack);
            }
        }

        public void OnStartAttack(Attack attack)
        {
            foreach (var onStartAttack in GetEffects<OnStartAttack>())
            {
                onStartAttack.Trigger(attack);
            }
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Game.FormatColor(Color.Yellow)}{Name}{Game.FormatColor(Color.White)}{Game.FORMAT_BOLD}\n";
            tooltip += $"{Description}\n";
            tooltip += $"HP {CurrentHP}/{this.GetStat(Stat.HP)}\n";
            foreach(StatusEffect statusEffect in this.GetStatusEffects())
            {
                tooltip += $"{Game.FORMAT_BOLD}{statusEffect.Name}{Game.FORMAT_BOLD} {statusEffect.BuildupTooltip} {statusEffect.DurationText}\n";
                tooltip += $"{statusEffect.Description}\n";
            }
        }

        public IEnumerable<DrawPass> GetDrawPasses() 
        {
            yield return DrawPass.Creature;
        }

        public void Draw(SceneGame scene, DrawPass pass)
        {
            Render.Draw(scene, this);
        }

        public override string ToString()
        {
            return $"Creature {ObjectID.ID}";
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

            Effect.Apply(new EffectStat(this, Stat.HP, 100));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));
        }
    }
}
