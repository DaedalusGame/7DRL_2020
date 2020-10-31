using LibNoise.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RoguelikeEngine.VisualEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    abstract class VisualEffect : IGameObject
    {
        public static Random Random = new Random();

        public SceneGame World { get; set; }
        public Map Map { get; set; }
        public double DrawOrder => 0;
        public bool Destroyed { get; set; }

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

        public virtual bool ShouldDraw(Map map)
        {
            return true;
        }
    }

    /*class CurrentSkill : VisualEffect
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
    }*/

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

    class Blob : Particle
    {
        public SpriteReference Sprite;
        public Vector2 EndPosition;
        public float Size;
        public Color Color;

        public Blob(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, float size, Color color, int time) : base(world, position)
        {
            Sprite = sprite;
            Frame = new Slider(time);
            EndPosition = Position + velocity * time;
            Size = size;
            Color = color;
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            Vector2 truePos = Vector2.Lerp(Position, EndPosition, (float)LerpHelper.QuadraticOut(0, 1, Frame.Slide));
            float trueSize = (float)LerpHelper.QuadraticOut(Size, 0, Frame.Slide);
            scene.DrawSpriteExt(Sprite, scene.AnimationFrame(Sprite, Frame.Time, Frame.EndTime), truePos - Sprite.Middle, Sprite.Middle, 0, new Vector2(trueSize), SpriteEffects.None, Color, 0);
        }
    }

    class Explosion : Particle
    {
        public SpriteReference Sprite;
        public Vector2 Velocity;
        public float Angle;

        public Explosion(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, float angle, int time) : base(world, position)
        {
            Sprite = sprite;
            Frame = new Slider(time);
            Velocity = velocity;
            Angle = angle;
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
            scene.DrawSpriteExt(Sprite, scene.AnimationFrame(Sprite, Frame.Time, Frame.EndTime), Position - Sprite.Middle, Sprite.Middle, Angle, SpriteEffects.None, 0);
        }
    }

    class FireExplosion : Explosion
    {
        public FireExplosion(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/explosion"), position, velocity, angle, time)
        {
        }
    }

    class SteamExplosion : Explosion
    {
        public SteamExplosion(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/explosion_steam"), position, velocity, angle, time)
        {
        }
    }

    class BloodExplosion : Explosion
    {
        public BloodExplosion(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/explosion_blood"), position, velocity, angle, time)
        {
        }
    }

    class AcidExplosion : Explosion
    {
        public AcidExplosion(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/explosion_acid"), position, velocity, angle, time)
        {
        }
    }

    class FlameBig : Explosion
    {
        public FlameBig(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/fire_big"), position, velocity, angle, time)
        {
        }
    }

    class FlameSmall : Explosion
    {
        public FlameSmall(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/fire_small"), position, velocity, angle, time)
        {
        }
    }

    class Smoke : Explosion
    {
        public Smoke(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/smoke"), position, velocity, angle, time)
        {
        }
    }

    class WaterSplash : Explosion
    {
        public WaterSplash(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/splash"), position, velocity, angle, time)
        {
        }
    }

    class ChaosSplash : Explosion
    {
        public ChaosSplash(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/splash_chaos"), position, velocity, angle, time)
        {
        }
    }

    class LightningFlash : Explosion
    {
        public LightningFlash(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/lightning_flash"), position, velocity, angle, time)
        {
        }

        public override void Update()
        {
            base.Update();
            if(Frame.Done)
            {
                new LightningExplosion(World, Position + new Vector2(4, -8), Velocity, 0, 15);
                new LightningExplosion(World, Position + new Vector2(-4, 8), Velocity, 0, 15);
            }
        }
    }

    class LightningExplosion : Explosion
    {
        public LightningExplosion(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/lightning_explosion"), position, velocity, angle, time)
        {
        }
    }

    class EnderExplosion : Explosion
    {
        public EnderExplosion(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/ender_explosion"), position, velocity, angle, time)
        {
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class Nuke : Particle
    {
        protected SpriteReference Sprite;
        protected float MaxScale;
        protected float Scale => (float)LerpHelper.Linear(0, MaxScale, GetScaleFunction(Frame.Slide));

        public Nuke(SceneGame world, SpriteReference sprite, Vector2 position, float scale, int time) : base(world, position)
        {
            Sprite = sprite;
            Frame = new Slider(time);
            MaxScale = scale;
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
        }

        protected double GetScaleFunction(double amount)
        {
            if (amount < 0.33)
                return LerpHelper.SineOut(0, 1, amount / 0.33);
            else if (amount < 0.66)
                return 1;
            else
                return LerpHelper.CubicIn(1, 0, (amount - 0.66) / 0.33);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, 0, new Vector2(Scale), SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class FireNuke : Nuke
    {
        public FireNuke(SceneGame world, SpriteReference sprite, Vector2 position, float scale, int time) : base(world, sprite, position, scale, time)
        {
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < 2; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = Random.NextFloat() * Sprite.Width * 0.5f * Scale;
                Vector2 offset = Util.AngleToVector(angle) * distance;
                Vector2 velocity = Util.AngleToVector(angle) * (Random.NextFloat() + 0.5f);
                new Cinder(World, SpriteLoader.Instance.AddSprite("content/cinder"), Position + offset, velocity, (int)Math.Min(Random.Next(90) + 90, Frame.EndTime - Frame.Time));
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = Random.NextFloat() * 200;
                Vector2 offset = Util.AngleToVector(angle) * distance;
                Vector2 velocity = Util.AngleToVector(angle) * (Random.NextFloat() + 0.5f) * 3 + new Vector2(0f, -1.5f);
                new Cinder(World, SpriteLoader.Instance.AddSprite("content/cinder"), Position + offset, velocity, Random.Next(20) + 20);
            }
        }
    }

    class EnderNuke : Nuke
    {
        public EnderNuke(SceneGame world, SpriteReference sprite, Vector2 position, float scale, int time) : base(world, sprite, position, scale, time)
        {
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < 2; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = Random.NextFloat() * Sprite.Width * 0.5f * Scale;
                Vector2 offset = Util.AngleToVector(angle) * distance;
                Vector2 velocity = Util.AngleToVector(angle) * (Random.NextFloat() + 0.5f);
                new Cinder(World, SpriteLoader.Instance.AddSprite("content/cinder_ender"), Position + offset, velocity, (int)Math.Min(Random.Next(90) + 90, Frame.EndTime - Frame.Time));
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = Random.NextFloat() * 200;
                Vector2 offset = Util.AngleToVector(angle) * distance;
                Vector2 velocity = Util.AngleToVector(angle) * (Random.NextFloat() + 0.5f) * 3 + new Vector2(0f,-1.5f);
                new Cinder(World, SpriteLoader.Instance.AddSprite("content/cinder_ender"), Position + offset, velocity, Random.Next(20) + 20);
            }

            if (Random.Next(10) < 10 && Frame.Slide < 0.66)
            {
                SpriteReference smoke;
                if(Random.NextDouble() < 0.4)
                    smoke = SpriteLoader.Instance.AddSprite("content/smoke_wave_big");
                else
                    smoke = SpriteLoader.Instance.AddSprite("content/smoke_wave");
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = Random.NextFloat() * 300;
                Vector2 offset = Util.AngleToVector(angle) * distance;
                Vector2 velocity = Util.AngleToVector(angle) * (Random.NextFloat() + 0.5f) * 5;
                new SmokeWave(World, smoke, Position + offset, velocity, Random.Next(20) + 20);
            }
        }
    }

    delegate Explosion ExplosionGenerator(Vector2 position, Vector2 velocity, float angle, int time);

    class BigExplosion : VisualEffect
    {
        Func<Vector2> Anchor;
        ExplosionGenerator Generator;

        public BigExplosion(SceneGame world, Func<Vector2> anchor, ExplosionGenerator generator) : base(world)
        {
            Frame = new Slider(10);
            Anchor = anchor;
            Generator = generator;
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();

            var position = Anchor();
            if (Frame.Time == 1)
            {
                Generator(position + new Vector2(0, 0), Vector2.Zero, 0, 30);
            }
            if (Frame.Time == 5)
            {
                Generator(position + new Vector2(16, 0), Vector2.Zero, 0, 15);
                Generator(position + new Vector2(0, 16), Vector2.Zero, 0, 15);
                Generator(position + new Vector2(-16, 0), Vector2.Zero, 0, 15);
                Generator(position + new Vector2(0, -16), Vector2.Zero, 0, 15);
            }
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

    class RingExplosion : VisualEffect
    {
        Vector2 Position;
        ExplosionGenerator Generator;
        int Count;
        float Distance;
        int Time;

        public RingExplosion(SceneGame world, Vector2 position, ExplosionGenerator generator, int count, float distance, int time) : base(world)
        {
            Position = position;
            Generator = generator;
            Count = count;
            Distance = distance;
            Time = time;
            Frame = new Slider(1);
        }

        public override void Update()
        {
            base.Update();

            for (int i = 0; i < Count; i++)
            {
                float angle = i * MathHelper.TwoPi / Count;
                Vector2 offset = Util.AngleToVector(angle) * Distance;

                Generator(Position + offset, Vector2.Zero, angle, Time + Random.Next(5));
            }

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

    class MultiExplosion : VisualEffect
    {
        Func<Vector2> Anchor;
        ExplosionGenerator Generator;
        Vector2 Velocity;

        public MultiExplosion(SceneGame world, Func<Vector2> anchor, Vector2 velocity, ExplosionGenerator generator, int time) : base(world)
        {
            Frame = new Slider(time);
            Anchor = anchor;
            Velocity = velocity;
            Generator = generator;
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();

            var position = Anchor();
            if ((Frame.EndTime - Frame.Time) % 10 == 0)
            {
                Generator(position, Velocity, 0, 30);
            }
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

    class BossExplosion : VisualEffect
    {
        Creature Anchor;
        ExplosionGenerator Generator;

        public BossExplosion(SceneGame world, Creature anchor, ExplosionGenerator generator) : base(world)
        {
            Frame = new Slider(1000000);
            Anchor = anchor;
            Generator = generator;
        }

        public override void Update()
        {
            base.Update();
            if (Anchor.Destroyed)
                this.Destroy();

            if ((Frame.EndTime - Frame.Time) % 20 == 0)
            {
                Vector2 emitPos = Anchor.VisualPosition() + Anchor.Mask.GetRandomPixel(Random);
                Generator(emitPos, Vector2.Zero, 0, 15);
                new ScreenShakeRandom(World, 3, 15, LerpHelper.Linear);
            }
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

    class GreenBlobPop : Explosion
    {
        public GreenBlobPop(SceneGame world, Vector2 position, Vector2 velocity, float angle, int time) : base(world, SpriteLoader.Instance.AddSprite("content/pop_blob_green"), position, velocity, angle, time)
        {
        }
    }

    class Volt : Particle
    {
        float Angle;
        SpriteReference Sprite;
        int SubImage;

        public Volt(SceneGame world, SpriteReference sprite, Vector2 position, int time) : base(world, position)
        {
            Sprite = sprite;
            SubImage = Random.Next(sprite.SubImageCount);
            Angle = Random.NextFloat() * MathHelper.TwoPi;
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
            scene.DrawSpriteExt(Sprite, SubImage, Position - Sprite.Middle, Sprite.Middle, Angle, Vector2.One, SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class Cutter : Particle
    {
        Func<Vector2> Anchor;
        Func<float, float> Scale;
        float Angle;
        Func<float, float> Speed;
        Color Color;

        public override Vector2 Position
        {
            get
            {
                return Anchor();
            }
            set
            {
                //NOOP
            }
        }

        public Cutter(SceneGame world, Func<Vector2> anchor, Func<float, float> speed, Func<float, float> scale, float angle, Color color, int time) : base(world, Vector2.Zero)
        {
            Anchor = anchor;
            Speed = speed;
            Color = color;
            Scale = scale;
            Angle = angle;
            Frame = new Slider(time);
        }

        public Cutter(SceneGame world, Func<Vector2> anchor, float speedStart, float speedEnd, LerpHelper.Delegate speedLerp, float scale, float angle, Color color, int time) : base(world, Vector2.Zero)
        {
            Anchor = anchor;
            Speed = (slide) => (float)speedLerp(speedStart, speedEnd, slide);
            Color = color;
            Scale = (slide) => scale;
            Angle = angle;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            Angle += Speed(Frame.Slide);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var cutter = SpriteLoader.Instance.AddSprite("content/cutter");

            scene.DrawSpriteExt(cutter, scene.AnimationFrame(cutter, Frame.Time - (Frame.EndTime - 4), 4), Position - cutter.Middle, cutter.Middle, Angle, new Vector2(Scale(Frame.Slide)), SpriteEffects.None, Color, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class RainDrop : Particle
    {
        int Distance;

        public RainDrop(SceneGame world, Vector2 position, int distance) : base(world, position)
        {
            Frame = new Slider(8);
            Distance = distance;
        }

        public override void Update()
        {
            base.Update();

            if (Frame.Done)
                this.Destroy();
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var rain = SpriteLoader.Instance.AddSprite("content/rain");

            float impactLength = 4;
            float moveLength = Frame.EndTime - impactLength;

            float moveSlide = 1 - MathHelper.Clamp(Frame.Time / moveLength, 0, 1);
            Vector2 offset = new Vector2(1,-4) * Distance * moveSlide;

            var middle = new Vector2(rain.Width / 2, 28);

            scene.DrawSpriteExt(rain, scene.AnimationFrame(rain, Frame.Time - moveLength, impactLength), Position + offset - middle, middle, 0, SpriteEffects.None, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class SmokeSmall : Particle
    {
        SpriteReference Sprite;
        int SubImage;
        Vector2 Velocity;
        float Scale => (float)LerpHelper.CubicIn(1, 0, Frame.Slide);
        Color Color;

        public SmokeSmall(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, Color color, int time) : base(world, position)
        {
            Sprite = sprite;
            SubImage = Random.Next(Sprite.SubImageCount);
            Velocity = velocity;
            Color = color;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            Position += Velocity;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var angle = Util.VectorToAngle(Velocity);
            scene.DrawSpriteExt(Sprite, SubImage, Position - Sprite.Middle, Sprite.Middle, angle - MathHelper.PiOver2, new Vector2(Scale), SpriteEffects.None, Color, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class SmokeSmallAdditive : SmokeSmall
    {
        public SmokeSmallAdditive(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, Color color, int time) : base(world, sprite, position, velocity, color, time)
        {

        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class SmokeSpores : Particle
    {
        SpriteReference Sprite;
        Vector2 Velocity;
        Color Color;
        int OffsetX, OffsetY;

        public SmokeSpores(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, Color color, int time) : base(world, position)
        {
            Sprite = sprite;
            Velocity = velocity;
            Color = color;
            Frame = new Slider(time);
            OffsetX = Random.Next(sprite.Width);
            OffsetY = Random.Next(sprite.Height);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            Position += Velocity;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            float alpha;
            if (Frame.Time < Frame.EndTime / 2)
                alpha = (float)LerpHelper.QuadraticIn(0, 1, Frame.GetSubSlide(0, Frame.EndTime / 7));
            else
                alpha = (float)LerpHelper.QuadraticOut(1, 0, Frame.GetSubSlide(Frame.EndTime / 2, Frame.EndTime));
            scene.SpriteBatch.Draw(Sprite.Texture, Position - Sprite.Middle / 2, new Rectangle(OffsetX, OffsetY, Sprite.Width / 2, Sprite.Height / 2), Color.WithAlpha(alpha));
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class SmokeWave : Particle
    {
        SpriteReference Sprite;
        Vector2 Velocity;

        public SmokeWave(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, int time) : base(world, position)
        {
            Sprite = sprite;
            Velocity = velocity;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            Position += Velocity;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var angle = Util.VectorToAngle(Velocity);
            if(Frame.Time % 3 == 1)
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, angle - MathHelper.PiOver2, Vector2.One, SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectLow;
        }
    }

    class FireField : VisualEffect
    {
        public List<Tile> Tiles = new List<Tile>();

        public FireField(SceneGame world, IEnumerable<Tile> tiles, int time) : base(world)
        {
            Tiles.AddRange(tiles);
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();

            if (Random.NextDouble() < LerpHelper.Linear(0, 0.5, Frame.Slide) && Tiles.Any())
            {
                Tile startTile = Tiles.Pick(Random);
                Vector2 startOffset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                new FlameSmall(World, startTile.VisualTarget + startOffset, Vector2.Zero, 0, 8);
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = Random.NextFloat() * 200;
                Vector2 offset = Util.AngleToVector(angle) * distance;
                Vector2 velocity = Util.AngleToVector(angle) * (Random.NextFloat() + 0.5f) * 1 + new Vector2(0f, -1.5f);
                new Cinder(World, SpriteLoader.Instance.AddSprite("content/cinder"), startTile.VisualTarget + startOffset, velocity, Random.Next(20) + 60);
            }
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

    class LightningField : VisualEffect
    {
        public SpriteReference Sprite;
        public List<Tile> Tiles = new List<Tile>();

        public LightningField(SceneGame world, SpriteReference sprite, IEnumerable<Tile> tiles, int time) : base(world)
        {
            Tiles.AddRange(tiles);
            Sprite = sprite;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();

            if (Random.NextDouble() < LerpHelper.Linear(0,0.5,Frame.Slide) && Tiles.Any())
            {
                Tile startTile = Tiles.Pick(Random);
                Tile endTile = Tiles.Pick(Random);
                if (startTile != endTile)
                {
                    Vector2 startOffset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                    Vector2 endOffset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                    new LightningSpark(World, Sprite, startTile.VisualTarget + startOffset, endTile.VisualTarget + endOffset, 2);
                }
            }
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

    class GeomancyPiece : Particle
    {
        Tile Tile;
        SpriteEffects Mirror;

        public override Vector2 Position
        {
            get
            {
                return Tile.VisualPosition;
            }
            set
            {
                //NOOP
            }
        }

        public GeomancyPiece(SceneGame world, Tile tile, SpriteEffects mirror, int time) : base(world, Vector2.Zero)
        {
            Tile = tile;
            Frame = new Slider(time);
            Mirror = mirror;
        }

        public override void Update()
        {
            base.Update();

            if (Frame.Done)
                this.Destroy();
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            SpriteReference geo = SpriteLoader.Instance.AddSprite("content/geotile");
            scene.DrawSprite(geo, 0, Position, Mirror, Color.Goldenrod * (1 - Frame.Slide), 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectLowAdditive;
        }
    }

    class GeomancyField : VisualEffect
    {
        struct GeoPiece
        {
            public Tile Tile;
            public float Distance;
            public SpriteEffects Mirror;

            public GeoPiece(Tile tile, float distance, SpriteEffects mirror)
            {
                Tile = tile;
                Distance = distance;
                Mirror = mirror;
            }
        }

        List<GeoPiece> Pieces = new List<GeoPiece>();
        float MaxDistance;

        public GeomancyField(SceneGame world, Tile center, IEnumerable<Tile> tiles, int time) : base(world)
        {
            foreach(Tile tile in tiles)
            {
                int dx = tile.X - center.X;
                int dy = tile.Y - center.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance > MaxDistance)
                    MaxDistance = distance;

                if (tile.Opaque || tile.Solid)
                    continue;

                if(Random.NextDouble() < 0.3)
                {
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.None));
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.FlipHorizontally));
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.FlipVertically));
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically));
                }
                else if(Random.NextDouble() < 0.5)
                {
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.FlipHorizontally));
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.FlipVertically));
                }
                else
                {
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.None));
                    Pieces.Add(new GeoPiece(tile, distance + Random.NextFloat() * 3, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically));
                }
            }
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();

            if(Frame.Done)
                this.Destroy();

            double distance = LerpHelper.QuadraticIn(0, MaxDistance, Frame.Slide);
            foreach(GeoPiece piece in Pieces.Where(x => x.Distance < distance))
            {
                new GeomancyPiece(World,piece.Tile,piece.Mirror,12 + Random.Next(30));
            }
            Pieces.RemoveAll(x => x.Distance < distance);
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

    class TileExplosion : VisualEffect
    {
        public TileExplosion(SceneGame world, IEnumerable<Tile> tiles) : base(world)
        {
            new ScreenShakeRandom(World, 8, 60, LerpHelper.Linear);
            foreach (Tile tile in tiles)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.7)
                    new FireExplosion(World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
            }
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

    abstract class Projectile : Particle
    {
        public override Vector2 Position { get => Tween; set {} }
        public abstract Vector2 Tween
        {
            get;
        }

        public Vector2 PositionStart;
        public Vector2 PositionEnd;
        public bool Hit;

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

        public virtual void Impact(Vector2 position)
        {
            Hit = true;
        }
    }

    class ProjectileEmitter : VisualEffect
    {
        Func<Vector2> Start, End;
        Func<Vector2, Vector2, Projectile> Emit;

        public ProjectileEmitter(SceneGame world, Func<Vector2> start, Func<Vector2> end, int time, Func<Vector2, Vector2, Projectile> emit) : base(world)
        {
            Frame = new Slider(time);
            Start = start;
            End = end;
            Emit = emit;
        }

        public override void Update()
        {
            base.Update();

            if(Frame.Time % 3 == 0)
            {
                Projectile projectile = Emit(Start(), End());
            }

            if (Frame.Done)
            {
                this.Destroy();
            }
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

    class Ball : Projectile
    {
        public SpriteReference Sprite;
        public LerpHelper.Delegate Lerp;
        public override Vector2 Tween => Vector2.Lerp(PositionStart, PositionEnd, (float)Lerp(0, 1, Frame.Slide));
       
        public Ball(SceneGame world, SpriteReference sprite, Vector2 positionStart, Vector2 positionEnd, LerpHelper.Delegate lerp, int time) : base(world, positionStart, positionEnd, time)
        {
            Sprite = sprite;
            Lerp = lerp;
        }
        
        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, 0, new Vector2(1), SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class EnergyBall : Projectile
    {
        public SpriteReference Sprite;
        public SpriteReference TrailSprite;
        public LerpHelper.Delegate Lerp;
        public override Vector2 Tween => GetTween();
        public float AngleVelocity;
        public float Distance;
        public float Angle1, Angle2;
        public float Rotation;

        public EnergyBall(SceneGame world, SpriteReference sprite, SpriteReference trailSprite, Vector2 positionStart, Vector2 positionEnd, float angleVelocity, float rotation, float distance, LerpHelper.Delegate lerp, int time) : base(world, positionStart, positionEnd, time)
        {
            Sprite = sprite;
            TrailSprite = trailSprite;
            Lerp = lerp;
            Angle1 = Random.NextFloat() * MathHelper.TwoPi;
            Angle2 = Random.NextFloat() * MathHelper.TwoPi;
            Frame = new Slider(time);
            AngleVelocity = angleVelocity;
            Distance = distance;
            Rotation = rotation;
        }

        private Vector2 GetTween()
        {
            var offsetStart = Util.AngleToVector(Angle1 + AngleVelocity * Frame.Slide) * (float)Lerp(0, Distance, Frame.Slide);
            var offsetEnd = Util.AngleToVector(Angle2 + AngleVelocity * Frame.Slide) * (float)Lerp(Distance, 0, Frame.Slide);
            return Vector2.Lerp(PositionStart + offsetStart, PositionEnd + offsetEnd, (float)Lerp(0, 1, Frame.Slide));
        }

        public override void Update()
        {
            base.Update();
            if (TrailSprite != null)
            {
                var offset = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * Sprite.Width / 2f;
                new Volt(World, TrailSprite, Position + offset, Random.Next(5));
            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, Rotation * Frame.Time, new Vector2(1), SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class Shards : Projectile
    {
        public SpriteReference Sprite;
        public LerpHelper.Delegate Lerp;
        public override Vector2 Tween => Vector2.Lerp(PositionStart, PositionEnd, (float)Lerp(0, 1, Frame.Slide));
        float Angle;

        public Shards(SceneGame world, SpriteReference sprite, Vector2 positionStart, Vector2 positionEnd, LerpHelper.Delegate lerp, int time) : base(world, positionStart, positionEnd, time)
        {
            Sprite = sprite;
            Lerp = lerp;
            Angle = Random.NextFloat() * MathHelper.TwoPi;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, Angle, new Vector2(1), SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class MissileHand : Projectile
    {
        public Vector2 Velocity;

        public override Vector2 Tween => Vector2.Lerp(TweenStraight, PositionEnd, (float)LerpHelper.QuadraticIn(0, 1, MoveFrame.Slide));
        Vector2 TweenStraight => Vector2.Lerp(PositionStart, PositionStart + Velocity, MoveFrame.Slide);

        Slider MoveFrame;

        float Angle => Util.VectorToAngle(PositionEnd - Tween);
        SpriteEffects Mirror;
        ColorMatrix ColorMatrix;
        float Alpha => (float)LerpHelper.QuadraticIn(1, 0, MathHelper.Clamp((Frame.Slide - 0.7f) / 0.3f, 0, 1));

        public MissileHand(SceneGame world, Vector2 positionStart, Vector2 positionEnd, Vector2 velocity, ColorMatrix colorMatrix, int moveTime, int time) : base(world, positionStart, positionEnd, time)
        {
            Velocity = velocity;
            if (Random.NextDouble() < 0.5)
                Mirror = SpriteEffects.FlipHorizontally;
            else
                Mirror = SpriteEffects.None;
            ColorMatrix = colorMatrix;
            MoveFrame = new Slider(moveTime);
        }

        public override void Update()
        {
            base.Update();

            MoveFrame += 1;

            new TrailAlpha(World, SpriteLoader.Instance.AddSprite("content/hand"), Tween, Vector2.Zero, Angle, Color.DarkGoldenrod, Mirror, 10);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            SpriteReference hand = SpriteLoader.Instance.AddSprite("content/hand");
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(ColorMatrix, matrix, projection);
            });
            scene.DrawSpriteExt(hand, 0, Position - hand.Middle, hand.Middle, Angle, new Vector2(1), Mirror, Color.White * Alpha, 0);
            scene.PopSpriteBatch();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }

    }

    abstract class Bullet : Projectile
    {
        public override Vector2 Tween => Vector2.Lerp(PositionStart, PositionEnd, MoveFrame.Slide);

        public Slider MoveFrame;

        public Bullet(SceneGame world, Vector2 positionStart, int time) : base(world, positionStart, positionStart, time)
        {
            MoveFrame = new Slider(1);
        }

        public void Setup(Vector2 position, int time)
        {
            PositionStart = PositionEnd = position;
            Frame = new Slider(time);
        }

        public void Move(Vector2 position, int time)
        {
            PositionStart = Position;
            PositionEnd = position;
            MoveFrame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            MoveFrame += 1;
        }
    }

    class Trail : Particle
    {
        public SpriteReference Sprite;
        public Vector2 Velocity;
        public float Angle;
        public Color Color;
        public SpriteEffects Mirror;

        public Trail(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, float angle, Color color, SpriteEffects mirror, int time) : base(world, position)
        {
            Sprite = sprite;
            Frame = new Slider(time);
            Velocity = velocity;
            Angle = angle;
            Color = color;
            Mirror = mirror;
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
            yield return DrawPass.EffectLow;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            Color color = new Color(Color.R, Color.G, Color.B, (int)MathHelper.Lerp(Color.A, 0, Frame.Slide));
            scene.DrawSpriteExt(Sprite, scene.AnimationFrame(Sprite, Frame.Time, Frame.EndTime), Position - Sprite.Middle, Sprite.Middle, Angle, Vector2.One, Mirror, color, 0);
        }
    }

    class TrailAlpha : Trail
    {
        public TrailAlpha(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, float angle, Color color, SpriteEffects mirror, int time) : base(world, sprite, position, velocity, angle, color, mirror, time)
        {
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectLowAdditive;
        }
    }

    class BulletNormal : Bullet
    {
        protected SpriteReference Sprite;
        protected ColorMatrix Color;
        protected double AnimationSpeed;

        public BulletNormal(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix color, float animationSpeed, int time) : base(world, positionStart, time)
        {
            Sprite = sprite;
            Color = color;
            AnimationSpeed = animationSpeed;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color, matrix, projection);
            });
            scene.DrawSpriteExt(Sprite, (int)(Frame.Time * AnimationSpeed), Position - Sprite.Middle, Sprite.Middle, 0, SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class BulletRandom : Bullet
    {
        protected SpriteReference Sprite;
        protected ColorMatrix Color;
        protected float Angle;

        public BulletRandom(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix color, int time) : base(world, positionStart, time)
        {
            Sprite = sprite;
            Color = color;
            Angle = Random.NextFloat() * MathHelper.TwoPi;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color, matrix, projection);
            });
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, Angle, SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class BulletAngular : Bullet
    {
        protected SpriteReference Sprite;
        protected ColorMatrix Color;

        public BulletAngular(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix color, int time) : base(world, positionStart, time)
        {
            Sprite = sprite;
            Color = color;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            float angle = Util.VectorToAngle(PositionEnd - PositionStart);
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color, matrix, projection);
            });
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, angle, SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class BulletArrow : BulletAngular
    {
        ToolArrow Arrow;

        public BulletArrow(SceneGame world, ToolArrow arrow, Vector2 positionStart, ColorMatrix color, int time) : base(world, null, positionStart, color, time)
        {
            Arrow = arrow;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            float angle = Util.VectorToAngle(PositionEnd - PositionStart);
            Arrow.DrawBullet(scene, Position, angle);
        }
    }

    class BulletTrail : BulletAngular
    {
        Color TrailColor;

        public BulletTrail(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix color, Color trailColor, int time) : base(world, sprite, positionStart, color, time)
        {
            TrailColor = trailColor;
        }

        public override void Update()
        {
            base.Update();
            float angle = Util.VectorToAngle(PositionEnd - PositionStart);
            new Trail(World, Sprite, Position, Vector2.Zero, angle, TrailColor, SpriteEffects.None, 10);
        }
    }

    class BulletSpeed : BulletAngular
    {
        LerpHelper.Delegate Lerp = LerpHelper.CubicOut;
        float Overshoot = 1.5f;
        public override Vector2 Tween => Vector2.Lerp(PositionStart, PositionStart + (PositionEnd - PositionStart) * Overshoot, (float)Lerp(0,1,MoveFrame.Slide));

        public BulletSpeed(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix color, int time) : base(world, sprite, positionStart, color, time)
        {
        }

        public override void Update()
        {
            base.Update();

            if(Lerp(0, Overshoot, MoveFrame.Slide) > 1 && !Hit)
            {
                Impact(Tween);
            }
        }
    }

    class BulletRock : Bullet
    {
        protected SpriteReference Sprite;
        protected ColorMatrix ColorMatrix;

        float Angle;
        float AngleVelocity;

        float InitialScale = 1;
        float Scale => (float)LerpHelper.QuadraticIn(InitialScale, 0, MathHelper.Clamp((Frame.Slide - 0.7f) / 0.3f, 0, 1));

        public BulletRock(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix colorMatrix, float angleVelocity, int time) : base(world, positionStart, time)
        {
            Sprite = sprite;
            ColorMatrix = colorMatrix;
            Angle = Random.NextFloat() * MathHelper.TwoPi;
            AngleVelocity = angleVelocity;
        }

        public override void Update()
        {
            base.Update();

            Angle += AngleVelocity;

            if (Frame.Done)
                this.Destroy();

            new Trail(World, Sprite, Position, Vector2.Zero, Angle, Color.Orange, SpriteEffects.None, 10);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(ColorMatrix, matrix, projection);
            });
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), SpriteEffects.None, Color.White, 0);
            scene.PopSpriteBatch();
        }
    }

    class BulletDelta : Bullet
    {
        protected SpriteReference Sprite;
        protected ColorMatrix ColorMatrix;

        float Angle;
        float AngleVelocity;

        float InitialScale = 1;
        float Scale => (float)LerpHelper.QuadraticIn(InitialScale, 0, MathHelper.Clamp((Frame.Slide - 0.7f) / 0.3f, 0, 1));

        public BulletDelta(SceneGame world, SpriteReference sprite, Vector2 positionStart, ColorMatrix colorMatrix, float angleVelocity, int time) : base(world, positionStart, time)
        {
            Sprite = sprite;
            ColorMatrix = colorMatrix;
            Angle = Random.NextFloat() * MathHelper.TwoPi;
            AngleVelocity = angleVelocity;
        }

        public override void Update()
        {
            base.Update();

            Angle += AngleVelocity;

            if (Frame.Done)
                this.Destroy();

            new Trail(World, Sprite, Position, Vector2.Zero, Angle, Color.Orange, SpriteEffects.None, 10);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(ColorMatrix, matrix, projection);
            });
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), SpriteEffects.None, Color.White, 0);
            scene.PopSpriteBatch();
        }
    }

    class LightningSpark : Particle
    {
        public SpriteReference Sprite;
        public Vector2 PositionStart;
        public Vector2 PositionEnd;
        public int RandomOffset;

        public LightningSpark(SceneGame world, SpriteReference sprite, Vector2 start, Vector2 end, int time) : base(world, Vector2.Zero)
        {
            Sprite = sprite;
            PositionStart = start;
            PositionEnd = end;
            RandomOffset = Random.Next(100);
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
            Vector2 point1 = PositionStart;
            Vector2 point2 = PositionEnd;

            float angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            int length = (int)(point2 - point1).Length();
            scene.PushSpriteBatch(samplerState: SamplerState.PointWrap);
            scene.SpriteBatch.Draw(Sprite.Texture, point1, new Rectangle(RandomOffset, 0, length, Sprite.Height), Color.White, angle, new Vector2(0, Sprite.Height / 2), 1.0f, SpriteEffects.None, 0);
            scene.PopSpriteBatch();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }
    
    class Beam : Projectile
    {
        public int TrailLength;
        public override Vector2 Tween => Vector2.Lerp(PositionStart, PositionEnd, MathHelper.Clamp((float)(Frame.Time) / (Frame.EndTime - TrailLength), 0, 1));
        public SpriteReference Sprite;
        public float Speed;

        public Beam(SceneGame world, SpriteReference sprite, Vector2 positionStart, Vector2 positionEnd, int time, int trail) : base(world, positionStart, positionEnd, time + trail)
        {
            Sprite = sprite;
            TrailLength = trail;
        }

        public override void Impact(Vector2 position)
        {
            Hit = true;
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            float time1 = (Frame.Time - TrailLength) / (Frame.EndTime - TrailLength);
            float time2 = Frame.Time / (Frame.EndTime - TrailLength);
           
            scene.DrawBeamLine(Sprite, PositionStart, PositionEnd, 1, 1, Frame.Time * Speed, time1, time2);
        }
    }

    class Arc : Projectile
    {
        public override Vector2 Tween => CurveFunction(Frame.Slide);
        public SpriteReference Sprite;
        public Vector2 Velocity1;
        public Vector2 Velocity2;
        public Func<Vector2> AnchorStart;
        public Func<Vector2> AnchorEnd;
        public float Speed;
        public Func<float, float> Thickness;

        public Arc(SceneGame world, SpriteReference sprite, Func<Vector2> anchorStart, Func<Vector2> anchorEnd, Vector2 velocityStart, Vector2 velocityEnd, float speed, int time) : base(world, Vector2.Zero, Vector2.Zero, time)
        {
            Sprite = sprite;
            AnchorStart = anchorStart;
            AnchorEnd = anchorEnd;
            Velocity1 = velocityStart;
            Velocity2 = velocityEnd;
            Speed = speed;
            Thickness = (slide) => 1 - Frame.Slide;
        }

        private Vector2 CurveFunction(float slide)
        {
            var start = AnchorStart();
            var end = AnchorEnd();
            var vStart = Vector2.Lerp(start, start + Velocity1, slide);
            var vEnd = Vector2.Lerp(end - Velocity2, end, slide);
            return Vector2.Lerp(vStart, vEnd, slide);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawBeamCurve(Sprite, CurveFunction, 100, Thickness, 1, Frame.Time * Speed, 0, 1);
        }
    }

    class TentacleBeam : Projectile
    {
        public int TrailLength;
        public override Vector2 Tween => CurveFunction(MathHelper.Clamp((float)(Frame.Time) / (Frame.EndTime - TrailLength), 0, 1));
        public SpriteReference Sprite;
        public Vector2 Velocity1;
        public Vector2 Velocity2;
        public Func<Vector2> AnchorStart;
        public Func<Vector2> AnchorEnd;

        public TentacleBeam(SceneGame world, SpriteReference sprite, Func<Vector2> anchorStart, Func<Vector2> anchorEnd, int time, int trail) : base(world, Vector2.Zero, Vector2.Zero, time + trail)
        {
            Sprite = sprite;
            TrailLength = trail;
            AnchorStart = anchorStart;
            AnchorEnd = anchorEnd;
            Velocity1 = new Vector2((Random.NextFloat() - 0.5f) * 2.0f * 100, (Random.NextFloat() - 0.5f) * 2.0f * 100);
            Velocity2 = new Vector2((Random.NextFloat() - 0.5f) * 2.0f * 100, (Random.NextFloat() - 0.5f) * 2.0f * 100);
        }

        private Vector2 CurveFunction(float slide)
        {
            var start = AnchorStart();
            var end = AnchorEnd();
            var vStart = Vector2.Lerp(start, start + Velocity1, slide);
            var vEnd = Vector2.Lerp(end - Velocity2, end, slide);
            return Vector2.Lerp(vStart, vEnd, slide);
        }

        public override void Impact(Vector2 position)
        {
            Hit = true;
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            float time1 = (Frame.Time - TrailLength) / (Frame.EndTime - TrailLength);
            float time2 = Frame.Time / (Frame.EndTime - TrailLength);

            scene.DrawBeamCurve(Sprite, CurveFunction, 100, (slide) => 1, 1, Frame.Time, time1, time2);
        }
    }

    class Missile : Projectile
    {
        public int TrailLength;
        public override Vector2 Tween => CurveFunction(MathHelper.Clamp((float)(Frame.Time) / (Frame.EndTime - TrailLength), 0, 1));
        public SpriteReference Sprite;
        public Vector2 Velocity1;
        public Vector2 Velocity2;
        public Func<Vector2> AnchorStart;
        public Func<Vector2> AnchorEnd;

        public Missile(SceneGame world, SpriteReference sprite, Func<Vector2> anchorStart, Func<Vector2> anchorEnd, Vector2 velocityStart, Vector2 velocityEnd, int time, int trail) : base(world, Vector2.Zero, Vector2.Zero, time + trail)
        {
            Sprite = sprite;
            TrailLength = trail;
            AnchorStart = anchorStart;
            AnchorEnd = anchorEnd;
            Velocity1 = velocityStart;
            Velocity2 = velocityEnd;
        }

        private Vector2 CurveFunction(float slide)
        {
            var start = AnchorStart();
            var end = AnchorEnd();
            float slideStart = (float)LerpHelper.QuinticOut(0, 1, slide);
            float slideEnd = slide;
            float slideMiddle = (float)LerpHelper.CubicIn(0, 1, slide);
            var vStart = Vector2.Lerp(start, start + Velocity1, slideStart);
            var vEnd = Vector2.Lerp(end - Velocity2, end, slideEnd);
            return Vector2.Lerp(vStart, vEnd, slideMiddle);
        }

        public override void Impact(Vector2 position)
        {
            Hit = true;
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            float time1 = (Frame.Time - TrailLength) / (Frame.EndTime - TrailLength);
            float time2 = Frame.Time / (Frame.EndTime - TrailLength);

            scene.DrawMissileCurve(Sprite, CurveFunction, 100, (slide) => 1, time1, time2);
        }
    }

    class Lightning : Beam
    {
        public Lightning(SceneGame world, Vector2 positionStart, Vector2 positionEnd, int time, int trail) : base(world, SpriteLoader.Instance.AddSprite("content/lightning"), positionStart, positionEnd, time, trail)
        {
        }

        public override void Impact(Vector2 position)
        {
            Hit = true;
            new LightningFlash(World, position, Vector2.Zero, 0, 6);
        }
    }

    class HeavenRay : Particle
    {
        public HeavenRay(SceneGame world, Tile tile, int time) : base(world, tile.VisualPosition)
        {
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
            int x = (int)Position.X + 8;
            int y = (int)Position.Y + 16;
            int size = (int)MathHelper.Lerp(16,0,Frame.Slide);
            scene.SpriteBatch.Draw(scene.Pixel, new Rectangle(x - size / 2, y - scene.Viewport.Height, size, scene.Viewport.Height), Color.White);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class Seism : Particle
    {
        protected SpriteReference Sprite;
        protected float Distance;
        protected int RandomOffset;

        Vector2 Offset => new Vector2(0,(float)LerpHelper.QuadraticOut(0, -Distance, Frame.Slide));

        public Seism(SceneGame world, SpriteReference sprite, Vector2 position, float distance, int time) : base(world, position)
        {
            Frame = new Slider(time);
            Sprite = sprite;
            Distance = distance;
            RandomOffset = Random.Next();
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.SpriteBatch.Draw(Sprite.Texture, Position + Offset + new Vector2(Sprite.Middle.X,Sprite.Height), new Rectangle(RandomOffset % 16, 0, Sprite.Width, Sprite.Height), Color.White, 0, Sprite.Middle, Vector2.One, SpriteEffects.None, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class SeismSmall : VisualEffect
    {
        public SeismSmall(SceneGame world, Tile tile, int time) : base(world)
        {
            SpriteReference seism0 = SpriteLoader.Instance.AddSprite("content/seism_0");
            SpriteReference seism1 = SpriteLoader.Instance.AddSprite("content/seism_1");
            SpriteReference seism2 = SpriteLoader.Instance.AddSprite("content/seism_2");
            new Seism(world, seism0, tile.VisualPosition, 8, (time * 1) / 4);
            new Seism(world, seism0, tile.VisualPosition, 12, (time * 2) / 4);
            new Seism(world, seism1, tile.VisualPosition, 14, (time * 3) / 4);
            new Seism(world, seism2, tile.VisualPosition, 16, (time * 4) / 4);
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

    class SeismArea : VisualEffect
    {
        public List<Tile> Tiles = new List<Tile>();

        public SeismArea(SceneGame world, IEnumerable<Tile> tiles, int time) : base(world)
        {
            var lookup = tiles.ToHashSet();
            foreach(Tile tile in tiles)
            {
                Tile bottom = tile.GetNeighbor(0, 1);
                Tile top = tile.GetNeighbor(0, -1);
                if (lookup.Contains(bottom))
                    continue;
                new SeismSmall(world, tile, time);
            }
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

    class Triangle : Particle
    {
        SpriteReference Sprite;
        Color Color;
        float Angle;
        float AngleVelocity;
        protected Vector2 Velocity;
        protected float InitialScale = 1;
        protected float Scale => (float)LerpHelper.QuadraticIn(InitialScale, 0, MathHelper.Clamp((Frame.Slide - 0.9f) / 0.1f, 0, 1));

        public Vector2 Tween => Vector2.Lerp(Position, Position + Velocity, (float)LerpHelper.QuadraticOut(0,1,Frame.Slide));

        public Triangle(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, float angleVelocity, Color color, int time) : base(world, position)
        {
            Sprite = sprite;
            Velocity = velocity;
            Frame = new Slider(time);
            Angle = Random.NextFloat() * MathHelper.TwoPi;
            AngleVelocity = angleVelocity;
            Color = color;
        }

        public override void Update()
        {
            base.Update();

            if (Frame.Done)
                this.Destroy();

            Angle += AngleVelocity;
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, Tween - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), SpriteEffects.None, Color, 0);
        }
    }

    class RockTremor : VisualEffect
    {
        Creature Anchor;

        public RockTremor(SceneGame world, Creature anchor, int time) : base(world)
        {
            Anchor = anchor;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();

            if (Frame.Done)
                this.Destroy();

            int n;
            if (Frame.Time <= 3)
                n = 20;
            else
                n = 2;

            for (int i = 0; i < n; i++)
            {
                Vector2 emitPos = new Vector2(Anchor.X * 16, Anchor.Y * 16) + Anchor.Mask.GetRandomPixel(Random);
                Vector2 centerPos = Anchor.VisualTarget;
                Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f) * 6;
                new Rock(World, SpriteLoader.Instance.AddSprite("content/rock"), emitPos, velocity, (Random.NextFloat() + 0.5f) * 6, new Color(162, 137, 119), 20);
            }
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

    class Rock : Particle
    {
        SpriteReference Sprite;
        int SubImage;
        Color Color;
        protected Vector2 Velocity;
        float Height;
        protected float InitialScale = 1;
        protected float Scale => (float)LerpHelper.QuadraticIn(InitialScale, 0, MathHelper.Clamp((Frame.Slide - 0.9f) / 0.1f, 0, 1));

        public Vector2 Tween => Vector2.Lerp(Position, Position + Velocity, (float)LerpHelper.QuadraticOut(0, 1, Frame.Slide)) + Jump;
        public Vector2 Jump => new Vector2(0, (float)LerpHelper.QuadraticOut(0,-Height,LerpHelper.ForwardReverse(0,1,Frame.Slide)));

        public Rock(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, float height, Color color, int time) : base(world, position)
        {
            Sprite = sprite;
            SubImage = Random.Next(Sprite.SubImageCount);
            Velocity = velocity;
            Height = height;
            Frame = new Slider(time);
            Color = color;
        }

        public override void Update()
        {
            base.Update();

            if (Frame.Done)
                this.Destroy();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, SubImage, Tween - Sprite.Middle, Sprite.Middle, 0, new Vector2(Scale), SpriteEffects.None, Color, 0);
        }
    }

    class Cinder : Particle
    {
        static SimplexPerlin Noise = new SimplexPerlin();

        protected SpriteReference Sprite;
        protected Vector2 Velocity;
        protected float DriftAngle;
        protected float Angle;
        protected float InitialScale;
        protected virtual float Scale => (float)LerpHelper.Linear(InitialScale,0,Frame.Slide);
        protected float RandomOffset;

        public Cinder(SceneGame world, SpriteReference sprite, Vector2 position, Vector2 velocity, int time) : base(world, position)
        {
            Sprite = sprite;
            Velocity = velocity;
            Frame = new Slider(time);
            InitialScale = MathHelper.Lerp(0.2f,1.0f,Random.NextFloat());
            RandomOffset = Random.NextFloat();
            Angle = Random.NextFloat() * MathHelper.TwoPi;
            DriftAngle = Random.NextFloat() * MathHelper.TwoPi;
        }

        public override void Update()
        {
            base.Update();
            Position += Velocity;
            Velocity += Util.AngleToVector(DriftAngle) * 0.1f;
            Velocity = Velocity.ClampLength(0, 2000);
            Angle += 0.01f;
            float driftAngleVelocity = Noise.GetValue(Frame.Slide, RandomOffset);
            DriftAngle += driftAngleVelocity;
            if (Frame.Done)
                this.Destroy();
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, Position - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), SpriteEffects.None, Color.White, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class WhirlWindEffect : VisualEffect
    {
        protected Creature Anchor;

        public WhirlWindEffect(SceneGame world, Creature anchor, int time) : base(world)
        {
            Anchor = anchor;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            float size = 0.5f + 0.5f * Random.NextFloat();
            size *= (float)LerpHelper.CircularOut(1, 0.5, Frame.GetSubSlide(10, Frame.EndTime));

            if (Frame.Time % 2 == 0)
            {
                int totaltime = 5 + Random.Next(10);
                float bigSize = 1.5f + Random.NextFloat();
                Color color = Color.Lerp(Color.White, Color.Gray, Random.NextFloat());
                Func<float, float> speedFunc = (slide) => (float)LerpHelper.Linear(-MathHelper.TwoPi / totaltime, -(MathHelper.TwoPi / totaltime) * 0.5f, slide);
                Func<float, float> sizeFunc = (slide) => (float)LerpHelper.CircularOut(size, size * bigSize, slide);
                new Cutter(World, () => Anchor.VisualTarget, speedFunc, sizeFunc, Random.NextFloat() * MathHelper.TwoPi, color, totaltime + Random.Next(4));
            }
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

    class DisintegrateEffect : VisualEffect
    {
        protected SpriteReference Sprite;
        protected Creature Anchor;

        public DisintegrateEffect(SceneGame world, SpriteReference sprite, Creature anchor, int time) : base(world)
        {
            Sprite = sprite;
            Anchor = anchor;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            int n;
            if (Frame.Time <= 1)
                n = 10;
            else
                n = 3;

            for (int i = 0; i < n; i++)
            {
                Vector2 emitPos = new Vector2(Anchor.X * 16, Anchor.Y * 16) + Anchor.Mask.GetRandomPixel(Random);
                Vector2 centerPos = Anchor.VisualTarget;
                Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f) * 0.2f;
                new Cinder(World, Sprite, emitPos, velocity, Random.Next(30) + 30);
            }
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

    class FlarePower : VisualEffect
    {
        protected SpriteReference Sprite;
        protected Creature Anchor;

        public FlarePower(SceneGame world, SpriteReference sprite, Creature anchor, int time) : base(world)
        {
            Sprite = sprite;
            Anchor = anchor;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            int n;
            if (Frame.Time <= 3)
                n = 50;
            else
                n = 2;

            for (int i = 0; i < n; i++)
            {
                Vector2 emitPos = new Vector2(Anchor.X * 16, Anchor.Y * 16) + Anchor.Mask.GetRandomPixel(Random);
                Vector2 centerPos = Anchor.VisualTarget;
                Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f) * 1;
                new Cinder(World, Sprite, emitPos, velocity, (int)Math.Min(Random.Next(90) + 90, Frame.EndTime - Frame.Time));
            }
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

    class FlareCharge : VisualEffect
    {
        protected SpriteReference Sprite;
        protected Creature Anchor;
        protected Func<Vector2> Target;

        public FlareCharge(SceneGame world, SpriteReference sprite, Creature anchor, Func<Vector2> target, int time) : base(world)
        {
            Sprite = sprite;
            Anchor = anchor;
            Target = target;
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
                this.Destroy();
            int n;
            if (Frame.Time <= 3)
                n = 50;
            else
                n = 2;

            for (int i = 0; i < n; i++)
            {
                Vector2 emitPos = new Vector2(Anchor.X * 16, Anchor.Y * 16) + Anchor.Mask.GetRandomPixel(Random);
                Vector2 centerPos = Anchor.VisualTarget;
                Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f) * 5;
                new CinderFlare(World, Sprite, Target, emitPos, velocity, (int)Math.Min(Random.Next(90) + 90, Frame.EndTime - Frame.Time));
            }
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

    class CinderFlare : Cinder
    {
        Func<Vector2> Anchor;
        Vector2 VisualPosition => Vector2.Lerp(Position, Anchor(), (float)LerpHelper.CircularOut(0, 1, Frame.Slide));
        protected override float Scale => InitialScale;

        public CinderFlare(SceneGame world, SpriteReference sprite, Func<Vector2> anchor, Vector2 position, Vector2 velocity, int time) : base(world, sprite, position, velocity, time)
        {
            Anchor = anchor;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.DrawSpriteExt(Sprite, 0, VisualPosition - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), SpriteEffects.None, Color.White, 0);
        }
    }

    class IronMaiden : VisualEffect
    {
        public Func<Vector2> Anchor;
        public Func<Vector2> Target;
        public Slider NextPiece;
        public Slider Pieces;
        int ExtraTime;

        public IronMaiden(SceneGame world, Func<Vector2> anchor, Func<Vector2> target, int time, int pieces, int extraTime) : base(world)
        {
            Anchor = anchor;
            Target = target;
            Frame = new Slider(time * pieces + extraTime);
            NextPiece = new Slider(time);
            Pieces = new Slider(pieces);
            ExtraTime = extraTime;
        }

        public override void Update()
        {
            base.Update();

            NextPiece += 1;

            if (NextPiece.Done && !Pieces.Done)
            {
                float angle = MathHelper.TwoPi * Pieces.Slide - Frame.Time * 0.1f;
                float time = Frame.EndTime - Frame.Time;
                
                new IronMaidenPart(World, Anchor(), Target(), angle, (int)time);
                Pieces += 1;
            }

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

    class IronMaidenPart : Projectile
    {
        float Angle;
        float TweenAngle;

        private Vector2 Offset => Util.AngleToVector(TweenAngle) * (float)LerpHelper.QuadraticOut(0, 50, LerpHelper.ForwardReverse(0, 1, Frame.Slide));
        public override Vector2 Tween => Offset + Vector2.Lerp(PositionStart, PositionEnd, (float)LerpHelper.Quadratic(0, 1, Frame.Slide));

        public IronMaidenPart(SceneGame world, Vector2 positionStart, Vector2 positionEnd, float angle, int time) : base(world, positionStart, positionEnd, time)
        {
            TweenAngle = angle;
            Angle = angle;
        }

        public override void Update()
        {
            base.Update();
            Angle += 0.1f;
            TweenAngle -= 0.1f;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var triangle = SpriteLoader.Instance.AddSprite("content/triangle");
            scene.DrawSpriteExt(triangle, scene.AnimationFrame(triangle, Frame.Time, Frame.EndTime), Position - triangle.Middle, triangle.Middle, Angle, Vector2.One, SpriteEffects.None, new Color(225, 174, 210), 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Effect;
        }
    }

    class ExperienceDrop : Particle
    {
        Vector2 Velocity;
        Slider Start;
        Slider End;
        Creature Creature;
        int Period;
        float Size;

        public ExperienceDrop(SceneGame world, Creature creature, Vector2 position, Vector2 velocity, int start, int end) : base(world, position)
        {
            Frame = new Slider(start + end);
            Velocity = velocity;
            Start = new Slider(start);
            End = new Slider(end);
            Creature = creature;
            Period = Random.Next(10,30);
            Size = MathHelper.Lerp(0.5f,1.0f,Random.NextFloat());
        }

        public override void Update()
        {
            base.Update();

            if (!Start.Done)
            {
                Start += 1;
            }

            if(!End.Done)
            {
                End += 1;
            }
            else
            {
                this.Destroy();
            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            var experience = SpriteLoader.Instance.AddSprite("content/exp_small");
            
            Vector2 startOffset = Vector2.Lerp(Vector2.Zero, Velocity, (float)LerpHelper.CubicOut(0, 1, Start.Slide));
            Vector2 pos = Vector2.Lerp(Position + startOffset, Creature.VisualTarget, (float)LerpHelper.Quadratic(0, 1, End.Slide));
            float size = (float)Math.Sin(End.Slide * MathHelper.Pi) * Size;

            scene.DrawSpriteExt(experience, scene.AnimationFrame(experience, Frame.Time, Frame.EndTime), pos - experience.Middle, experience.Middle, 0, new Vector2(size), SpriteEffects.None, Color.LightGoldenrodYellow, 0);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.EffectAdditive;
        }
    }

    class DamagePopup : Particle
    {
        protected Func<Vector2> Anchor;

        public Message Message;
        public string Text;
        public TextParameters Parameters;
        float StartAngle;

        public override Vector2 Position
        {
            get
            {
                return Anchor();
            }
            set
            {
                //NOOP
            }
        }
        public Vector2 Offset => new Vector2(0, -32) * (float)LerpHelper.QuadraticOut(0, 1, Frame.Slide);

        public DamagePopup(SceneGame world, Func<Vector2> anchor, Message message, int time) : base(world, Vector2.Zero)
        {
            Anchor = anchor;
            Message = message;
            Text = message.Text;
            Parameters = new TextParameters().SetColor(Color.White, Color.Black).SetBold(true);
            StartAngle = Random.NextFloat() * MathHelper.TwoPi;
            Frame = new Slider(time);
        }

        private Vector2 CharOffset(int index)
        {
            float duration = Frame.EndTime;
            var perChar = (duration / Text.Length) * 0.5f;
            float time = (Frame.Time - index * perChar) / duration;
            float tick = Frame.Time * 2;
            float lerp = MathHelper.Clamp((tick - index * perChar) / duration, 0, 1);
            if (lerp <= 0)
                return new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var angle = StartAngle + time * Math.PI * 4;
            return new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle)) * 10 * (float)Math.Sin(Math.PI / 2 + lerp * Math.PI / 2);
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
            var height = FontUtil.GetStringHeight(Text, Parameters);
            Vector2 pos = Vector2.Transform(Position + Offset, scene.WorldTransform);
            var parameters = Parameters.Copy();
            parameters.SetOffset(CharOffset);
            scene.DrawText(Text, pos - new Vector2(0, height / 2), Alignment.Center, parameters);
        }
    }
}
