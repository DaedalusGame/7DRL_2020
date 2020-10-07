using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class SkillRaisePeatMummy : Skill
    {
        public SkillRaisePeatMummy() : base("Raise Peat Mummy", "Raise a Peat Mummy on a random Bog tile within 4 tiles radius of the caster.", 5, 20, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8) && GetValidTargets(user).Any();
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        private IEnumerable<Tile> GetValidTargets(Creature user)
        {
            IEnumerable<Tile> tiles = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 4);
            return tiles.Where(tile => tile is Bog && !tile.Creatures.Any());
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            ShowSkill(user);
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            var targetTile = GetValidTargets(user).Shuffle(Random).First();
            var peatMummy = new PeatMummy(targetTile.World);
            peatMummy.MoveTo(targetTile, 0);
            peatMummy.AddControlTurn();
            yield return user.WaitSome(20);
        }
    }

}
