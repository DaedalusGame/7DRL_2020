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
    class RedDragon : Enemy
    {
        public RedDragon(SceneGame world) : base(world)
        {
            Name = "Crimson Dragon";
            Description = "Ignition";

            Render = new CreatureDragonRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/dragon_red")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 440));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 25));
            Effect.ApplyInnate(new EffectStatMultiply(this, Element.Fire.DamageRate, -1));

            Effect.ApplyInnate(new EffectFamily(this, Family.Dragon));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillFireBreath());

            Effect.Apply(new EffectTrait(this, Trait.DeathThroesCrimson));
        }

        [Construct("dragon_red")]
        public static RedDragon Construct(Context context)
        {
            return new RedDragon(context.World);
        }
    }

    class WhiteDragon : Enemy
    {
        public WhiteDragon(SceneGame world) : base(world)
        {
            Name = "Pale Dragon";
            Description = "White out";

            Render = new CreatureDragonRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/dragon_white")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 800));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 50));
            Effect.ApplyInnate(new EffectStatMultiply(this, Element.Ice.DamageRate, -1));

            Effect.ApplyInnate(new EffectFamily(this, Family.Dragon));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillIceBreath());

            //Effect.Apply(new EffectTrait(this, Trait.DeathThroesCrimson));
        }

        [Construct("dragon_white")]
        public static WhiteDragon Construct(Context context)
        {
            return new WhiteDragon(context.World);
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

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 440));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 25));
            Effect.ApplyInnate(new EffectStatMultiply(this, Element.Thunder.DamageRate, -1));

            Effect.ApplyInnate(new EffectFamily(this, Family.Dragon));
            Effect.ApplyInnate(new EffectTrait(this, Trait.LightningField));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillLightning());
        }

        [Construct("dragon_blue")]
        public static BlueDragon Construct(Context context)
        {
            return new BlueDragon(context.World);
        }
    }

    class GreenDragon : Enemy
    {
        public GreenDragon(SceneGame world) : base(world)
        {
            Name = "Viridian Dragon";
            Description = "Deadly halitosis";

            Render = new CreatureDragonRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/dragon_green")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 670));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 20));

            Effect.ApplyInnate(new EffectFamily(this, Family.Dragon));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillViperBite());
        }

        [Construct("dragon_green")]
        public static GreenDragon Construct(Context context)
        {
            return new GreenDragon(context.World);
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

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 560));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectFamily(this, Family.Dragon));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillRendingClaw());
            Skills.Add(new SkillAcidTouch());
            Skills.Add(new SkillIronMaiden());
        }

        [Construct("dragon_yellow")]
        public static YellowDragon Construct(Context context)
        {
            return new YellowDragon(context.World);
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

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 1700));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 35));

            Effect.ApplyInnate(new EffectFamily(this, Family.Dragon));
            Effect.ApplyInnate(new EffectTrait(this, Trait.Undead));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillForcefield());
            Skills.Add(new SkillAgeOfDragons());
            Skills.Add(new SkillOblivion());
        }

        [Construct("dragon_bone")]
        public static BoneDragon Construct(Context context)
        {
            return new BoneDragon(context.World);
        }
    }
}
