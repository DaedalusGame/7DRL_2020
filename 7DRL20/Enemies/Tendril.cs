using Microsoft.Xna.Framework;
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
    class CreatureTendrilRenderer : CreatureRender
    {
        ColorMatrix Color;

        public CreatureTendrilRenderer(ColorMatrix color)
        {
            Color = color;
        }

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            SpriteReference abyssalTendril = SpriteLoader.Instance.AddSprite("content/abyssal_tendril");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            Random rand = new Random(this.GetHashCode());
            int wiggleFrame = poseData.PoseFrame + rand.Next(100);

            bool tendrilDown = IsTendrilDown(poseData.Pose);
            bool tendrilDownLast = IsTendrilDown(poseData.PoseLast);
            int frameOffset = 0;
            bool invisible = poseData.Pose == CreaturePose.Walk;
            switch (poseData.Pose)
            {
                case (CreaturePose.Stand):
                    frameOffset = 2 + (wiggleFrame / 10) % 4;
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 2 + (wiggleFrame / 2) % 4;
                    break;
                default:
                    frameOffset = 2 + (wiggleFrame / 4) % 4;
                    break;
            }

            if (tendrilDown != tendrilDownLast && poseData.PoseFrame < 5 + rand.Next(20))
            {
                frameOffset = 0 + (wiggleFrame / 10) % 2;
                invisible = false;
            }

            if (invisible)
                return;

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(abyssalTendril, frameOffset, pos - new Vector2(0, 8), mirror, color, 0);
            scene.PopSpriteBatch();
        }

        private bool IsTendrilDown(CreaturePose pose)
        {
            return pose == CreaturePose.Walk;
        }
    }

    class CreatureTendrilBushRender : CreatureRender
    {
        ColorMatrix Color;

        public CreatureTendrilBushRender(ColorMatrix color)
        {
            Color = color;
        }

        public override void DrawFrame(SceneGame scene, Vector2 pos, PoseData poseData, Facing facing, Matrix transform, Color color, ColorMatrix colorMatrix)
        {
            SpriteReference abyssalTendril = SpriteLoader.Instance.AddSprite("content/abyssal_tendril_bush");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;

            bool tendrilDown = IsTendrilDown(poseData.Pose);
            bool tendrilDownLast = IsTendrilDown(poseData.PoseLast);
            int frameOffset = 0;
            switch (poseData.Pose)
            {
                case (CreaturePose.Attack):
                    frameOffset = 3 + (poseData.PoseFrame / 2) % 4;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 3 + (poseData.PoseFrame / 4) % 4;
                    break;
            }

            if (tendrilDown != tendrilDownLast && poseData.PoseFrame < 15)
            {
                frameOffset = 1 + (poseData.PoseFrame / 10) % 2;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * colorMatrix, transform * matrix, projection);
            });
            scene.DrawSprite(abyssalTendril, frameOffset, pos - new Vector2(0, 16), mirror, color, 0);
            scene.PopSpriteBatch();
        }

        private bool IsTendrilDown(CreaturePose pose)
        {
            return pose == CreaturePose.Stand || pose == CreaturePose.Walk;
        }
    }

    class AbyssalTendril : Enemy
    {
        public AbyssalTendril(SceneGame world) : base(world)
        {
            Name = "Abyssal Tendril";
            Description = "Dread mummy";

            Render = new CreatureTendrilRenderer(ColorMatrix.Identity);
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 80));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectFamily(this, Family.Plant));
            Effect.ApplyInnate(new EffectFamily(this, Family.Abyssal));

            Effect.ApplyInnate(new EffectTrait(this, Trait.AcidBlood));

            Skills.Add(new SkillTentacleBash());
            Skills.Add(new SkillTentacleSlash());
            Skills.Add(new SkillWallop());
        }

        [Construct("abyssal_tendril")]
        public static AbyssalTendril Construct(Context context)
        {
            return new AbyssalTendril(context.World);
        }
    }

    class AbyssalTendrilBud : Enemy
    {
        public AbyssalTendrilBud(SceneGame world) : base(world)
        {
            Name = "Abyssal Tendril Bud";
            Description = "Dread mummy";

            Render = new CreatureStaticRender()
            {
                Color = ColorMatrix.Identity,
                Sprite = SpriteLoader.Instance.AddSprite("abyssal_tendril_bud"),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 200));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectFamily(this, Family.Plant));
            Effect.ApplyInnate(new EffectFamily(this, Family.Abyssal));

            Effect.ApplyInnate(new EffectTrait(this, Trait.BloodThroesAcid));
            Effect.ApplyInnate(new EffectTrait(this, Trait.DeathThroesTendril));
            Effect.ApplyInnate(new EffectMovementType(this, MovementType.Stationary, 10));

            Skills.Add(new SkillCreateTentacles());
            Skills.Add(new SkillVenomBite());
        }

        [Construct("abyssal_tendril_bud")]
        public static AbyssalTendrilBud Construct(Context context)
        {
            return new AbyssalTendrilBud(context.World);
        }
    }

    class AbyssalTendrilBush : Enemy
    {
        public AbyssalTendrilBush(SceneGame world) : base(world)
        {
            Name = "Abyssal Tendril Bush";
            Description = "Dread mummy";

            Render = new CreatureTendrilBushRender(ColorMatrix.Identity);
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 500));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectFamily(this, Family.Plant));
            Effect.ApplyInnate(new EffectFamily(this, Family.Abyssal));

            Effect.ApplyInnate(new EffectTrait(this, Trait.BloodThroesAcid));
            Effect.ApplyInnate(new EffectTrait(this, Trait.DeathThroesTendril));
            Effect.ApplyInnate(new EffectMovementType(this, MovementType.Stationary, 10));

            Skills.Add(new SkillCreateTentacles());
            Skills.Add(new SkillGrabbingTentacle());
            Skills.Add(new SkillVenomBite());
            Skills.Add(new SkillLick());
        }

        [Construct("abyssal_tendril_bush")]
        public static AbyssalTendrilBush Construct(Context context)
        {
            return new AbyssalTendrilBush(context.World);
        }
    }

    class AbyssalTendrilTree : Enemy
    {
        public AbyssalTendrilTree(SceneGame world) : base(world)
        {
            Name = "Abyssal Tendril Tree";
            Description = "Dread mummy";

            Render = new CreatureStaticRender()
            {
                Color = ColorMatrix.Identity,
                Sprite = SpriteLoader.Instance.AddSprite("abyssal_tendril_tree"),
            };
            Mask.Add(new Point(0, 0));
            Mask.Add(new Point(0, 1));
            Mask.Add(new Point(1, 0));
            Mask.Add(new Point(1, 1));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 950));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 50));

            Effect.ApplyInnate(new EffectFamily(this, Family.Plant));
            Effect.ApplyInnate(new EffectFamily(this, Family.Abyssal));

            Effect.ApplyInnate(new EffectTrait(this, Trait.BloodThroesAcid));
            Effect.ApplyInnate(new EffectTrait(this, Trait.DeathThroesTendril));
            Effect.ApplyInnate(new EffectMovementType(this, MovementType.Stationary, 10));

            Skills.Add(new SkillCreateTentacles());
            Skills.Add(new SkillGrabbingTentacle());
            Skills.Add(new SkillVenomBite());
            Skills.Add(new SkillLick());
        }

        [Construct("abyssal_tendril_tree")]
        public static AbyssalTendrilTree Construct(Context context)
        {
            return new AbyssalTendrilTree(context.World);
        }
    }
}
