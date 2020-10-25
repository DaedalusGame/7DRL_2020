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

        public override void Draw(SceneGame scene, Creature creature)
        {
            SpriteReference abyssalTendril = SpriteLoader.Instance.AddSprite("content/abyssal_tendril");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
            Random rand = new Random(creature.GetHashCode());
            int frame = creature.PoseFrame + rand.Next(100);

            bool tendrilDown = IsTendrilDown(creature.Pose);
            bool tendrilDownLast = IsTendrilDown(creature.PoseLast);
            int frameOffset = 0;
            bool invisible = creature.Pose == CreaturePose.Walk;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Stand):
                    frameOffset = 2 + (frame / 10) % 4;
                    break;
                case (CreaturePose.Attack):
                    frameOffset = 2 + (frame / 2) % 4;
                    break;
                default:
                    frameOffset = 2 + (frame / 4) % 4;
                    break;
            }

            if (tendrilDown != tendrilDownLast && creature.PoseFrame < 5 + rand.Next(20))
            {
                frameOffset = 0 + (frame / 10) % 2;
                invisible = false;
            }

            if (invisible)
                return;

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(abyssalTendril, frameOffset, creature.VisualPosition() - new Vector2(0, 8), mirror, 0);
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

        public override void Draw(SceneGame scene, Creature creature)
        {
            SpriteReference abyssalTendril = SpriteLoader.Instance.AddSprite("content/abyssal_tendril_bush");

            var mirror = Microsoft.Xna.Framework.Graphics.SpriteEffects.None;

            bool tendrilDown = IsTendrilDown(creature.Pose);
            bool tendrilDownLast = IsTendrilDown(creature.PoseLast);
            int frameOffset = 0;
            switch (creature.VisualPose())
            {
                case (CreaturePose.Attack):
                    frameOffset = 3 + (creature.PoseFrame / 2) % 4;
                    break;
                case (CreaturePose.Cast):
                    frameOffset = 3 + (creature.PoseFrame / 4) % 4;
                    break;
            }

            if (tendrilDown != tendrilDownLast && creature.PoseFrame < 15)
            {
                frameOffset = 1 + (creature.PoseFrame / 10) % 2;
            }

            scene.PushSpriteBatch(shader: scene.Shader, shaderSetup: (matrix, projection) =>
            {
                scene.SetupColorMatrix(Color * creature.VisualColor(), matrix, projection);
            });
            scene.DrawSprite(abyssalTendril, frameOffset, creature.VisualPosition() - new Vector2(0, 16), mirror, 0);
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
}
