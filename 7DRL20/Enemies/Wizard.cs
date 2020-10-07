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
    class WizardImpact : Enemy
    {
        public WizardImpact(SceneGame world) : base(world)
        {
            Name = "Impactimancer";
            Description = "";

            Render = new CreaturePaperdollRender()
            {
                HeadColor = ColorMatrix.Tint(Color.LightSkyBlue),
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_hat_gray"),
                BodyColor = ColorMatrix.Tint(new Color(128, 255, 192)),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_robe"),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 320));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Skills.Add(new SkillImpact());
            Skills.Add(new SkillMagicPower());
        }

        [Construct("wizard_impact")]
        public static WizardImpact Construct(Context context)
        {
            return new WizardImpact(context.World);
        }
    }

    class WizardFire : Enemy
    {
        public WizardFire(SceneGame world) : base(world)
        {
            Name = "Pyromancer";
            Description = "";

            Render = new CreaturePaperdollRender()
            {
                HeadColor = ColorMatrix.Tint(new Color(255, 16, 16)),
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_hat_gray"),
                BodyColor = ColorMatrix.Tint(new Color(255, 128, 16)),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_robe"),
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 320));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Skills.Add(new SkillMagicPower());
        }

        [Construct("wizard_fire")]
        public static WizardFire Construct(Context context)
        {
            return new WizardFire(context.World);
        }
    }
}
