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

            Effect.Apply(new EffectStat(this, Stat.HP, 440));
            Effect.Apply(new EffectStat(this, Stat.Attack, 25));
            Effect.Apply(new EffectStatMultiply(this, Element.Fire.DamageRate, -1));

            Effect.Apply(new EffectFamily(this, Family.Dragon));

            Skills.Add(new SkillFireBreath());
            Skills.Add(new SkillIceBreath());
            Skills.Add(new SkillAttack());

            Effect.Apply(new EffectTrait(this, Trait.DeathThroesCrimson));
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

            Effect.Apply(new EffectFamily(this, Family.Dragon));

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

            Effect.Apply(new EffectFamily(this, Family.Dragon));

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

            Effect.Apply(new EffectFamily(this, Family.Dragon));
            Effect.Apply(new EffectTrait(this, Trait.Undead));
            Effect.Apply(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillForcefield());
            Skills.Add(new SkillAgeOfDragons());
            Skills.Add(new SkillOblivion());
        }
    }
}
