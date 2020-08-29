using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using RoguelikeEngine.Attacks;
using RoguelikeEngine.Effects;

namespace RoguelikeEngine
{
    class CloudPart
    {
        public Cloud Parent;
        public Tile Tile => MapTile.Tile;
        public MapTile MapTile;
        public int Duration;

        public CloudPart(Cloud parent, MapTile mapTile, int duration)
        {
            Parent = parent;
            MapTile = mapTile;
            Duration = duration;
        }
    }

    class Cloud : IEffectHolder, IGameObject
    {
        public SceneGame World { get; set; }
        public double DrawOrder => 0;
        public bool Destroyed { get; set; }

        public ReusableID ObjectID { get; private set; }

        protected Random Random = new Random();
        public Map Map
        {
            get;
            set;
        }
        protected List<CloudPart> Parts = new List<CloudPart>();

        public string Name;
        public string Description;
        
        public Cloud(Map map)
        {
            World = map.World;
            World.ToAdd.Enqueue(this);
            ObjectID = EffectManager.SetID(this);
            Map = map;
            AddNormalTurn();
        }

        public void OnDestroy()
        {
            this.ClearEffects();
            EffectManager.DeleteHolder(this);
        }

        public void AddNormalTurn()
        {
            ActionQueue queue = World.ActionQueue;
            queue.Add(new TurnTakerCloud(queue, this));
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            var list = new List<T>();
            list.AddRange(EffectManager.GetEffects<T>(this));
            return list;
        }

        public void Add(Tile tile, int duration)
        {
            if (tile.Map != Map)
                return;

            CloudPart part = Get(tile);
            if(part == null)
            {
                Parts.Add(new CloudPart(this, tile.Parent, duration));
                UpdateMask();
            }
        }

        public void Remove(Tile tile)
        {
            int removed = Parts.RemoveAll(part => tile.Parent == part.MapTile);
            if(removed > 0)
            {
                UpdateMask();
            }
        }

        public void UpdateMask()
        {
            foreach(var effect in GetEffects<OnTile>())
            {
                effect.Remove();
            }

            foreach(var part in Parts)
            {
                Effect.Apply(new OnTile(part.MapTile, this));
            }
        }

        public CloudPart Get(Tile tile)
        {
            if (tile.Map != Map)
                return null;

            return Parts.Find(part => tile.Parent == part.MapTile);
        }

        /// <summary>
        /// Moves some randomly picked set of clouds in the specified direction
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="n">How many clouds to move</param>
        public void Drift(int dx, int dy, int n)
        {
            n = Math.Min(n, Parts.Count);
            if (n == 0 || (dx == 0 && dy == 0))
                return;
            foreach(var part in Parts.Shuffle(Random).Take(n))
            {
                var neighbor = part.Tile.GetNeighbor(dx, dy);
                var existingCloud = Get(neighbor);
                if (!neighbor.Solid && existingCloud == null)
                    part.MapTile = neighbor.Parent;
            }
            UpdateMask();
        }

        public virtual Wait NormalTurn(Turn turn)
        {
            foreach (var part in Parts)
                part.Duration--;

            int removed = Parts.RemoveAll(part => part.Duration <= 0);

            if (removed > 0)
                UpdateMask();

            return Wait.NoWait;
        }

        public virtual void Update()
        {
            //NOOP
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            tooltip += $"{Game.FORMAT_BOLD}{Game.FormatColor(Color.Yellow)}{Name}{Game.FormatColor(Color.White)}{Game.FORMAT_BOLD}\n";
            tooltip += $"{Description}\n";
        }

        public bool ShouldDraw(Map map)
        {
            return map == Map;
        }

        public virtual IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }

        public virtual void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }
    }

    class CloudGeomancy : Cloud
    {
        List<Creature> Masters = new List<Creature>();

        public CloudGeomancy(Map map) : base(map)
        {
            Name = "Geomancy";
            Description = "Stat bonuses based on tile";
        }

        public void AddMaster(Creature creature)
        {
            if (!Masters.Contains(creature))
                Masters.Add(creature);
        }

        public override Wait NormalTurn(Turn turn)
        {
            Masters.RemoveAll(creature => creature.Dead);

            if (Masters.Empty())
            {
                this.Destroy();
            }
            else
            {
                ProvideEffect();
            }

            return base.NormalTurn(turn);
        }

        public void ProvideEffect()
        {
            foreach (Creature creature in Map.Creatures)
            {
                if (!creature.HasStatusEffect<Geomancy>())
                {
                    creature.AddStatusEffect(new Geomancy(this)
                    {
                        Buildup = 1.0,
                        Duration = new Slider(float.PositiveInfinity),
                    });
                }
            }
        }
    }

    class CloudSmoke : Cloud
    {
        int Ticks;

        public CloudSmoke(Map map) : base(map)
        {
            Name = "Smoke Cloud";
            Description = "Harmless smoke.";
        }

        public override void Update()
        {
            SpriteReference smoke = SpriteLoader.Instance.AddSprite("content/smoke_small");

            Ticks++;

            foreach (var part in Parts)
            {
                if((part.GetHashCode() + Ticks) % 1 == 0)
                {
                    Vector2 pos = new Vector2(16 * part.Tile.X + Random.Next(16), 16 * part.Tile.Y + Random.Next(16));
                    new SmokeSmall(World, smoke, pos, Vector2.Zero, Color.White, 12);
                }
            }
            base.Update();
        }

        public override Wait NormalTurn(Turn turn)
        {
            Drift(0, 1, Parts.Count / 2);
            Drift(0, -1, Parts.Count / 2);
            Drift(1, 0, Parts.Count / 2);
            Drift(-1, 0, Parts.Count / 2);

            return base.NormalTurn(turn);
        }
    }

    class WeatherRain : Cloud
    {
        int Ticks;
        public int Duration;

        public WeatherRain(Map map) : base(map)
        {
        }

        public override void Update()
        {
            Ticks++;

            for (int i = 0; i < 3; i++)
            {
                Vector2 impact = World.GetScreenPosition(Random.NextFloat(), Random.NextFloat());
                Tile tile = Map.GetTile((int)(impact.X / 16),(int)(impact.Y / 16));
                if(!tile.Solid)
                    new RainDrop(World, impact, Random.Next(20, 100));
            }

            base.Update();
        }

        public override Wait NormalTurn(Turn turn)
        {
            ProvideEffect();

            Duration--;
            if (Duration <= 0)
                this.Destroy();

            return base.NormalTurn(turn);
        }

        public void ProvideEffect()
        {
            foreach (Creature creature in Map.Creatures)
            {
                creature.AddStatusEffect(new Wet()
                {
                    Buildup = 1.0,
                    Duration = new Slider(10),
                });
            }
        }
    }

    class CloudPoisonSmoke : Cloud
    {
        int Ticks;

        public CloudPoisonSmoke(Map map) : base(map)
        {
            Name = "Poison Smoke Cloud";
            Description = "Poisonous smoke.";
        }

        public override void Update()
        {
            SpriteReference smoke = SpriteLoader.Instance.AddSprite("content/cloud_big");

            Ticks++;

            foreach (var part in Parts)
            {
                if ((part.GetHashCode() + Ticks) % 7 == 0)
                {
                    Vector2 pos = new Vector2(16 * part.Tile.X + Random.Next(16), 16 * part.Tile.Y + Random.Next(16));
                    new SmokeSmall(World, smoke, pos, Vector2.Zero, new Color(185, 13, 242), 24);
                }
            }
            base.Update();
        }

        public override Wait NormalTurn(Turn turn)
        {
            Drift(0, 1, Parts.Count / 2);
            Drift(0, -1, Parts.Count / 2);
            Drift(1, 0, Parts.Count / 2);
            Drift(-1, 0, Parts.Count / 2);

            return base.NormalTurn(turn);
        }
    }

    class CloudIce : Cloud
    {
        int Ticks;

        public CloudIce(Map map) : base(map)
        {
            Name = "Blizzard";
            Description = $"Deals {Element.Ice.FormatString} damage every turn.";
        }

        public override void Update()
        {
            SpriteReference smoke = SpriteLoader.Instance.AddSprite("content/cloud_big");

            Ticks++;

            foreach (var part in Parts)
            {
                if ((part.GetHashCode() + Ticks) % 7 == 0)
                {
                    Vector2 pos = new Vector2(16 * part.Tile.X + Random.Next(16), 16 * part.Tile.Y + Random.Next(16));
                    new SmokeSmallAdditive(World, smoke, pos, Vector2.Zero, new Color(64, 128, 255), 24);
                }
            }
            base.Update();
        }

        public override Wait NormalTurn(Turn turn)
        {
            HashSet<Creature> targets = new HashSet<Creature>();

            foreach(var part in Parts)
            {
                targets.AddRange(part.Tile.Creatures);
            }

            foreach(var target in targets)
            {
                target.AttackSelf(IceAttack);
                target.TakeDamage(10, Element.Ice);
                target.CheckDead(Vector2.Zero);
            }

            Drift(0, 1, Parts.Count / 2);
            Drift(0, -1, Parts.Count / 2);
            Drift(1, 0, Parts.Count / 2);
            Drift(-1, 0, Parts.Count / 2);

            return base.NormalTurn(turn);
        }

        private Attack IceAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);

            return attack;
        }
    }
}
