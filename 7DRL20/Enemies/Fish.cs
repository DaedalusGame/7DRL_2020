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

            Render = new CreatureFishRender(ColorMatrix.Identity);
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 80));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Effect.Apply(new EffectFamily(this, Family.Fish));

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

            Render = new CreatureFishRender(ColorMatrix.TwoColorLight(Color.Black, new Color(255, 160, 64)));
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 30));
            Effect.Apply(new EffectStat(this, Stat.Attack, 80));

            Effect.Apply(new EffectFamily(this, Family.Fish));

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

            Render = new CreatureFishRender(ColorMatrix.TwoColor(Color.Black, Color.LightSeaGreen));
            Mask.Add(Point.Zero);

            Effect.Apply(new EffectStat(this, Stat.HP, 100));
            Effect.Apply(new EffectStat(this, Stat.Attack, 10));

            Effect.Apply(new EffectFamily(this, Family.Fish));

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
