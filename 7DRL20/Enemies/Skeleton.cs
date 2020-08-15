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

            Effect.Apply(new EffectTrait(this, Trait.Undead));
            Effect.Apply(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillAttack());
            Skills.Add(new SkillSpinSlash());
        }
    }

    class PeatMummy : Enemy
    {
        public PeatMummy(SceneGame world) : base(world)
        {
            Name = "Peat Mummy";
            Description = "Preserved for eternity";

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_peatskull"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_peatmummy"),
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 50));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Effect.Apply(new EffectTrait(this, Trait.Undead));
            Effect.Apply(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillMudTouch());
        }
    }

    class PrettyLich : Enemy
    {
        public PrettyLich(SceneGame world) : base(world)
        {
            Name = "Pretty Lich";
            Description = "I AM BEAUTIFUL";

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_pretty_lich"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_robe"),
                BodyColor = ColorMatrix.TwoColor(new Color(69, 56, 37), new Color(198, 213, 223)),
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 50));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Effect.Apply(new EffectTrait(this, Trait.Undead));
            Effect.Apply(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillAttack());
        }
    }

    class DeathKnight : Enemy
    {
        public DeathKnight(SceneGame world) : base(world)
        {
            Name = "Death Knight";
            Description = "Guardian of the fortress";

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_skull"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_armor_knight"),
                HeadColor = ColorMatrix.TwoColor(new Color(129, 166, 0), new Color(237, 255, 106)),
                BodyColor = ColorMatrix.TwoColor(Color.Black, Color.SeaGreen),
            };
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 1200));
            Effect.Apply(new EffectStat(this, Stat.Attack, 40));

            Effect.Apply(new EffectTrait(this, Trait.Undead));
            Effect.Apply(new EffectFamily(this, Family.Bloodless));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDrainTouch());
            Skills.Add(new SkillDeathSword());
            Skills.Add(new SkillBloodSword());
            Skills.Add(new SkillIronMaiden());
            Skills.Add(new SkillWarp());
        }
    }
}
