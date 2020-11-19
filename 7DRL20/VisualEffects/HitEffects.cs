using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace RoguelikeEngine.VisualEffects
{
    class HitDamageSpark : VisualPreset.AtCreature
    {
        public HitDamageSpark(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var damageSpark = SpriteLoader.Instance.AddSprite("content/damage_spark");

            creature.FlashHelper.AddFlash(ColorMatrix.Flat(Color.White), 5);

            if(creature == World.Player)
            {
                new ScreenShakeRandom(World, 2, 5, LerpHelper.QuadraticIn);
                new ScreenFlashSimple(World, ColorMatrix.Flat(Color.IndianRed, 0.25f), LerpHelper.Linear, 5);
            }

            for(int i = 0; i < 4; i++)
            {
                new ParticleSpark(World, 20)
                {
                    Sprite = damageSpark,
                    Position = creature.VisualTarget,
                    Target = creature.VisualTarget + Util.AngleToVector(MathHelper.PiOver4 + i * MathHelper.PiOver2) * 240,
                    VelocityLerp = LerpHelper.QuadraticOut,
                    Angle = i * MathHelper.PiOver2,
                    Color = new Color(192, 64, 192),
                    ColorEnd = new Color(64, 128, 255),
                    ColorLerp = LerpHelper.QuadraticOut,
                    Pass = DrawPass.EffectAdditive,
                };
            }
        }
    }

    class HitBludgeon : VisualPreset.AtCreature
    {
        public HitBludgeon(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            creature.FlashHelper.AddFlash(ColorMatrix.Flat(Color.White), 5);
        }
    }

    class HitSlash : VisualPreset.AtCreature
    {
        int Parts = 0;

        public HitSlash(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var slash = SpriteLoader.Instance.AddSprite("content/slash_big");

            var angle = Random.NextAngle();
            for (int i = -Parts; i <= Parts; i++)
            {
                var offset = Util.AngleToVector(angle + MathHelper.PiOver4) * 6 * i;

                var explosion = new ParticleExplosion(World, slash, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
                {
                    Position = creature.VisualTarget + offset,
                    Angle = angle,
                };
            }
        }
    }

    class HitPierce : VisualPreset.AtCreature
    {
        public HitPierce(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var pierce = SpriteLoader.Instance.AddSprite("content/hit_pierce");

            var explosion = new ParticleExplosion(World, pierce, Vector2.Zero, LerpHelper.QuadraticIn, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
                Angle = Random.NextAngle(),
            };
        }
    }

    class HitFire : VisualPreset.AtCreature
    {
        public HitFire(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var fire_big = SpriteLoader.Instance.AddSprite("content/fire_big");
            var fire_small = SpriteLoader.Instance.AddSprite("content/fire_small");

            var explosion = new ParticleExplosion(World, fire_big, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 20)
            {
                Position = creature.VisualTarget,
            };
            explosion.OnUpdate += (particle) =>
            {
                if (particle.Frame.Time > 5 && particle.Frame.Time % 3 == 0)
                {
                    var flame = new ParticleExplosion(World, fire_small, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 6)
                    {
                        Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    };
                }
            };
            creature.FlashHelper.AddFlash(ColorMatrix.TwoColorLight(Color.OrangeRed, Color.Orange), LerpHelper.QuadraticOut, 40);
        }
    }

    class HitIce : VisualPreset.AtCreature
    {
        public HitIce(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var particle = SpriteLoader.Instance.AddSprite("content/rock");

            for (int i = 0; i < 12; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var iceParticle = new ParticleThrow(World, particle, creature.VisualTarget + Util.AngleToVector(angle) * 2, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    SubImage = Random.Next(particle.SubImageCount),
                    Angle = Random.NextAngle(),
                    ColorMatrix = ColorMatrix.TwoColorLight(Color.Blue, Color.White),
                    Pass = DrawPass.EffectAdditive,
                };
            }
            creature.FlashHelper.AddFlash(ColorMatrix.Flat(Color.White), LerpHelper.QuadraticOut, 5);
            creature.FlashHelper.AddFlash(ColorMatrix.TwoColorLight(Color.SkyBlue, Color.White), 10);
        }
    }

    class HitThunder : VisualPreset.AtCreature
    {
        public HitThunder(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var thunder = SpriteLoader.Instance.AddSprite("content/hit_thunder");
            var spark = SpriteLoader.Instance.AddSprite("content/lightning_spark");

            var explosion = new ParticleExplosion(World, thunder, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
                Pass = DrawPass.EffectAdditive,
            };
            explosion.OnUpdate += (particle) =>
            {
                var volt = new ParticleDynamic(World, Random.Next(5))
                {
                    Sprite = spark,
                    SubImage = Random.Next(spark.SubImageCount),
                    Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    Angle = Random.NextAngle(),
                    Pass = DrawPass.EffectAdditive,
                };
            };
        }
    }

    class HitWater : VisualPreset.AtCreature
    {
        public HitWater(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var blob_water = SpriteLoader.Instance.AddSprite("content/pop_blob_water");
            var particle = SpriteLoader.Instance.AddSprite("content/bullet_water");

            var explosion = new ParticleExplosion(World, blob_water, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
                Pass = DrawPass.EffectAdditive,
            };

            for(int i = 0; i < 6; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var waterParticle = new ParticleThrow(World, particle, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    Scale = Random.NextFloat() * 0.75f + 0.25f,
                    Pass = DrawPass.EffectAdditive,
                };
            }
        }
    }

    class HitWind : VisualPreset.AtCreature
    {
        public HitWind(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var cloak = new Cloak(creature, 10);
            cloak.OnUpdate += (c) =>
            {
                float size = 0.15f + 0.15f * Random.NextFloat();
                size *= (float)LerpHelper.CircularOut(1, 0.5, c.Frame.GetSubSlide(10, c.Frame.EndTime));

                if (c.Frame.Time % 2 == 0)
                {
                    var pos = c.Creature.VisualTarget;
                    int totaltime = 5 + Random.Next(10);
                    float bigSize = 1.5f + Random.NextFloat();
                    Color color = Color.Lerp(Color.White, Color.Gray, Random.NextFloat());

                    new ParticleCutter(World, totaltime + Random.Next(4))
                    {
                        Sprite = SpriteLoader.Instance.AddSprite("content/cutter"),
                        FadeSlide = 0.75f,
                        Position = pos,
                        Angle = Random.NextAngle(),
                        RotationStart = -MathHelper.TwoPi / totaltime,
                        RotationEnd = -(MathHelper.TwoPi / totaltime) * 0.5f,
                        AngleLerp = LerpHelper.Linear,
                        Scale = size,
                        ScaleEnd = size * bigSize,
                        ScaleLerp = LerpHelper.CircularOut,
                        Color = color,
                        Pass = DrawPass.EffectAdditive,
                    };
                }
            };
        }
    }

    class HitEarth : VisualPreset.AtCreature
    {
        public HitEarth(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var particle = SpriteLoader.Instance.AddSprite("content/rock");

            for (int i = 0; i < 16; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var earthParticle = new ParticleThrow(World, particle, creature.VisualTarget + Util.AngleToVector(angle) * 2, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    SubImage = Random.Next(particle.SubImageCount),
                    Angle = Random.NextAngle(),
                    Color = new Color(162, 137, 119),
                };
            }
        }
    }

    class HitHoly : VisualPreset.AtCreature
    {
        public HitHoly(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var sparkle = SpriteLoader.Instance.AddSprite("content/sparkle_item");
            var particle_sparkle = SpriteLoader.Instance.AddSprite("content/sparkle");

            var explosion = new ParticleExplosion(World, sparkle, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 20)
            {
                Position = creature.VisualTarget,
                Pass = DrawPass.EffectAdditive,
            };
            int sparkles = 4;
            float fan = MathHelper.Pi * 0.4f;
            for (int i = 0; i < sparkles; i++)
            {
                float angle = -fan + i * fan * 2f / (sparkles - 1);
                var holyParticle = new ParticleThrow(World, particle_sparkle, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * 16, 4, LerpHelper.QuadraticOut, LerpHelper.QuadraticOut, 15, 0.7f)
                {
                    SubImage = Random.Next(particle_sparkle.SubImageCount),
                    ImageSpeed = 0.5f,
                    Pass = DrawPass.EffectAdditive,
                };
            }
        }
    }

    class HitDark : VisualPreset.AtCreature
    {
        public HitDark(SceneGame world) : base(world)
        {
        }

        private float DarkRadius(float a, float b, float slide, float split)
        {
            float startSlide = Util.ReverseLerp(slide, 0f, split);
            float endSlide = Util.ReverseLerp(slide, split, 1f);

            return slide < 0.2f ? (float)LerpHelper.QuadraticOut(a, b, startSlide) : (float)LerpHelper.QuadraticIn(b, a, endSlide);
        }

        public override void Activate(Creature creature)
        {
            var circle_dark = SpriteLoader.Instance.AddSprite("content/circle_dark");
            var particle = SpriteLoader.Instance.AddSprite("content/cloud_mist");

            var dark = new ParticleNuke(World, (slide) => DarkRadius(0, 1, slide, 0.2f), 20)
            {
                Sprite = circle_dark,
                Position = creature.VisualTarget,
                Radius = 12,
                TexPrecision = 20,
            };

            for(int i = 0; i < 12; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 20 + Random.NextFloat() * 10;
                int time = Random.Next(10) + 10;
                var darkParticle = new ParticleSpore(World, particle, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * distance, (a, b, slide) => DarkRadius((float)a, (float)b, (float)slide, 0.2f), time)
                {
                    Scale = Random.NextFloat() * 0.25f + 0.25f,
                    Color = new Color(189, 34, 255),
                };
            }
        }
    }

    class HitAcid : VisualPreset.AtCreature
    {
        public HitAcid(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var blob_acid = SpriteLoader.Instance.AddSprite("content/pop_acid");
            var particle = SpriteLoader.Instance.AddSprite("content/bullet_acid");

            var explosion = new ParticleExplosion(World, blob_acid, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
            };

            for (int i = 0; i < 6; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var acidParticle = new ParticleThrow(World, particle, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    Scale = Random.NextFloat() * 0.75f + 0.25f,
                };
            }
        }
    }

    class HitBlood : VisualPreset.AtCreature
    {
        public HitBlood(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var blood_hit = SpriteLoader.Instance.AddSprite("content/hit_blood");
            var particle = SpriteLoader.Instance.AddSprite("content/blood");

            var explosion = new ParticleExplosion(World, blood_hit, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
            };

            for (int i = 0; i < 6; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var acidParticle = new ParticleThrow(World, particle, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    Angle = Random.NextAngle(),
                    Scale = Random.NextFloat() * 0.75f + 0.25f,
                };
            }
        }
    }

    class HitArcane : VisualPreset.AtCreature
    {
        public HitArcane(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var arcane_explosion = SpriteLoader.Instance.AddSprite("content/pop_arcane");
            var flare = SpriteLoader.Instance.AddSprite("content/flare_arcane");

            var explosion = new ParticleExplosion(World, arcane_explosion, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
                Pass = DrawPass.EffectAdditive,
            };
            explosion.OnUpdate += (particle) =>
            {
                var flare_explosion = new ParticleExplosion(World, flare, Vector2.Zero, LerpHelper.QuadraticOut, LerpHelper.Linear, 5)
                {
                    Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    Scale = Random.NextFloat() * 0.5f + 0.5f,
                    Pass = DrawPass.EffectAdditive,
                };
            };
        }
    }

    class HitPoison : VisualPreset.AtCreature
    {
        public HitPoison(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var poison_explosion = SpriteLoader.Instance.AddSprite("content/pop_blob_poison");
            var smoke = SpriteLoader.Instance.AddSprite("content/smoke_small");

            var explosion = new ParticleExplosion(World, poison_explosion, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 20)
            {
                Position = creature.VisualTarget,
            };
            explosion.OnUpdate += (particle) =>
            {
                var flare_explosion = new ParticleBlob(World, smoke, Vector2.Zero, LerpHelper.Linear, LerpHelper.CubicIn, 10)
                {
                    Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    SubImage = Random.Next(smoke.SubImageCount),
                    Color = new Color(204, 100, 238),
                };
            };
        }
    }

    class HitHellfire : VisualPreset.AtCreature
    {
        public HitHellfire(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var fire_big = SpriteLoader.Instance.AddSprite("content/blaze_big");
            var fire_small = SpriteLoader.Instance.AddSprite("content/blaze_small");

            var explosion = new ParticleExplosion(World, fire_big, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 20)
            {
                Position = creature.VisualTarget,
            };
            explosion.OnUpdate += (particle) =>
            {
                if (particle.Frame.Time > 5 && particle.Frame.Time % 3 == 0)
                {
                    var flame = new ParticleExplosion(World, fire_small, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 6)
                    {
                        Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    };
                }
            };
        }
    }

    class HitBlackFlame : VisualPreset.AtCreature
    {
        public HitBlackFlame(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var fire_big = SpriteLoader.Instance.AddSprite("content/black_fire_big");
            var fire_small = SpriteLoader.Instance.AddSprite("content/black_fire_small");

            var explosion = new ParticleExplosion(World, fire_big, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 20)
            {
                Position = creature.VisualTarget,
            };
            explosion.OnUpdate += (particle) =>
            {
                if (particle.Frame.Time > 5 && particle.Frame.Time % 3 == 0)
                {
                    var flame = new ParticleExplosion(World, fire_small, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 6)
                    {
                        Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    };
                }
            };
        }
    }

    class HitInferno : VisualPreset.AtCreature
    {
        public HitInferno(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var fire_small = SpriteLoader.Instance.AddSprite("content/fire_small");

            var cloak = new Cloak(creature, 10);
            cloak.OnUpdate += (c) =>
            {
                float size = 0.15f + 0.15f * Random.NextFloat();
                size *= (float)LerpHelper.CircularOut(1, 0.5, c.Frame.GetSubSlide(10, c.Frame.EndTime));

                if (c.Frame.Time % 1 == 0)
                {
                    var pos = c.Creature.VisualTarget;
                    int totaltime = 5 + Random.Next(10);
                    float bigSize = 1.5f + Random.NextFloat();
                    Color color = new Color(255, 64, 16);

                    new ParticleCutter(World, totaltime + Random.Next(4))
                    {
                        Sprite = SpriteLoader.Instance.AddSprite("content/cutter"),
                        FadeSlide = 0.75f,
                        Position = pos,
                        Angle = Random.NextAngle(),
                        RotationStart = -MathHelper.TwoPi / totaltime,
                        RotationEnd = -(MathHelper.TwoPi / totaltime) * 0.5f,
                        AngleLerp = LerpHelper.Linear,
                        Scale = size,
                        ScaleEnd = size * bigSize,
                        ScaleLerp = LerpHelper.CircularOut,
                        Color = color,
                        Pass = DrawPass.EffectAdditive,
                    };
                }
            };

            for (int i = 0; i < 3; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 12 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 20;
                var waterParticle = new ParticleThrow(World, null, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * distance, 16 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    Scale = Random.NextFloat() * 0.75f + 0.25f,
                    Pass = DrawPass.EffectAdditive,
                };
                waterParticle.OnUpdate += (particle) =>
                {
                    if (particle.Frame.Time % 4 == i % 4)
                    {
                        var explosion = new ParticleExplosion(World, fire_small, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
                        {
                            Position = particle.Position,
                        };
                    }
                };
            }
        }
    }

    class HitBlizzard : VisualPreset.AtCreature
    {
        public HitBlizzard(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var particle = SpriteLoader.Instance.AddSprite("content/rock");

            var cloak = new Cloak(creature, 10);
            cloak.OnUpdate += (c) =>
            {
                float size = 0.15f + 0.15f * Random.NextFloat();
                size *= (float)LerpHelper.CircularOut(1, 0.5, c.Frame.GetSubSlide(10, c.Frame.EndTime));

                if (c.Frame.Time % 1 == 0)
                {
                    var pos = c.Creature.VisualTarget;
                    int totaltime = 5 + Random.Next(10);
                    float bigSize = 1.5f + Random.NextFloat();
                    Color color = new Color(64, 128, 255);

                    new ParticleCutter(World, totaltime + Random.Next(4))
                    {
                        Sprite = SpriteLoader.Instance.AddSprite("content/cutter"),
                        FadeSlide = 0.75f,
                        Position = pos,
                        Angle = Random.NextAngle(),
                        RotationStart = -MathHelper.TwoPi / totaltime,
                        RotationEnd = -(MathHelper.TwoPi / totaltime) * 0.5f,
                        AngleLerp = LerpHelper.Linear,
                        Scale = size,
                        ScaleEnd = size * bigSize,
                        ScaleLerp = LerpHelper.CircularOut,
                        Color = color,
                        Pass = DrawPass.EffectAdditive,
                    };
                }

                if(c.Frame.Time % 2 == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = Random.NextFloat() * MathHelper.TwoPi;
                        float distance = 12 + Random.NextFloat() * 12;
                        int time = Random.Next(10) + 10;
                        var iceParticle = new ParticleThrow(World, particle, creature.VisualTarget + Util.AngleToVector(angle) * 4, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                        {
                            SubImage = Random.Next(particle.SubImageCount),
                            Angle = Random.NextAngle(),
                            ColorMatrix = ColorMatrix.TwoColorLight(Color.Blue, Color.White),
                            Pass = DrawPass.EffectAdditive,
                        };
                    }
                }
            };
        }
    }

    class HitLight : VisualPreset.AtCreature
    {
        public HitLight(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var thunder = SpriteLoader.Instance.AddSprite("content/hit_light");
            var spark = SpriteLoader.Instance.AddSprite("content/lightning_spark_light");

            var explosion = new ParticleExplosion(World, thunder, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 20)
            {
                Position = creature.VisualTarget,
                Pass = DrawPass.EffectAdditive,
            };
            explosion.OnUpdate += (particle) =>
            {
                var volt = new ParticleDynamic(World, Random.Next(5))
                {
                    Sprite = spark,
                    SubImage = Random.Next(spark.SubImageCount),
                    Position = creature.VisualPosition() + creature.Mask.GetRandomPixel(Random),
                    Angle = Random.NextAngle(),
                    Pass = DrawPass.EffectAdditive,
                };
            };
        }
    }

    class HitDrought : VisualPreset.AtCreature
    {
        public HitDrought(SceneGame world) : base(world)
        {
        }

        public override void Activate(Creature creature)
        {
            var blob_water = SpriteLoader.Instance.AddSprite("content/pop_blob_water");
            var particle_water = SpriteLoader.Instance.AddSprite("content/bullet_water");
            var particle_ice = SpriteLoader.Instance.AddSprite("content/rock");

            var explosion = new ParticleExplosion(World, blob_water, Vector2.Zero, LerpHelper.Linear, LerpHelper.Linear, 10)
            {
                Position = creature.VisualTarget,
                Pass = DrawPass.EffectAdditive,
            };

            for (int i = 0; i < 6; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var waterParticle = new ParticleThrow(World, particle_water, creature.VisualTarget, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    Scale = Random.NextFloat() * 0.75f + 0.25f,
                    Pass = DrawPass.EffectAdditive,
                };
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = Random.NextFloat() * MathHelper.TwoPi;
                float distance = 4 + Random.NextFloat() * 12;
                int time = Random.Next(10) + 10;
                var iceParticle = new ParticleThrow(World, particle_ice, creature.VisualTarget + Util.AngleToVector(angle) * 2, creature.VisualTarget + Util.AngleToVector(angle) * distance, 4 + Random.NextFloat() * 8, LerpHelper.Linear, LerpHelper.QuadraticOut, time, 0.7f)
                {
                    SubImage = Random.Next(particle_ice.SubImageCount),
                    Angle = Random.NextAngle(),
                    ColorMatrix = ColorMatrix.TwoColorLight(Color.Blue, Color.White),
                    Pass = DrawPass.EffectAdditive,
                };
            }
        }
    }

}
