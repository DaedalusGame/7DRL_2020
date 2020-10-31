using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.VisualEffects
{
    class Anchored : VisualEffect
    {
        public Func<Vector2> Anchor;

        public Action<Anchored> OnUpdate;

        public Anchored(SceneGame world, Func<Vector2> anchor, int time) : base(world)
        {
            Anchor = anchor;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            OnUpdate?.Invoke(this);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }
    }

    class Cloak : VisualEffect
    {
        public Creature Creature;

        public Action<Cloak> OnUpdate;

        public Cloak(Creature creature, int time) : base(creature.World)
        {
            Creature = creature;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            OnUpdate?.Invoke(this);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }

        public static void PowerUp(Cloak cloak, int modulo, ColorMatrix color, LerpHelper.Delegate scaleLerp, LerpHelper.Delegate colorLerp, int time)
        {
            if(cloak.Frame.Time % modulo == 0)
            {
                var afterimage = new ParticleAfterImageLocked(cloak.Creature, scaleLerp, colorLerp, time)
                {
                    Position = cloak.Creature.VisualPosition(),
                    ColorMatrix = color,
                    Scale = 1f,
                    ScaleEnd = 2f,
                    Pass = DrawPass.EffectAdditive,
                    Origin = cloak.Creature.BottomOffset,
                };
            }
        }

        public static void PowerDown(Cloak cloak, int modulo, ColorMatrix color, LerpHelper.Delegate scaleLerp, LerpHelper.Delegate colorLerp, int time)
        {
            if (cloak.Frame.Time % modulo == 0)
            {
                var afterimage = new ParticleAfterImageLocked(cloak.Creature, scaleLerp, colorLerp, time)
                {
                    Position = cloak.Creature.VisualPosition(),
                    ColorMatrix = color,
                    Scale = 1f,
                    ScaleEnd = 2f,
                    Pass = DrawPass.Effect,
                    Origin = cloak.Creature.TopOffset,
                };
            }
        }
    }

    class Field : VisualEffect
    {
        public List<Tile> Tiles = new List<Tile>();

        public Action<Field> OnUpdate;

        public Field(SceneGame world, IEnumerable<Tile> tiles, int time) : base(world)
        {
            Tiles.AddRange(tiles);
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            OnUpdate?.Invoke(this);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            //NOOP
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            return Enumerable.Empty<DrawPass>();
        }

        public static void FieldLightning(Field field, VisualPreset.BetweenPositions arc, float chanceStart, float chanceEnd, LerpHelper.Delegate chanceLerp)
        {
            if (Random.NextDouble() < chanceLerp(chanceStart, chanceEnd, field.Frame.Slide) && field.Tiles.Any())
            {
                Tile startTile = field.Tiles.Pick(Random);
                Tile endTile = field.Tiles.Pick(Random);
                if (startTile != endTile)
                {
                    Vector2 startOffset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                    Vector2 endOffset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                    arc.Activate(startTile.VisualTarget + startOffset, endTile.VisualTarget + endOffset);
                }
            }
        }

        public static void FieldExplosion(Field field, VisualPreset.AtPosition explosion, float chanceStart, float chanceEnd, LerpHelper.Delegate chanceLerp)
        {
            if (Random.NextDouble() < chanceLerp(chanceStart, chanceEnd, field.Frame.Slide) && field.Tiles.Any())
            {
                Tile tile = field.Tiles.Pick(Random);
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                explosion.Activate(tile.VisualTarget + offset);
            }
        }
    }
}
