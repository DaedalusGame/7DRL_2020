using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Enemies
{
    abstract class Enemy : Creature
    {
        static public Random Random = new Random();
        public Creature AggroTarget;

        public virtual string BossMessage => "WARNING\n\nMANIFESTATION OF EXTINCTION UNIT";
        public virtual string[] BossDescription => new string[] { BossMessage };

        protected List<Skill> Skills = new List<Skill>();

        public Enemy(SceneGame world) : base(world)
        {
        }

        public void MakeAggressive(Creature target)
        {
            AggroTarget = target;
        }

        private Skill GetUsableSkill()
        {
            foreach (Skill skill in Skills.Shuffle())
            {
                if (skill.CanUse(this))
                {
                    return skill;
                }
            }

            return null;
        }

        private void FaceTowards(Rectangle target)
        {
            Rectangle source = Mask.GetRectangle(X,Y);

            int dx = Util.GetDeltaX(source, target);
            int dy = Util.GetDeltaY(source, target);

            Facing? newFacing = Util.GetFacing(dx, dy);
            if (newFacing != null)
                Facing = newFacing.Value;
        }

        private void FaceTowards(Creature target)
        {
            FaceTowards(target.Mask.GetRectangle(target.X,target.Y));
        }

        public override Wait TakeTurn(ActionQueue queue)
        {
            this.ResetTurn();
            Wait wait = Wait.NoWait;
            if (Dead)
                return wait;
            FaceTowards(AggroTarget);
            Skill usableSkill = GetUsableSkill();
            foreach (Skill skill in Skills)
                skill.Update();
            if (usableSkill != null)
            {
                CurrentAction = Scheduler.Instance.RunAndWait(RoutineUseSkill(this, usableSkill));
                wait = usableSkill.WaitUse ? CurrentAction : Wait.NoWait;
            }
            else
            {
                var move = new[] { Facing.North, Facing.East, Facing.South, Facing.West }.Pick(Random).ToOffset();
                CurrentAction = Scheduler.Instance.RunAndWait(RoutineMove(move.X, move.Y));
            }

            foreach (StatusEffect statusEffect in this.GetStatusEffects())
                statusEffect.Update();
            return wait;
        }

        private IEnumerable<Wait> RoutineUseSkill(Creature user, Skill skill)
        {
            foreach(Wait wait in skill.RoutineUse(user))
                yield return wait;
            skill.HideSkill(user);
        }

        public virtual void OnManifest()
        {
            //NOOP
        }

        public override void AddTooltip(ref string tooltip)
        {
            base.AddTooltip(ref tooltip);
            foreach(Skill skill in Skills)
            {
                if(!skill.Hidden(this))
                    tooltip += $"{skill.GetTooltip()}\n";
            }
        }
    }

    class CreatureDragonRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 8;
                    break;
                case (Facing.East):
                    facingOffset = 4;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 4;
                    break;
            }

            int frameOffset = 0;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Stand):
                    frameOffset = 1;
                    break;
                case (CreaturePose.Walk):
                    double lerp = LerpHelper.ForwardReverse(0, 2, (creature.Frame / 50.0) % 1);
                    frameOffset = (int)Math.Round(lerp);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 3;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 3;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(Sprite, facingOffset + frameOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }


    class CreatureFishRender : CreatureRender
    {
        ColorMatrix Color;

        public CreatureFishRender(ColorMatrix color)
        {
            Color = color;
        }

        public override void Draw(SceneGame scene, Creature creature)
        {
            SpriteReference fish = SpriteLoader.Instance.AddSprite("content/fish");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 2;
                    break;
                case (Facing.East):
                    facingOffset = 1;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 1;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(fish, facingOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class CreatureCannonRender : CreatureRender
    {
        ColorMatrix Color;

        public CreatureCannonRender(ColorMatrix color)
        {
            Color = color;
        }

        public override void Draw(SceneGame scene, Creature creature)
        {
            SpriteReference fish = SpriteLoader.Instance.AddSprite("content/cannon");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 2;
                    break;
                case (Facing.East):
                    facingOffset = 1;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 1;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(fish, facingOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class CreatureBlobRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int frameOffset = 0;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Stand):
                    frameOffset = (int)Math.Round(creature.Frame / 40.0);
                    break;
                case (CreaturePose.Walk):
                    frameOffset = (int)Math.Round(creature.Frame / 10.0);
                    break;
                case (CreaturePose.Attack):
                    frameOffset = (int)Math.Round(creature.Frame / 5.0);
                    break;
                case (CreaturePose.Cast):
                    frameOffset = (int)Math.Round(creature.Frame / 3.0);
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(Sprite, frameOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class BigCreatureRender : CreatureRender
    {
        public SpriteReference Sprite;
        public ColorMatrix Color = ColorMatrix.Identity;

        public override void Draw(SceneGame scene, Creature creature)
        {
            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            int facingOffset = 0;
            switch (creature.VisualFacing())
            {
                case (Facing.North):
                    facingOffset = 2;
                    break;
                case (Facing.East):
                    facingOffset = 1;
                    mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
                    break;
                case (Facing.South):
                    facingOffset = 0;
                    break;
                case (Facing.West):
                    facingOffset = 1;
                    break;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix);
            });
            scene.DrawSprite(Sprite, facingOffset, creature.VisualPosition(), mirror, 0);
            scene.PopSpriteBatch();
        }
    }

    class Gashwal : Enemy
    {
        public override Vector2 VisualTarget => VisualPosition() + new Vector2(16,16);

        public Gashwal(SceneGame world) : base(world)
        {
            Name = "Gashwal";
            Description = "Let's dance";

            Render = new BigCreatureRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/gashwal"),
                Color = ColorMatrix.TwoColor(new Color(69, 56, 37), new Color(223, 213, 198)),
            };
            Mask.Add(new Point(0, 0));
            Mask.Add(new Point(0, 1));
            Mask.Add(new Point(1, 0));
            Mask.Add(new Point(1, 1));

            Effect.Apply(new EffectStat(this, Stat.HP, 50));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillLightning());
            //Skills.Add(new SkillDrainTouch());
            //Skills.Add(new SkillAttack());
        }
    }

    class Wallhach : Enemy
    {
        Slider WingOpen = new Slider(60);

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

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillGeomancy());
            Skills.Add(new SkillHeptablast());
        }

        public override void OnManifest()
        {
            ActionQueue.AddImmediate(this);
        }

        public override IEnumerable<DrawPass> GetDrawPasses()
        {
            yield return DrawPass.Creature;
            yield return DrawPass.EffectAdditive;
        }

        public override void Update()
        {
            base.Update();
            if (Witnessed)
                WingOpen += 1;
        }

        public override void Draw(SceneGame scene, DrawPass pass)
        {
            if (pass == DrawPass.EffectAdditive)
            {
                if (Witnessed)
                {
                    DrawWing(scene, 9, (float)LerpHelper.QuadraticIn(0, 1, WingOpen.Slide), WingOpen.Slide, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
                    DrawWing(scene, 9, (float)LerpHelper.QuadraticIn(0, -1, WingOpen.Slide), WingOpen.Slide, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);
                }
            }
            else
            {
                base.Draw(scene, pass);
            }
        }

        private void DrawWing(SceneGame scene, int segments, float directionMod, float distanceMod, Microsoft.Xna.Framework.Graphics.SpriteEffects mirror)
        {
            SpriteReference hand = SpriteLoader.Instance.AddSprite("content/hand");
            int index = 0;
            for (int i = 1; i <= segments; i++)
            {
                int subSegments = 9;
                float angle = directionMod * MathHelper.ToRadians(90 - i * 5);
                float distance = (float)LerpHelper.Quadratic(10, distanceMod * 50, (float)i / segments);
                Vector2 pivot = VisualPosition() + Util.AngleToVector(angle) * distance;
                scene.DrawSpriteExt(hand, 0, pivot + GetHandOffset(index), hand.Middle, angle + directionMod * MathHelper.PiOver4, Vector2.One, mirror, new Color(244, 211, 23), 0);
                index++;
                for (int e = 0; e <= subSegments; e++)
                {
                    float subSegmentSlide = (float)e / (subSegments + 1);
                    float subAngle = angle - directionMod * MathHelper.ToRadians(i * 2);
                    float subDistance = distanceMod * e * 5;
                    float visAngle = subAngle + directionMod * MathHelper.PiOver2 + directionMod * MathHelper.ToRadians(i * -10);
                    scene.DrawSpriteExt(hand, 0, pivot + GetHandOffset(index) + Util.AngleToVector(subAngle) * subDistance, hand.Middle, visAngle, Vector2.One, mirror, new Color(244, 211, 23) * MathHelper.Lerp(0.3f, 1, subSegmentSlide), 0);
                    index++;
                }
            }
        }

        private Vector2 GetHandOffset(int index)
        {
            return Util.AngleToVector(index * 90 + MathHelper.ToRadians(Frame * 3)) * 2;
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

            Effect.Apply(new EffectStat(this, Stat.HP, 10));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Skills.Add(new SkillEnderBlast());
            Skills.Add(new SkillEnderRam());
            Skills.Add(new SkillEnderMow());
            Skills.Add(new SkillEnderClaw());
            Skills.Add(new SkillEnderPowerUp());
            Skills.Add(new SkillEnderFlare());
            Skills.Add(new SkillEnderQuake());
            Skills.Add(new SkillSideJump(3,5));
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

    class Ctholoid : Enemy
    {
        public Ctholoid(SceneGame world) : base(world)
        {
            Name = "Cthuloid";
            Description = "Inhabitant of the dark underground caverns";

            Render = new CreatureFishRender(ColorMatrix.TwoColor(Color.Black, Color.LightSeaGreen));
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 100));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDive());
            Skills.Add(new SkillWarp());
        }
    }

    class BlastCannon : Enemy
    {
        public BlastCannon(SceneGame world) : base(world)
        {
            Name = "Blast Cannon";
            Description = "High Sentry";

            Render = new CreatureCannonRender(ColorMatrix.TwoColor(Color.Black, Color.LightSeaGreen));
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 600));
            Effect.Apply(new EffectStat(this, Stat.Attack, 25));

            Skills.Add(new SkillCannonShot());
        }
    }

    class DeathKnight : Enemy
    {
        public DeathKnight(SceneGame world) : base(world)
        {
            Name = "Death Knight";
            Description = "Guardian of the fortress";

            Render = new CreaturePaperdollRender() {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_skull"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_armor_knight"),
                HeadColor = ColorMatrix.TwoColor(new Color(129, 166, 0), new Color(237, 255, 106)),
                BodyColor = ColorMatrix.TwoColor(Color.Black, Color.SeaGreen),
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 1200));
            Effect.Apply(new EffectStat(this, Stat.Attack, 40));
            this.AddStatusEffect(new Undead());

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillDeathSword());
            Skills.Add(new SkillBloodSword());
            Skills.Add(new SkillIronMaiden());
            Skills.Add(new SkillWarp());
        }
    }

    class Skeleton : Enemy
    {
        public Skeleton(SceneGame world) : base(world)
        {
            Name = "Skeleton";
            Description = "Dread mummy";

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_skull"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_robe"),
                BodyColor = ColorMatrix.TwoColor(new Color(69, 56, 37), new Color(223, 213, 198)),
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 50));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));
            this.AddStatusEffect(new Undead());

            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillAttack());
        }
    }

    class GoreVala : Enemy
    {
        public GoreVala(SceneGame world) : base(world)
        {
            Name = "Gore Vala";
            Description = "Toothy salmon with anger issues";

            Render = new CreatureFishRender(ColorMatrix.Identity);
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 80));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDive());
        }
    }

    class Vorrax : Enemy
    {
        public Vorrax(SceneGame world) : base(world)
        {
            Name = "Vorrax";
            Description = "Hungry hungry sea demon";

            Render = new CreatureFishRender(ColorMatrix.TwoColorLight(Color.Black,new Color(255,160,64)));
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 30));
            Effect.Apply(new EffectStat(this, Stat.Attack, 80));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDive());
        }
    }

    class BlueDragon : Enemy
    {
        public BlueDragon(SceneGame world) : base(world)
        {
            Name = "Teal Dragon";
            Description = "Lightning rod";

            Render = new CreatureDragonRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/dragon_blue")
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 440));
            Effect.Apply(new EffectStat(this, Stat.Attack, 25));
            Effect.Apply(new EffectStatMultiply(this, Element.Thunder.DamageRate, -1));

            Skills.Add(new SkillLightning());
            Skills.Add(new SkillAttack());
        }
    }

    class YellowDragon : Enemy
    {
        public YellowDragon(SceneGame world) : base(world)
        {
            Name = "Ochre Dragon";
            Description = "Eats armor for breakfast";

            Render = new CreatureDragonRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/dragon_yellow")
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 560));
            Effect.Apply(new EffectStat(this, Stat.Attack, 5));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillAcidTouch());
            Skills.Add(new SkillAcidTouch());
            Skills.Add(new SkillIronMaiden());
        }
    }

    class BoneDragon : Enemy
    {
        public BoneDragon(SceneGame world) : base(world)
        {
            Name = "Bone Dragon";
            Description = "Obliterator";

            Render = new CreatureDragonRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/dragon_bone")
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 1700));
            Effect.Apply(new EffectStat(this, Stat.Attack, 35));

            Skills.Add(new SkillForcefield());
            Skills.Add(new SkillAgeOfDragons());
            Skills.Add(new SkillOblivion());
        }
    }

    class AcidBlob : Enemy
    {
        public AcidBlob(SceneGame world) : base(world)
        {
            Name = "Acid Blob";
            Description = "I'm the trashman";

            Render = new CreatureBlobRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/blob_acid")
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 120));
            Effect.Apply(new EffectStat(this, Stat.Attack, 15));

            Skills.Add(new SkillAcidTouch());
            Skills.Add(new SkillAttack());
        }
    }

    class PoisonBlob : Enemy
    {
        public PoisonBlob(SceneGame world) : base(world)
        {
            Name = "Poison Blob";
            Description = "How dare you";

            Render = new CreatureBlobRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/blob_poison")
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 120));
            Effect.Apply(new EffectStat(this, Stat.Attack, 15));

            Skills.Add(new SkillPoisonTouch());
            Skills.Add(new SkillAttack());
        }
    }
}
