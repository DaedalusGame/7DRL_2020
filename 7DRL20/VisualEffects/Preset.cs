using Microsoft.Xna.Framework;
using System;

namespace RoguelikeEngine.VisualEffects
{
    abstract class VisualPreset
    {
        protected Random Random = new Random();
        protected SceneGame World;

        public float Angle;
        public Color Color;
        public DrawPass DrawPass;
        public int Time;

        public VisualPreset(SceneGame world)
        {
            World = world;
        }

        public abstract class AtPosition : VisualPreset
        {
            public Vector2 Velocity;
            
            public AtPosition(SceneGame world) : base(world)
            {
            }

            public abstract void Activate(Vector2 position);
        }

        public abstract class AtAnchor : VisualPreset
        {
            public AtAnchor(SceneGame world) : base(world)
            {
            }

            public abstract void Activate(Func<Vector2> position);
        }

        public abstract class BetweenPositions : VisualPreset
        {
            public BetweenPositions(SceneGame world) : base(world)
            {
            }

            public abstract void Activate(Vector2 start, Vector2 end);
        }

        public abstract class BetweenAnchors : VisualPreset
        {
            public BetweenAnchors(SceneGame world) : base(world)
            {
            }

            public abstract void Activate(Func<Vector2> start, Func<Vector2> end);
        }

        public abstract class AtCreature : VisualPreset
        {
            public AtCreature(SceneGame world) : base(world)
            {
            }

            public abstract void Activate(Creature creature);
        }
    }

    class Explosion : VisualPreset.AtPosition
    {
        SpriteReference Sprite;
        LerpHelper.Delegate SubImageLerp;
        LerpHelper.Delegate VelocityLerp;

        public Explosion(SceneGame world) : base(world)
        {
        }

        public override void Activate(Vector2 position)
        {
            var explosion = new ParticleExplosion(World, Sprite, Velocity, SubImageLerp, VelocityLerp, Time)
            {
                Position = position,
                Angle = Angle,
            };
        }
    }

    class RingExplosion : VisualPreset.AtPosition
    {
        int Count;
        float Distance;
        AtPosition Explosion;
        int TimeMin, TimeMax;

        public RingExplosion(SceneGame world) : base(world)
        {
        }

        public override void Activate(Vector2 position)
        {
            for (int i = 0; i < Count; i++)
            {
                float angle = i * MathHelper.TwoPi / Count;
                Vector2 offset = Util.AngleToVector(angle) * Distance;

                Explosion.Angle = angle;
                Explosion.Time = Random.Next(TimeMin, TimeMax);
                Explosion.Activate(position + offset);
            }
        }
    }

    class Tremor : VisualPreset.AtAnchor
    {
        int Count;
        float DistanceMin, DistanceMax;
        BetweenPositions Rock;
        Vector2 Center;

        public Tremor(SceneGame world) : base(world)
        {
        }

        public override void Activate(Func<Vector2> position)
        {
            for (int i = 0; i < Count; i++)
            {
                Vector2 emitPos = position();
                Vector2 centerPos = Center;
                Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (DistanceMin + Random.NextFloat() * (DistanceMax - DistanceMin));
                Rock.Time = 20;
                Rock.Activate(emitPos, emitPos + velocity);
            }
        }
    }

    class RockBetween : VisualPreset.BetweenPositions
    {
        SpriteReference Sprite;
        float HeightMin, HeightMax;
        LerpHelper.Delegate VelocityLerp;
        LerpHelper.Delegate ScaleLerp;
        float FadeSlide;

        public RockBetween(SceneGame world) : base(world)
        {
        }

        public override void Activate(Vector2 start, Vector2 end)
        {
            float height = HeightMin + Random.NextFloat() * (HeightMax - HeightMin);
            new ParticleThrow(World, Sprite, start, end, height, VelocityLerp, ScaleLerp, Time, FadeSlide);
        }
    }

    class RockRandom : VisualPreset.AtPosition
    {
        SpriteReference Sprite;
        float HeightMin, HeightMax;
        float DistanceMin, DistanceMax;
        LerpHelper.Delegate VelocityLerp;
        LerpHelper.Delegate ScaleLerp;
        float FadeSlide;

        public RockRandom(SceneGame world) : base(world)
        {
        }

        public override void Activate(Vector2 pos)
        {
            Vector2 velocity = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * (DistanceMin + Random.NextFloat() * (DistanceMax - DistanceMin));
            float height = HeightMin + Random.NextFloat() * (HeightMax - HeightMin);
            new ParticleThrow(World, Sprite, pos, pos + velocity, height, VelocityLerp, ScaleLerp, Time, FadeSlide);
        }
    }

    class CinderRandom : VisualPreset.AtPosition
    {
        SpriteReference Sprite;
        float SpeedMin, SpeedMax;
        float ScaleMin, ScaleMax;
        float RotationMin, RotationMax;
        float DriftSpeed;
        float VelocityMax;
        LerpHelper.Delegate ScaleLerp;
        LerpHelper.Delegate AngleLerp;

        public CinderRandom(SceneGame world) : base(world)
        {
        }

        public override void Activate(Vector2 position)
        {
            Vector2 velocity = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * (SpeedMin + Random.NextFloat() * (SpeedMax - SpeedMin));
            float scale = ScaleMin + Random.NextFloat() * (ScaleMax - ScaleMin);
            float rotation = RotationMin + Random.NextFloat() * (RotationMax - RotationMin);

            new ParticleCinder(World, Sprite, position, velocity, DriftSpeed, VelocityMax, rotation, scale, ScaleLerp, AngleLerp, Time);
        }
    }
}
