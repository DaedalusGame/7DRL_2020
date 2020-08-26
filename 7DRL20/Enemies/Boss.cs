using Microsoft.Xna.Framework;
using RoguelikeEngine.Attacks;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Skills;
using RoguelikeEngine.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Enemies
{
    class Gashwal : Enemy
    {
        public override Vector2 VisualTarget => VisualPosition() + new Vector2(16, 16);

        public Gashwal(SceneGame world) : base(world)
        {
            Name = "Gashwal";
            Description = "Let's dance";

            Render = new BigCreatureRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/gashwal"),
            };
            Mask.Add(new Point(0, 0));
            Mask.Add(new Point(0, 1));
            Mask.Add(new Point(1, 0));
            Mask.Add(new Point(1, 1));

            Effect.Apply(new EffectStat(this, Stat.HP, 900));
            Effect.Apply(new EffectStat(this, Stat.Attack, 50));

            Effect.Apply(new EffectFamily(this, Family.Boss));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillWildSpin());
            Skills.Add(new SkillPuddleStomp());
            Skills.Add(new SkillRainDance());
            Skills.Add(new SkillPrance(3, 5));
            Skills.Add(new SkillPounce(0, 5));
        }
    }

    class GashwalHairy : Enemy
    {
        public override Vector2 VisualTarget => VisualPosition() + new Vector2(16, 16);

        public GashwalHairy(SceneGame world) : base(world)
        {
            Name = "Hairy Gashwal";
            Description = "Disco Inferno";

            Render = new BigCreatureRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/hairy_gashwal"),
            };
            Mask.Add(new Point(0, 0));
            Mask.Add(new Point(0, 1));
            Mask.Add(new Point(1, 0));
            Mask.Add(new Point(1, 1));

            Effect.Apply(new EffectStat(this, Stat.HP, 1200));
            Effect.Apply(new EffectStat(this, Stat.Attack, 50));

            Effect.Apply(new EffectFamily(this, Family.Boss));

            Effect.Apply(new EffectTrait(this, Trait.Sparking));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillLightningClaw());
            Skills.Add(new SkillWildSpin());
            Skills.Add(new SkillPuddleStomp());
            Skills.Add(new SkillLightningDance());
            Skills.Add(new SkillRainDance());
            Skills.Add(new SkillPrance(3, 5));
            Skills.Add(new SkillPounce(0, 5));
        }
    }

    class Wallhach : Enemy
    {
        public Func<float> WingOpen = () => 0;
        public Func<Color> WingColor = () => Color.Goldenrod;
        public Func<SpriteReference> WingSprite = () => SpriteLoader.Instance.AddSprite("content/hand");

        private bool LastWitnessed = false;
        private bool Witnessed => World.SeenBosses.Contains(this);

        public Wallhach(SceneGame world) : base(world)
        {
            Name = "Wallhach";
            Description = "Ancient minister of the end times";

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_hood"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_armor"),
                HeadColor = ColorMatrix.TwoColor(new Color(233, 197, 50), new Color(255, 254, 213)),
                BodyColor = ColorMatrix.TwoColor(new Color(233, 197, 50), new Color(255, 254, 213)),
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 1200));
            Effect.Apply(new EffectStat(this, Stat.Attack, 40));

            Effect.Apply(new EffectFamily(this, Family.Boss));

            //Skills.Add(new SkillAttack());
            Skills.Add(new SkillPhalange());
            Skills.Add(new SkillGeomancy());
            Skills.Add(new SkillDeltaAttack());
            Skills.Add(new SkillHeptablast());
            Skills.Add(new SkillWedlock());
        }

        public override void OnManifest()
        {
            Control.AddImmediate();
        }

        public override void Update()
        {
            base.Update();
            if (Witnessed && !LastWitnessed)
                WingOpen = Slide(0, 1, LerpHelper.Linear, 60);
            LastWitnessed = Witnessed;
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Creature;
            yield return DrawPass.EffectAdditive;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            if (pass == DrawPass.EffectAdditive)
            {
                if (Witnessed)
                {
                    float wingOpen = WingOpen();
                    Color windColor = WingColor();
                    SpriteReference wingSprite = WingSprite();
                    DrawWing(scene, wingSprite, windColor, 9, (float)LerpHelper.QuadraticIn(0, 1, wingOpen), wingOpen, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
                    DrawWing(scene, wingSprite, windColor, 9, (float)LerpHelper.QuadraticIn(0, -1, wingOpen), wingOpen, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);
                }
            }
            else
            {
                base.Draw(scene, pass);
            }
        }

        private void DrawWing(SceneGame scene, SpriteReference sprite, Color color, int segments, float directionMod, float distanceMod, Microsoft.Xna.Framework.Graphics.SpriteEffects mirror)
        {
            //new Color(244, 211, 23)
            //SpriteReference hand = SpriteLoader.Instance.AddSprite("content/hand");
            int index = 0;
            for (int i = 1; i <= segments; i++)
            {
                int subSegments = 9;
                float angle = directionMod * MathHelper.ToRadians(90 - i * 5);
                float distance = (float)LerpHelper.Quadratic(10, distanceMod * 50, (float)i / segments);
                Vector2 pivot = VisualPosition() + Util.AngleToVector(angle) * distance;
                scene.DrawSpriteExt(sprite, 0, pivot + GetHandOffset(index), sprite.Middle, angle + directionMod * MathHelper.PiOver4, Vector2.One, mirror, color, 0);
                index++;
                for (int e = 0; e <= subSegments; e++)
                {
                    float subSegmentSlide = (float)e / (subSegments + 1);
                    float subAngle = angle - directionMod * MathHelper.ToRadians(i * 2);
                    float subDistance = distanceMod * e * 5;
                    float visAngle = subAngle + directionMod * MathHelper.PiOver2 + directionMod * MathHelper.ToRadians(i * -10);
                    scene.DrawSpriteExt(sprite, 0, pivot + GetHandOffset(index) + Util.AngleToVector(subAngle) * subDistance, sprite.Middle, visAngle, Vector2.One, mirror, color * MathHelper.Lerp(0.3f, 1, subSegmentSlide), 0);
                    index++;
                }
            }
        }

        public static List<Vector2> GetWingPositions(Vector2 position, float slide)
        {
            List<Vector2> positions = new List<Vector2>();
            positions.AddRange(GetWingPositions(position, 9, (float)LerpHelper.QuadraticIn(0, 1, slide), slide));
            positions.AddRange(GetWingPositions(position, 9, (float)LerpHelper.QuadraticIn(0, -1, slide), slide));
            return positions;
        }

        private static IEnumerable<Vector2> GetWingPositions(Vector2 position, int segments, float directionMod, float distanceMod)
        {
            int index = 0;
            for (int i = 1; i <= segments; i++)
            {
                int subSegments = 9;
                float angle = directionMod * MathHelper.ToRadians(90 - i * 5);
                float distance = (float)LerpHelper.Quadratic(10, distanceMod * 50, (float)i / segments);
                Vector2 pivot = position + Util.AngleToVector(angle) * distance;
                yield return pivot;
                index++;
                for (int e = 0; e <= subSegments; e++)
                {
                    float subAngle = angle - directionMod * MathHelper.ToRadians(i * 2);
                    float subDistance = distanceMod * e * 5;
                    yield return pivot + Util.AngleToVector(subAngle) * subDistance;
                    index++;
                }
            }
        }

        private Vector2 GetHandOffset(int index)
        {
            return Util.AngleToVector(index * 90 + MathHelper.ToRadians(Frame * 3)) * 2;
        }

        public IEnumerable<Wait> RoutineOpenWing(float slide, int time, LerpHelper.Delegate lerp)
        {
            WingOpen = Slide(WingOpen(), slide, lerp, time);
            WingColor = Static<Color>(Color.Goldenrod);
            yield return WaitSome(time);
        }

        public IEnumerable<Wait> RoutineFlashWing(int time)
        {
            WingOpen = Slide(WingOpen(), 1.0f, LerpHelper.QuadraticIn, time);
            WingColor = Slide(WingColor(), Color.White, LerpHelper.QuadraticIn, time);
            yield return WaitSome(time);
            WingColor = Slide(Color.White, Color.TransparentBlack, LerpHelper.QuadraticIn, 10);
            yield return WaitSome(10);
            WingOpen = Static(0f);
        }

        public override IEnumerable<Wait> RoutineDie(Vector2 dir)
        {
            var pos = new Vector2(Tile.X * 16, Tile.Y * 16);
            VisualPosition = Slide(pos, pos + new Vector2(dir.X * 8, dir.Y * 8), LerpHelper.Linear, 20);
            VisualPose = Static(CreaturePose.Stand);
            VisualColor = SoftFlash(ColorMatrix.Identity, ColorMatrix.Flat(Color.White), LerpHelper.QuadraticOut, 10);
            DeadWait = new WaitTime(200);
            yield return Scheduler.Instance.RunAndWait(RoutineOpenWing(0.8f, 50, LerpHelper.Quadratic));
            new BossExplosion(World, this, (position, velocity, angle, time) => new FireExplosion(World, position, velocity, angle, time));
        }

        public override IEnumerable<Wait> RoutineDestroy()
        {
            yield return DeadWait;
            yield return Scheduler.Instance.RunAndWait(RoutineFlashWing(15));
            if (Dead && !Destroyed && this != World.Player)
                this.Destroy();
            new ScreenFlashLocal(World, () => ColorMatrix.Sun(), VisualTarget, 60, 150, 80, 50);
            new FireNuke(World, SpriteLoader.Instance.AddSprite("content/nuke_fire"), VisualTarget, 1, 80);
            new ScreenShakeRandom(World, 8, 80, LerpHelper.QuarticIn);

            yield return new WaitTime(100);
        }
    }

    class Erebizo : Enemy
    {
        public override Vector2 VisualTarget => VisualPosition() + new Vector2(16, 16);

        public Erebizo(SceneGame world) : base(world)
        {
            Name = "Erebizo";
            Description = "Insatiable";

            Render = new BigCreatureRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/erebizo"),
                Color = ColorMatrix.Identity,
            };
            Mask.Add(new Point(0, 0));
            Mask.Add(new Point(0, 1));
            Mask.Add(new Point(1, 0));
            Mask.Add(new Point(1, 1));

            Effect.Apply(new EffectStat(this, Stat.HP, 50));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Effect.Apply(new EffectFamily(this, Family.Boss));

            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillLightning());
            //Skills.Add(new SkillDrainTouch());
            //Skills.Add(new SkillAttack());
        }
    }

    class EnderErebizo : Enemy
    {
        public override Vector2 VisualTarget => VisualPosition() + new Vector2(16, 16);

        public EnderErebizo(SceneGame world) : base(world)
        {
            Name = "Ender Erebizo";
            Description = "The end has come";

            Render = new BigCreatureRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/ender_erebizo"),
                Color = ColorMatrix.Identity,
            };
            Mask.Add(new Point(0, 0));
            Mask.Add(new Point(0, 1));
            Mask.Add(new Point(1, 0));
            Mask.Add(new Point(1, 1));

            Effect.Apply(new EffectStat(this, Stat.HP, 3000));
            Effect.Apply(new EffectStat(this, Stat.Attack, 160));

            Effect.Apply(new EffectFamily(this, Family.Boss));

            Skills.Add(new SkillEnderBlast());
            Skills.Add(new SkillEnderRam());
            Skills.Add(new SkillEnderMow());
            Skills.Add(new SkillEnderClaw());
            Skills.Add(new SkillEnderPowerUp());
            Skills.Add(new SkillEnderFlare());
            Skills.Add(new SkillEnderQuake());
            Skills.Add(new SkillSideJump(3, 5));
        }

        public override void Update()
        {
            base.Update();

            SpriteReference cinder = SpriteLoader.Instance.AddSprite("content/cinder_ender");

            BigCreatureRender render = (BigCreatureRender)Render;
            bool powered = this.HasStatusEffect(statusEffect => statusEffect is PoweredUp);
            if (powered)
            {
                render.Sprite = SpriteLoader.Instance.AddSprite("content/ender_erebizo_powered");
                for (int i = 0; i < 1; i++)
                {
                    Vector2 emitPos = new Vector2(X * 16, Y * 16) + Mask.GetRandomPixel(Random);
                    Vector2 centerPos = VisualTarget;
                    Vector2 velocity = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * (Random.NextFloat() + 0.5f);
                    velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f);
                    new Cinder(World, cinder, emitPos, velocity, Random.Next(90) + 20);
                }
            }
            else
                render.Sprite = SpriteLoader.Instance.AddSprite("content/ender_erebizo");
        }
    }
}
