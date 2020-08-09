using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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
        protected Map Map;
        protected List<CloudPart> Parts = new List<CloudPart>();
        
        public Cloud(Map map)
        {
            World = map.World;
            World.ToAdd.Enqueue(this);
            ObjectID = EffectManager.NewID(this);
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

    class CloudSmoke : Cloud
    {
        int Ticks;

        public CloudSmoke(Map map) : base(map)
        {
        }

        public override void Update()
        {
            Ticks++;

            foreach (var part in Parts)
            {
                if((part.GetHashCode() + Ticks) % 20 == 0)
                {
                    new Smoke(World, part.Tile.VisualTarget, Vector2.Zero, 0, 12);
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
}
