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
    class ArmorTenmoku : Enemy
    {
        public ArmorTenmoku(SceneGame world) : base(world)
        {
            Name = "Tenmoku Armor";
            Description = "It glazes on its own";

            ColorMatrix tenmoku = ColorMatrix.TwoColor(new Color(89, 70, 55), new Color(114, 126, 141));

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_head1"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_double"),
                HeadColor = tenmoku,
                BodyColor = tenmoku,
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Overclock, 1));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Earth.DamageRate, -0.5));

            Skills.Add(new SkillPetrolVolley());
            Skills.Add(new SkillOilBlast());
        }

        [Construct("armor_tenmoku")]
        public static ArmorTenmoku Construct(Context context)
        {
            return new ArmorTenmoku(context.World);
        }
    }

    class ArmorParis : Enemy
    {
        public ArmorParis(SceneGame world) : base(world)
        {
            Name = "Paris Armor";
            Description = "It corrodes on its own";

            ColorMatrix paris = ColorMatrix.TwoColor(new Color(27, 82, 65), new Color(118, 231, 200));

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_head2"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_double"),
                HeadColor = paris,
                BodyColor = paris,
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Overclock, 1));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Acid.DamageRate, -0.5));

            Skills.Add(new SkillAcidVolley());
        }

        [Construct("armor_paris")]
        public static ArmorParis Construct(Context context)
        {
            return new ArmorParis(context.World);
        }
    }

    class ArmorBrine : Enemy
    {
        public ArmorBrine(SceneGame world) : base(world)
        {
            Name = "Brine Armor";
            Description = "It dehydrates on its own";

            ColorMatrix salt = ColorMatrix.TwoColor(new Color(131, 137, 158), new Color(255, 255, 255));

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_head3"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_double"),
                HeadColor = salt,
                BodyColor = salt,
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Overclock, 1));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Water.DamageRate, -0.5));

            Skills.Add(new SkillSaltVolley());
        }

        [Construct("armor_brine")]
        public static ArmorBrine Construct(Context context)
        {
            return new ArmorBrine(context.World);
        }
    }

    class ArmorCrystal : Enemy
    {
        public ArmorCrystal(SceneGame world) : base(world)
        {
            Name = "Crystal Armor";
            Description = "It refracts on its own";

            ColorMatrix crystal = ColorMatrix.TwoColor(new Color(154, 121, 223), new Color(206, 255, 253));

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_head4"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_double"),
                HeadColor = crystal,
                BodyColor = crystal,
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Overclock, 1));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Thunder.DamageRate, -0.5));

            Skills.Add(new SkillChainLightningVolley());
        }

        [Construct("armor_crystal")]
        public static ArmorCrystal Construct(Context context)
        {
            return new ArmorCrystal(context.World);
        }
    }

    class ArmorSullen : Enemy
    {
        public ArmorSullen(SceneGame world) : base(world)
        {
            Name = "Sullen Armor";
            Description = "It sinks on its own";

            ColorMatrix sullen = ColorMatrix.TwoColor(new Color(95, 104, 122), new Color(125, 174, 173));

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_head3"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_double"),
                HeadColor = sullen,
                BodyColor = sullen,
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Overclock, 1));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Water.DamageRate, -0.5));

            Skills.Add(new SkillPoisonVolley());
        }

        [Construct("armor_sullen")]
        public static ArmorSullen Construct(Context context)
        {
            return new ArmorSullen(context.World);
        }
    }

    class ArmorBone : Enemy
    {
        public ArmorBone(SceneGame world) : base(world)
        {
            Name = "Bone Armor";
            Description = "It rots on its own";

            ColorMatrix bone = ColorMatrix.TwoColor(new Color(98, 23, 23), new Color(163, 163, 163));

            Render = new CreaturePaperdollRender()
            {
                Head = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_head5"),
                Body = SpriteLoader.Instance.AddSprite("content/paperdoll_machine_double"),
                HeadColor = bone,
                BodyColor = bone,
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectFamily(this, Family.Construct));
            Effect.ApplyInnate(new EffectFamily(this, Family.Bloodless));

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 400));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 5));

            Effect.ApplyInnate(new EffectTrait(this, Trait.Undead, 1));

            Effect.ApplyInnate(new EffectStatPercent(this, Element.Dark.DamageRate, -0.5));

            Skills.Add(new SkillBoneVolley());
        }

        [Construct("armor_bone")]
        public static ArmorBone Construct(Context context)
        {
            return new ArmorBone(context.World);
        }
    }
}
