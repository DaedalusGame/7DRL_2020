using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class SkillForcefield : Skill
    {
        public SkillForcefield() : base("Forcefield", "Select 1 random base Element. Become weak to this element. Become immune to all the others.", 0, 0, 1)
        {
            Priority = 10;
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            var weakElement = Element.MagicalElements.Pick(Random);
            user.AddStatusEffect(new Forcefield()
            {
                Buildup = 1,
            }.SetElement(weakElement));
            yield return user.WaitSome(20);
        }
    }

    class SkillAgeOfDragons : Skill
    {
        public SkillAgeOfDragons() : base("Age of Dragons", "10 Extra Turns.", 15, 15, float.PositiveInfinity)
        {
            Priority = 10;
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            throw new NotImplementedException();
        }
    }

    class SkillOilBlast : Skill
    {
        public SkillOilBlast() : base("Oil Blast", "All nearby creatures are Oiled.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            SpriteReference blob = SpriteLoader.Instance.AddSprite("content/blob");
            for (int i = 0; i < 70; i++)
            {
                new Blob(user.World, blob, user.VisualTarget, Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * MathHelper.Lerp(1, 2, Random.NextFloat()), MathHelper.Lerp(0.2f, 1, Random.NextFloat()), Color.Black, Random.Next(24, 72));
            }
            foreach (var targetCreature in SkillUtil.GetCircularArea(user, 2).SelectMany(tile => tile.Creatures).Distinct())
            {
                targetCreature.AddStatusEffect(new Oiled()
                {
                    Duration = new Slider(30),
                    Buildup = 1,
                });
            }
            yield return user.WaitSome(20);
        }
    }

    class SkillMagicPower : Skill
    {
        public SkillMagicPower() : base("Magic Power", "All spells deal double damage.", 3, 20, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            user.AddStatusEffect(new MagicPower()
            {
                Duration = new Slider(15),
                Buildup = 1,
            });
            yield return user.WaitSome(20);
        }
    }
}
