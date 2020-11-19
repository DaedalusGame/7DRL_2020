using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
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

    class SkillCreateTentacles : Skill
    {
        public SkillCreateTentacles() : base("Create Tentacles", "Creates tentacles in all surrounding tiles.", 0, 20, float.PositiveInfinity)
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
            ShowSkill(user);
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            var targetTiles = SkillUtil.GetFrontierTiles(user).Shuffle(Random);
            int maxCount = targetTiles.Count();
            var currentCount = user.GetEffects<EffectSummon>().Count(x => x.Slave is AbyssalTendril);
            if(currentCount < maxCount)
            {
                foreach(var targetTile in targetTiles)
                {
                    if (!targetTile.Solid && !targetTile.Creatures.Any() && currentCount < maxCount)
                    {
                        var tentacle = new AbyssalTendril(targetTile.World);
                        tentacle.MoveTo(targetTile, 0);
                        tentacle.AddControlTurn();
                        tentacle.VisualPose = tentacle.FlickPose(CreaturePose.Walk, CreaturePose.Stand, 5);
                        Effect.Apply(new EffectSummon(user, tentacle));
                        currentCount++;
                        yield return user.WaitSome(10);
                    }
                }
            }
            yield return user.WaitSome(20);
        }
    }

    class SkillDeployBomb : Skill
    {
        int Count = 2;

        public SkillDeployBomb() : base("Deploy Bomb", "Creates bombs in adjacent tiles.", 0, 20, float.PositiveInfinity)
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
            ShowSkill(user);
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            //var userTiles = user.Mask.Select(o => user.Tile.GetNeighbor(o.X, o.Y)).ToList();
            var targetTiles = SkillUtil.GetFrontierTiles(user).Shuffle(Random);
            int currentCount = 0;
            foreach (var targetTile in targetTiles)
            {
                if (!targetTile.Solid && !targetTile.Creatures.Any() && currentCount < Count)
                {
                    //var userTile = userTiles.Pick(Random);
                    var bomb = new AutoBomb(targetTile.World);
                    bomb.MoveTo(targetTile, 0);
                    bomb.VisualPosition = bomb.SlideJump(user.VisualTarget - bomb.CenterOffset, bomb.ActualPosition, 10, LerpHelper.Quadratic, 10);
                    bomb.AddControlTurn();
                    Effect.Apply(new EffectSummon(user, bomb));
                    currentCount++;
                    yield return user.WaitSome(3);
                }
            }
            yield return user.WaitSome(20);
        }
    }
}
