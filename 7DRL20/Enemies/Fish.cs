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
    class GoreVala : Enemy
    {
        public GoreVala(SceneGame world) : base(world)
        {
            Name = "Gore Vala";
            Description = "Toothy salmon with anger issues";

            Render = new CreatureDirectionalRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/fish")
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 80));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectFamily(this, Family.Fish));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDive());
        }

        [Construct("gore_vala")]
        public static GoreVala Construct(Context context)
        {
            return new GoreVala(context.World);
        }
    }

    class Vorrax : Enemy
    {
        public Vorrax(SceneGame world) : base(world)
        {
            Name = "Vorrax";
            Description = "Hungry hungry sea demon";

            Render = new CreatureDirectionalRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/fish"),
                Color = ColorMatrix.TwoColorLight(Color.Black, new Color(255, 160, 64))
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 30));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 80));

            Effect.ApplyInnate(new EffectFamily(this, Family.Fish));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDive());
        }

        [Construct("vorrax")]
        public static Vorrax Construct(Context context)
        {
            return new Vorrax(context.World);
        }
    }

    class Ctholoid : Enemy
    {
        public Ctholoid(SceneGame world) : base(world)
        {
            Name = "Cthuloid";
            Description = "Inhabitant of the dark underground caverns";

            Render = new CreatureDirectionalRender()
            {
                Sprite = SpriteLoader.Instance.AddSprite("content/fish"),
                Color = ColorMatrix.TwoColor(Color.Black, Color.LightSeaGreen)
            };
            Mask.Add(Point.Zero);

            Effect.ApplyInnate(new EffectStat(this, Stat.HP, 100));
            Effect.ApplyInnate(new EffectStat(this, Stat.Attack, 10));

            Effect.ApplyInnate(new EffectFamily(this, Family.Fish));

            Skills.Add(new SkillAttack());
            Skills.Add(new SkillDive());
            Skills.Add(new SkillChaosJaunt());
        }

        [Construct("cthuloid")]
        public static Ctholoid Construct(Context context)
        {
            return new Ctholoid(context.World);
        }
    }
}
