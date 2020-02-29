using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;

namespace RoguelikeEngine
{
    abstract class Tile : IEffectHolder
    {
        protected MapTile Parent;
        public ReusableID ObjectID
        {
            get;
            set;
        }
        public Map Map => Parent.Map;
        public int X => Parent.X;
        public int Y => Parent.Y;
        public Tile Under => Parent.UnderTile;

        public string Name;

        public Tile NewTile => Parent.Tile;
        public IEnumerable<Creature> Creatures => Parent.GetEffects<Effects.OnTile>().Select(x => x.Holder).OfType<Creature>();
        public IEnumerable<Item> Items => Parent.GetEffects<Effects.OnTile>().Select(x => x.Holder).OfType<Item>();

        List<Effect> TileEffects = new List<Effect>();

        public Tile(string name)
        {
            ObjectID = EffectManager.NewID();
            Name = name;
        }

        public void AddTileEffect(Effect effect)
        {
            effect.Apply();
            TileEffects.Add(effect);
        }

        public IEnumerable<Effect> GetTileEffects()
        {
            return TileEffects.GetAndClean(effect => effect.Removed);
        }

        public void Replace(Tile newTile)
        {
            Parent.Set(NewTile);
        }

        public void SetParent(MapTile parent)
        {
            Parent = parent;
        }

        public Tile GetNeighbor(int dx, int dy)
        {
            return Map.GetTile(X + dx, Y + dy);
        }

        public void AddPrimary(IEffectHolder holder)
        {
            Effect.Apply(new Effects.OnTile.Primary(Parent, holder));
        }

        public void Add(IEffectHolder holder)
        {
            Effect.Apply(new Effects.OnTile(Parent, holder));
        }

        public virtual void AddTooltip(ref string tooltip)
        {
            foreach (Creature creature in Creatures)
            {
                creature.AddTooltip(ref tooltip);
                tooltip += "\n";
            }
            foreach (Item item in Items)
            {
                item.AddTooltip(ref tooltip);
                tooltip += "\n";
            }
        }

        public abstract void Draw(SceneGame scene);
    }

    class FloorCave : Tile
    {
        public FloorCave() : base("Cave Floor")
        {
        }

        public override void Draw(SceneGame scene)
        {
            var floor = SpriteLoader.Instance.AddSprite("content/tile_floor");
            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.DarkGoldenrod, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.Gray, 0);
        }
    }

    class WallCave : Tile
    {
        public WallCave() : base("Cave Wall")
        {
        }

        public override void Draw(SceneGame scene)
        {
            var cave0 = SpriteLoader.Instance.AddSprite("content/cave_base");
            var cave1 = SpriteLoader.Instance.AddSprite("content/cave_layer");
            scene.DrawSprite(cave0, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.Brown, 0);
            scene.DrawSprite(cave1, 0, new Vector2(16 * Parent.X, 16 * Parent.Y), Microsoft.Xna.Framework.Graphics.SpriteEffects.None, Color.Goldenrod, 0);
        }
    }

    class Smelter : Tile
    {
        public Smelter() : base("Smelter")
        {
        }

        public override void Draw(SceneGame scene)
        {
            
        }
    }
}
