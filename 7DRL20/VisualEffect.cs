using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class VisualEffect : IGameObject
    {
        public SceneGame World { get; set; }
        public double DrawOrder => 0;
        bool IGameObject.Destroyed { get; set; }

        public Slider Frame;

        public VisualEffect(SceneGame world)
        {
            World = world;
            World.ToAdd.Enqueue(this);
        }

        public void OnDestroy()
        {
            //NOOP
        }

        public virtual void Update()
        {
            Frame += 1;
        }

        public abstract IEnumerable<DrawPass> GetDrawPasses();

        public abstract void Draw(SceneGame scene, DrawPass pass);
    }

    class CurrentSkill : VisualEffect
    {
        public Skill Skill;

        public CurrentSkill(SceneGame world, Skill skill, int time) : base(world)
        {
            Skill = skill;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
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

    abstract class Particle : VisualEffect
    {
        public virtual Vector2 Position
        {
            get;
            set;
        }

        public Particle(SceneGame world, Vector2 position) : base(world)
        {
            Position = position;
        }
    }

    class Explosion : Particle
    {
        public Vector2 Velocity;

        public Explosion(SceneGame world, Vector2 position, Vector2 velocity, int time) : base(world, position)
        {
            Frame = new Slider(time);
            Velocity = velocity;
        }

        public override void Update()
        {
            base.Update();
            Position += Velocity;
            if (Frame.Done)
                this.Destroy();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var explosion = SpriteLoader.Instance.AddSprite("content/explosion");
            scene.DrawSprite(explosion, scene.AnimationFrame(explosion, Frame.Time, Frame.EndTime), Position - explosion.Middle, SpriteEffects.None, 0);
        }
    }

    class Smoke : Explosion
    {
        public Smoke(SceneGame world, Vector2 position, Vector2 velocity, int time) : base(world, position, velocity, time)
        {
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var explosion = SpriteLoader.Instance.AddSprite("content/smoke");
            scene.DrawSprite(explosion, scene.AnimationFrame(explosion, Frame.Time, Frame.EndTime), Position - explosion.Middle, SpriteEffects.None, 0);
        }
    }

    class WaterSplash : Explosion
    {
        public WaterSplash(SceneGame world, Vector2 position, Vector2 velocity, int time) : base(world, position, velocity, time)
        {
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var explosion = SpriteLoader.Instance.AddSprite("content/splash");
            scene.DrawSprite(explosion, scene.AnimationFrame(explosion, Frame.Time, Frame.EndTime), Position - explosion.Middle, SpriteEffects.None, 0);
        }
    }

    class LightningFlash : Explosion
    {
        public LightningFlash(SceneGame world, Vector2 position, Vector2 velocity, int time) : base(world, position, velocity, time)
        {
        }

        public override void Update()
        {
            base.Update();
            if(Frame.Done)
            {
                new LightningExplosion(World, Position + new Vector2(4, -8), Velocity, 15);
                new LightningExplosion(World, Position + new Vector2(-4, 8), Velocity, 15);
            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var explosion = SpriteLoader.Instance.AddSprite("content/lightning_flash");
            scene.DrawSprite(explosion, scene.AnimationFrame(explosion, Frame.Time, Frame.EndTime), Position - explosion.Middle, SpriteEffects.None, 0);
        }
    }

    class LightningExplosion : Explosion
    {
        public LightningExplosion(SceneGame world, Vector2 position, Vector2 velocity, int time) : base(world, position, velocity, time)
        {
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {

            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var explosion = SpriteLoader.Instance.AddSprite("content/lightning_explosion");
            scene.DrawSprite(explosion, scene.AnimationFrame(explosion, Frame.Time, Frame.EndTime), Position - explosion.Middle, SpriteEffects.None, 0);
        }
    }

    abstract class Projectile : Particle
    {
        public override Vector2 Position { get => Tween; set {} }
        public abstract Vector2 Tween
        {
            get;
        }

        public Vector2 PositionStart;
        public Vector2 PositionEnd;

        public Projectile(SceneGame world, Vector2 positionStart, Vector2 positionEnd, int time) : base(world, Vector2.Zero)
        {
            Frame = new Slider(time);
            PositionStart = positionStart;
            PositionEnd = positionEnd;
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {
                Impact(Position);
                this.Destroy();
            }
        }

        public abstract void Impact(Vector2 position);
    }

    class Lightning : Projectile
    {
        public int TrailLength;
        public Vector2 TweenTrail => Vector2.Lerp(PositionStart, PositionEnd, MathHelper.Clamp((float)(Frame.Time - TrailLength) / (Frame.EndTime - TrailLength * 2), 0, 1));
        public override Vector2 Tween => Vector2.Lerp(PositionStart, PositionEnd, MathHelper.Clamp((float)(Frame.Time) / (Frame.EndTime - TrailLength * 2), 0, 1));

        public Lightning(SceneGame world, Vector2 positionStart, Vector2 positionEnd, int time, int trail) : base(world, positionStart, positionEnd, time + trail * 2)
        {
            TrailLength = trail;
        }

        public override void Impact(Vector2 position)
        {
            new LightningFlash(World, position, Vector2.Zero, 6);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var beam = SpriteLoader.Instance.AddSprite("content/lightning");

            Vector2 point1 = TweenTrail;
            Vector2 point2 = Tween;

            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            int length = (int)(point2 - point1).Length();
            scene.PushSpriteBatch(samplerState: SamplerState.PointWrap);
            scene.SpriteBatch.Draw(beam.Texture, point1, new Rectangle((int)Frame.Time * 2, 0, length, beam.Height), Color.White, angle, new Vector2(0, beam.Height / 2), 1.0f, SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }
    }

    class DamagePopup : Particle
    {
        public string Text;
        public TextParameters Parameters;
        public Vector2 Offset => new Vector2(0, -16) * (float)LerpHelper.QuadraticOut(0, 1, Frame.Slide);

        public DamagePopup(SceneGame world, Vector2 position, string text, TextParameters parameters, int time) : base(world, position)
        {
            Text = text;
            Parameters = parameters;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.UI;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            string fit = FontUtil.FitString(Text, Parameters);
            var height = FontUtil.GetStringHeight(fit);
            Vector2 pos = Vector2.Transform(Position + Offset, scene.WorldTransform);
            scene.DrawText(Text, pos - new Vector2(0, height / 2), Alignment.Center, Parameters);
        }
    }
}
