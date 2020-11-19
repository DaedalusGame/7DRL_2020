using LibNoise.Primitive;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.VisualEffects
{
    class ParticleDynamic : VisualEffect
    {
        public SpriteReference Sprite;

        protected float? OverrideSlide;
        protected float Slide => OverrideSlide ?? Frame.Slide;

        public virtual int SubImage
        {
            get;
            set;
        }
        public virtual Vector2 Position
        {
            get;
            set;
        }
        public virtual float Scale
        {
            get;
            set;
        } = 1;
        public virtual float Angle
        {
            get;
            set;
        }
        public virtual Color Color
        {
            get;
            set;
        } = Color.White;
        public virtual ColorMatrix? ColorMatrix
        {
            get;
            set;
        }
        public virtual SpriteEffects Mirror
        {
            get;
            set;
        }

        public DrawPass Pass = DrawPass.Effect;

        public Action<ParticleDynamic> OnUpdate;
        public Action<ParticleDynamic> OnDestroy;

        public ParticleDynamic(SceneGame world, int time) : base(world)
        {
            Frame = new Slider(time);
        }

        public Vector2 GetSlidePosition(float slide)
        {
            OverrideSlide = slide;
            Vector2 pos = Position;
            OverrideSlide = null;
            return pos;
        }

        public int AnimateSubImage(float imageSpeed, LerpHelper.Delegate lerp)
        {
            return (int)lerp(0, imageSpeed * Frame.EndTime, Slide);
        }

        public int LerpSubImage(LerpHelper.Delegate lerp)
        {
            return (int)MathHelper.Clamp(Sprite.SubImageCount * (float)lerp(0, 1, Slide), 0, Sprite.SubImageCount - 1);
        }

        public Vector2 AnimatePosition(Vector2 velocity, LerpHelper.Delegate lerp)
        {
            return Vector2.Lerp(Vector2.Zero, velocity * Frame.EndTime, (float)lerp(0, 1, Slide));
        }

        public Vector2 AnimateBetween(Vector2 offset, LerpHelper.Delegate lerp)
        {
            return Vector2.Lerp(Vector2.Zero, offset, (float)lerp(0, 1, Slide));
        }

        public Vector2 AnimateJump(Vector2 offset, float height, LerpHelper.Delegate lerp)
        {
            var jumpOffset = Vector2.Lerp(new Vector2(0, 0), new Vector2(0, -height), (float)Math.Sin(Slide * MathHelper.Pi));
            return Vector2.Lerp(Vector2.Zero, offset, (float)lerp(0, 1, Slide)) + jumpOffset;
        }

        public Vector2 AnimateMissile(Vector2 offset, Vector2 velocityStart, Vector2 velocityEnd, LerpHelper.Delegate lerpStart, LerpHelper.Delegate lerpMid, LerpHelper.Delegate lerpEnd)
        {
                var start = Vector2.Lerp(Vector2.Zero, velocityStart * Frame.EndTime, (float)lerpStart(0, 1, Slide));
                var end = Vector2.Lerp(offset - velocityEnd * Frame.EndTime, offset, (float)lerpEnd(0, 1, Slide));
                return Vector2.Lerp(start, end, (float)lerpMid(0, 1, Slide));
        }

        public float AngleSpin(float speed, LerpHelper.Delegate lerp)
        {
            return (float)lerp(0, speed * Frame.EndTime, Slide);
        }

        public float AngleTowards(Vector2 position)
        {
            return Util.VectorToAngle(position - Position);
        }

        public float AngleVelocity(float angle)
        {
            return Util.VectorToAngle(GetSlidePosition(Frame.Slide + 0.001f) - GetSlidePosition(Frame.Slide - 0.001f));
        }

        public float ScaleTowards(float start, float end, LerpHelper.Delegate lerp)
        {
            return (float)lerp(start, end, Slide);
        }

        public Color ColorAlpha(Color color, float alpha, LerpHelper.Delegate lerp)
        {
            return new Color(color.R, color.G, color.B, (int)lerp(color.A, alpha * 255f, Slide));
        }
        
        public Color ColorLerp(Color a, Color b, LerpHelper.Delegate lerp)
        {
            return Color.Lerp(a, b, (float)lerp(0, 1, Slide));
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {
                OnDestroy?.Invoke(this);
                this.Destroy();
            }
            OnUpdate?.Invoke(this);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            if (Sprite == null)
                return;

            if (ColorMatrix.HasValue)
            {
                scene.PushSpriteBatch(shaderSetup: (matrix, projection) =>
                {
                    scene.SetupColorMatrix(ColorMatrix.Value, matrix, projection);
                });
            }
            scene.DrawSpriteExt(Sprite, SubImage, Position - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), Mirror, Color, 0);
            if (ColorMatrix.HasValue)
                scene.PopSpriteBatch();
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return Pass;
        }
    }

    class ParticleAfterImage : ParticleDynamic
    {
        protected CreatureRender Render;
        public virtual Facing Facing
        {
            get;
            set;
        }
        public virtual PoseData PoseData
        {
            get;
            set;
        }
        public virtual Vector2 Origin
        {
            get;
            set;
        }

        public float ScaleEnd;
        public LerpHelper.Delegate ScaleLerp;
        public LerpHelper.Delegate ColorLerp;

        public override float Scale => ScaleTowards(base.Scale, ScaleEnd, ScaleLerp);
        public override Color Color => ColorAlpha(base.Color, 0, ColorLerp);

        public ParticleAfterImage(SceneGame world, LerpHelper.Delegate scaleLerp, LerpHelper.Delegate colorLerp, int time) : base(world, time)
        {
            ScaleLerp = scaleLerp;
            ColorLerp = colorLerp;
        }

        public void SetPose(CreatureRender render, Facing facing, PoseData poseData, Vector2 origin)
        {
            Render = render;
            Facing = facing;
            PoseData = poseData;
            Origin = origin;
        }

        public void SetPose(Creature creature, Vector2 origin)
        {
            SetPose(creature.Render, creature.VisualFacing(), creature.Render.GetPoseData(creature), origin);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            Matrix transform = Matrix.CreateRotationZ(Angle) * Matrix.CreateScale(new Vector3(Scale, Scale, 1));
            transform = Matrix.CreateTranslation(new Vector3(-Position - Origin, 0)) * transform * Matrix.CreateTranslation(new Vector3(Position + Origin, 0));
            Render.DrawFrame(scene, Position, PoseData, Facing, transform, Color, ColorMatrix ?? RoguelikeEngine.ColorMatrix.Identity);
        }
    }

    class ParticleAfterImageLocked : ParticleAfterImage
    {
        Creature Creature;

        public override Facing Facing => Creature.VisualFacing();
        public override PoseData PoseData => Render.GetPoseData(Creature);

        public ParticleAfterImageLocked(Creature creature, LerpHelper.Delegate scaleLerp, LerpHelper.Delegate colorLerp, int time) : base(creature.World, scaleLerp, colorLerp, time)
        {
            Creature = creature;
            Render = creature.Render;
        }
    }

    class ParticleSpark : ParticleDynamic
    {
        List<Vector2> TrailPositions = new List<Vector2>();
        public Vector2 Target;
        public Color ColorEnd;
        public LerpHelper.Delegate VelocityLerp;
        public LerpHelper.Delegate ColorLerp;

        public override Vector2 Position => base.Position + AnimateBetween(Target - base.Position, VelocityLerp);

        public ParticleSpark(SceneGame world, int time) : base(world, time)
        {
        }

        public override void Update()
        {
            TrailPositions.Add(Position);
            if (TrailPositions.Count > 5)
                TrailPositions.RemoveAt(0);
            base.Update();
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            if (Sprite == null)
                return;

            if (ColorMatrix.HasValue)
            {
                scene.PushSpriteBatch(shaderSetup: (matrix, projection) =>
                {
                    scene.SetupColorMatrix(ColorMatrix.Value, matrix, projection);
                });
            }

            Vector2 nextPosition = Position;

            int i = 0;
            foreach(var trailPos in TrailPositions)
            {
                Vector2 currentPosition = Vector2.Lerp(trailPos, nextPosition, 0.5f);
                float lerpSlide = Util.ReverseLerp(Slide, 0.5f, 1.0f);
                Color color = Color.Lerp(Color, ColorEnd, (float)i / (TrailPositions.Count - 1));
                color = Color.Lerp(color, Color.TransparentBlack, (float)ColorLerp(0, 1, lerpSlide));
                scene.DrawSpriteExt(Sprite, SubImage, currentPosition - Sprite.Middle, Sprite.Middle, Angle, new Vector2(Scale), Mirror, color, 0);
                nextPosition = currentPosition;
                i++;
            }
            
            if (ColorMatrix.HasValue)
                scene.PopSpriteBatch();
        }
    }

    class ParticleExplosion : ParticleDynamic
    {
        LerpHelper.Delegate SubImageLerp;
        Vector2 Velocity;
        LerpHelper.Delegate VelocityLerp;

        public override int SubImage => LerpSubImage(SubImageLerp);
        public override Vector2 Position => base.Position + AnimatePosition(Velocity, VelocityLerp);

        public ParticleExplosion(SceneGame world, SpriteReference sprite, Vector2 velocity, LerpHelper.Delegate subImageLerp, LerpHelper.Delegate velocityLerp, int time) : base(world, time)
        {
            Sprite = sprite;
            SubImageLerp = subImageLerp;
            Velocity = velocity;
            VelocityLerp = velocityLerp;
        }
    }

    class ParticleCutter : ParticleDynamic
    {
        public float RotationStart;
        public float RotationEnd;
        public float ScaleEnd;
        public float FadeSlide;
        public LerpHelper.Delegate SubImageLerp = LerpHelper.Linear;
        public LerpHelper.Delegate VelocityLerp = LerpHelper.Linear;
        public LerpHelper.Delegate AngleLerp = LerpHelper.Linear;
        public LerpHelper.Delegate ScaleLerp = LerpHelper.Linear;

        public override int SubImage => Slide > FadeSlide ? (int)MathHelper.Clamp(Sprite.SubImageCount * (float)SubImageLerp(0, 1, Util.ReverseLerp(Slide, FadeSlide, 1)), 0, Sprite.SubImageCount - 1) : 0;
        public override float Scale => ScaleTowards(base.Scale, ScaleEnd, ScaleLerp);
        float Rotation => (float)AngleLerp(RotationStart, RotationEnd, Slide);

        public ParticleCutter(SceneGame world, int time) : base(world, time)
        {
               
        }

        public override void Update()
        {
            base.Update();
            Angle += Rotation;
        }
    }

    class ParticleBlob : ParticleDynamic
    {
        Vector2 Velocity;
        LerpHelper.Delegate VelocityLerp;
        LerpHelper.Delegate ScaleLerp;

        public override Vector2 Position => base.Position + AnimatePosition(Velocity, VelocityLerp);
        public override float Scale => (float)ScaleLerp(base.Scale, 0, Slide);

        public ParticleBlob(SceneGame world, SpriteReference sprite, Vector2 velocity, LerpHelper.Delegate velocityLerp, LerpHelper.Delegate scaleLerp, int time) : base(world, time)
        {
            Sprite = sprite;
            Velocity = velocity;
            VelocityLerp = velocityLerp;
            ScaleLerp = scaleLerp;
        }
    }

    class ParticleSpore : ParticleDynamic
    {
        Vector2 Target;
        LerpHelper.Delegate VelocityLerp;
        int OffsetX, OffsetY;

        public override Vector2 Position => base.Position + this.AnimateBetween(Target - base.Position, VelocityLerp);

        public ParticleSpore(SceneGame world, SpriteReference sprite, Vector2 start, Vector2 end, LerpHelper.Delegate velocityLerp, int time) : base(world, time)
        {
            Sprite = sprite;
            Position = start;
            Target = end;
            VelocityLerp = velocityLerp;
            OffsetX = Random.Next(sprite.Width);
            OffsetY = Random.Next(sprite.Height);
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.SpriteBatch.Draw(Sprite.Texture, Position - Sprite.Middle * Scale, new Rectangle(OffsetX, OffsetY, (int)(Sprite.Width * Scale), (int)(Sprite.Height * Scale)), Color);
        }
    }

    class ParticleThrow : ParticleDynamic
    {
        public float ImageSpeed;
        Vector2 Target;
        float Height;
        public LerpHelper.Delegate SubImageLerp = LerpHelper.Linear;
        LerpHelper.Delegate VelocityLerp;
        LerpHelper.Delegate ScaleLerp;
        float FadeSlide;

        public override int SubImage => base.SubImage + AnimateSubImage(ImageSpeed, SubImageLerp);
        public override Vector2 Position => base.Position + AnimateJump(Target - base.Position, Height, VelocityLerp);
        public override float Scale => Slide > FadeSlide ? (float)ScaleLerp(base.Scale, 0, (Slide - FadeSlide) / (1 - FadeSlide)) : base.Scale;

        public ParticleThrow(SceneGame world, SpriteReference sprite, Vector2 start, Vector2 end, float height, LerpHelper.Delegate velocityLerp, LerpHelper.Delegate scaleLerp, int time, float fadeSlide) : base(world, time)
        {
            Sprite = sprite;
            Position = start;
            Target = end;
            Height = height;
            VelocityLerp = velocityLerp;
            ScaleLerp = scaleLerp;
            FadeSlide = fadeSlide;
        }
    }

    class ParticleCinder : ParticleDynamic
    {
        static SimplexPerlin Noise = new SimplexPerlin();

        Vector2 Offset;
        Vector2 Velocity;
        float DriftAngle;
        float DriftSpeed;
        float Rotation;
        float VelocityMax;
        float RandomOffset;
        LerpHelper.Delegate ScaleLerp;
        LerpHelper.Delegate AngleLerp;

        public override Vector2 Position => base.Position + Offset;
        public override float Scale => (float)ScaleLerp(base.Scale, 0, Slide);
        public override float Angle => base.Angle + AngleSpin(Rotation, AngleLerp);

        public ParticleCinder(SceneGame world, SpriteReference sprite, Vector2 pos, Vector2 velocity, float driftSpeed, float velocityMax, float rotation, float scale, LerpHelper.Delegate scaleLerp, LerpHelper.Delegate angleLerp, int time) : base(world, time)
        {
            Sprite = sprite;
            Position = pos;
            Velocity = velocity;
            VelocityMax = velocityMax;
            Rotation = rotation;
            Angle = Random.NextFloat() * MathHelper.TwoPi;
            Scale = scale;
            DriftSpeed = driftSpeed;
            DriftAngle = Random.NextFloat() * MathHelper.TwoPi;
            ScaleLerp = scaleLerp;
            AngleLerp = angleLerp;
            RandomOffset = Random.NextFloat();
        }

        public override void Update()
        {
            base.Update();
            Offset += Velocity;
            Velocity += Util.AngleToVector(DriftAngle) * DriftSpeed;
            Velocity = Velocity.ClampLength(0, VelocityMax);
            float driftAngleVelocity = Noise.GetValue(Frame.Slide, RandomOffset);
            DriftAngle += driftAngleVelocity;
        }
    }

    class ParticleCinderAbsorb : ParticleCinder
    {
        Vector2 Target;
        LerpHelper.Delegate VelocityLerp;

        public override Vector2 Position => base.Position + AnimateBetween(Target - base.Position, VelocityLerp);

        public ParticleCinderAbsorb(SceneGame world, SpriteReference sprite, Vector2 pos, Vector2 velocity, Vector2 target, float driftSpeed, float velocityMax, float rotation, float scale, LerpHelper.Delegate velocityLerp, LerpHelper.Delegate scaleLerp, LerpHelper.Delegate angleLerp, int time) : base(world, sprite, pos, velocity, driftSpeed, velocityMax, rotation, scale, scaleLerp, angleLerp, time)
        {
            Target = target;
            VelocityLerp = velocityLerp;
        }
    }

    class ParticleCircle : VisualEffect
    {
        static SamplerState SamplerState = new SamplerState()
        {
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Clamp,
            Filter = TextureFilter.Point,
        };

        public SpriteReference Sprite;

        public virtual Vector2 Position
        {
            get;
            set;
        }
        public virtual float AngleStart
        {
            get;
            set;
        }
        public virtual float AngleEnd
        {
            get;
            set;
        } = MathHelper.TwoPi;
        public virtual float Radius
        {
            get;
            set;
        }
        public virtual float TexOffset
        {
            get;
            set;
        }
        public virtual float Start
        {
            get;
            set;
        }
        public virtual float End
        {
            get;
            set;
        } = 1;
        public virtual Color Color
        {
            get;
            set;
        } = Color.White;
        public virtual ColorMatrix? ColorMatrix
        {
            get;
            set;
        }

        public int Precision = 100;
        public int TexPrecision;

        public DrawPass Pass = DrawPass.Effect;

        public ParticleCircle(SceneGame world, int time) : base(world)
        {
            Frame = new Slider(time);
        }

        public override void Update()
        {
            base.Update();
            if (Frame.Done)
            {
                this.Destroy();
            }
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            scene.SetupColorMatrix(ColorMatrix.HasValue ? ColorMatrix.Value : RoguelikeEngine.ColorMatrix.Identity, scene.WorldTransform, scene.Projection);
            scene.DrawCircle(Sprite, SamplerState, Position, Precision, AngleStart, AngleEnd, Radius, TexOffset, TexPrecision, Start, End);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return Pass;
        }
    }

    class ParticleRing : ParticleCircle
    {
        public float Thickness;
        public float StartRadius;
        public LerpHelper.Delegate InnerLerp;
        public LerpHelper.Delegate OuterLerp;

        public override float Start => (float)InnerLerp(StartRadius - Thickness, 1, Frame.Slide);
        public override float End => (float)OuterLerp(StartRadius, 1, Frame.Slide);

        public ParticleRing(SceneGame world, float startRadius, float thickness, LerpHelper.Delegate innerLerp, LerpHelper.Delegate outerLerp, int time) : base(world, time)
        {
            Thickness = thickness;
            StartRadius = startRadius;
            InnerLerp = innerLerp;
            OuterLerp = outerLerp;
        }
    }

    class ParticleWave : ParticleCircle
    {
        public float Thickness;
        public float StartRadius;
        public Color EndColor;
        public LerpHelper.Delegate Lerp;
        public LerpHelper.Delegate ColorLerp;

        public override float Start => (float)Lerp(StartRadius, 1 - Thickness, Frame.Slide);
        public override float End => (float)Lerp(StartRadius, 1 - Thickness, Frame.Slide) + Thickness;
        public override Color Color => Color.Lerp(base.Color, EndColor, (float)ColorLerp(0, 1, Frame.Slide));

        public ParticleWave(SceneGame world, float startRadius, float thickness, LerpHelper.Delegate lerp, LerpHelper.Delegate colorLerp, int time) : base(world, time)
        {
            Thickness = thickness;
            StartRadius = startRadius;
            Lerp = lerp;
            ColorLerp = colorLerp;
        }
    }

    class ParticleNuke : ParticleCircle
    {
        public Func<float, float> RadiusFunction;

        float Thickness => Sprite.Height / Radius;
        public override float Start => RadiusFunction(Frame.Slide) - Thickness;
        public override float End => RadiusFunction(Frame.Slide);

        public ParticleNuke(SceneGame world, Func<float, float> radiusFunc, int time) : base(world, time)
        {
            RadiusFunction = radiusFunc;
        }
    }
}
