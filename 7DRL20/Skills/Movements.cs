using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    abstract class SkillJumpBase : Skill
    {
        public override bool Hidden(Creature user) => true;

        public struct TileDirection
        {
            public Tile Tile;
            public Facing Facing;

            public TileDirection(Tile tile, Facing facing)
            {
                Tile = tile;
                Facing = facing;
            }
        }

        public SkillJumpBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && GetPossibleTiles(user, user.AggroTarget).Any();
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                Consume();
                yield return user.WaitSome(20);
                var tiles = GetPossibleTiles(user, targetCreature);
                if (tiles.Any())
                {
                    TileDirection tile = tiles.First();
                    Vector2 startJump = user.VisualPosition();
                    user.MoveTo(tile.Tile, 20);
                    user.Facing = tile.Facing;
                    user.VisualPosition = user.SlideJump(startJump, new Vector2(user.X, user.Y) * 16, 16, LerpHelper.Linear, 20);
                    yield return user.WaitSome(20);
                    Land(user);
                }
                yield return user.WaitSome(20);
            }
        }

        protected abstract void Land(Creature user);

        protected abstract IEnumerable<TileDirection> GetPossibleTiles(Creature user, Creature target);

        protected bool CanLand(Creature creature, Tile tile)
        {
            foreach (var point in creature.Mask)
            {
                Tile neighbor = tile.GetNeighbor(point.X, point.Y);
                if (neighbor.Solid || neighbor.Creatures.Any())
                    return false;
            }
            return true;
        }

        protected IEnumerable<TileDirection> GetAlignedTiles(Tile targetTile, Rectangle userRectangle, Rectangle targetRectangle, int dist)
        {
            int yTop = targetRectangle.Top + 1 - userRectangle.Height + -userRectangle.Y;
            int yBottom = targetRectangle.Bottom - 1 + -userRectangle.Y;
            int xLeft = targetRectangle.Left + 1 - userRectangle.Width + -userRectangle.X;
            int xRight = targetRectangle.Right - 1 + -userRectangle.X;

            List<TileDirection> tiles = new List<TileDirection>();

            for (int y = yTop; y <= yBottom; y++)
            {
                for (int x = 0; x < dist; x++)
                {
                    int left = xLeft - userRectangle.Width - x;
                    int right = xRight + userRectangle.Width + x;

                    tiles.Add(new TileDirection(targetTile.GetNeighbor(left, y), Facing.East));
                    tiles.Add(new TileDirection(targetTile.GetNeighbor(right, y), Facing.West));
                }
            }

            for (int x = yTop; x <= yBottom; x++)
            {
                for (int y = 0; y < dist; y++)
                {
                    int top = yTop - userRectangle.Height - y;
                    int bottom = yBottom + userRectangle.Height + y;

                    tiles.Add(new TileDirection(targetTile.GetNeighbor(x, top), Facing.South));
                    tiles.Add(new TileDirection(targetTile.GetNeighbor(x, bottom), Facing.North));
                }
            }

            return tiles;
        }
    }

    class SkillSideJump : SkillJumpBase
    {
        int Distance;
        int JumpDistance;

        public SkillSideJump(int distance, int jumpDistance) : base("Side Step", "Move to tile aligned with enemy", 2, 3, float.PositiveInfinity)
        {
            Distance = distance;
            JumpDistance = jumpDistance;
        }

        protected override IEnumerable<TileDirection> GetPossibleTiles(Creature user, Creature target)
        {
            if (target == null || target.Tile == null)
                return Enumerable.Empty<TileDirection>();

            Rectangle userRectangle = user.Mask.GetRectangle();
            Rectangle targetRectangle = target.Mask.GetRectangle();

            Facing left = user.Facing.TurnLeft();
            Facing right = user.Facing.TurnRight();

            return GetAlignedTiles(target.Tile, userRectangle, targetRectangle, Distance)
                .Where(tile => tile.Facing == left || tile.Facing == right)
                .Where(tile => GetSquareDistance(tile.Tile, user.Tile) < JumpDistance * JumpDistance)
                .Where(tile => CanLand(user, tile.Tile))
                .Shuffle();
        }

        protected override void Land(Creature user)
        {
            new ScreenShakeRandom(user.World, 6, 30, LerpHelper.Linear);
        }
    }

    class SkillDive : Skill
    {
        public override bool Hidden(Creature user) => true;
        public override bool WaitUse => true;

        public SkillDive() : base("Dive", "Move to tile nearby", 2, 3, float.PositiveInfinity)
        {
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && user.Tile is Water;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            yield return user.CurrentAction;
            Consume();
            Vector2 pos = new Vector2(user.X * 16, user.Y * 16);
            new WaterSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 0, 12);
            user.VisualColor = user.Static(Color.Transparent);
            var nearbyTiles = user.Tile.GetNearby(4).Where(tile => !tile.Solid && tile is Water && !tile.Creatures.Any()).ToList();
            user.MoveTo(nearbyTiles.Pick(Random), 0);
            yield return user.WaitSome(5);
            new WaterSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 0, 12);
            user.VisualColor = user.Static(Color.White);
        }
    }

    class SkillWarp : Skill
    {
        public override bool Hidden(Creature user) => true;
        public override bool WaitUse => true;

        public SkillWarp() : base("Warp", "Move to chase enemy", 0, 2, float.PositiveInfinity)
        {
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && !InRange(user, user.AggroTarget, 4);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                yield return user.CurrentAction;
                Consume();
                user.VisualColor = user.Flick(user.Flash(user.Static(Color.Transparent), user.Static(Color.White), 2, 2), user.Static(Color.White), 20);
                yield return user.WaitSome(20);
                user.VisualColor = user.Static(Color.Transparent);
                var nearbyTiles = targetCreature.Tile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).ToList();
                user.MoveTo(nearbyTiles.Pick(Random), 0);
                yield return user.WaitSome(20);
                user.VisualColor = user.Flick(user.Flash(user.Static(Color.Transparent), user.Static(Color.White), 2, 2), user.Static(Color.White), 20);
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillChaosJaunt : Skill
    {
        public override bool Hidden(Creature user) => true;
        public override bool WaitUse => true;

        public SkillChaosJaunt() : base("Chaos Jaunt", "Move to chase enemy, deal chaos damage to surrounding tiles.", 0, 2, float.PositiveInfinity)
        {
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && !InRange(user, user.AggroTarget, 4);
        }

        private void EmitFlare(Creature creature, int n)
        {
            SpriteReference sprite = SpriteLoader.Instance.AddSprite("content/cinder");
            for (int i = 0; i < n; i++)
            {
                Vector2 emitPos = new Vector2(creature.X * 16, creature.Y * 16) + creature.Mask.GetRandomPixel(Random);
                Vector2 centerPos = creature.VisualTarget;
                Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f) * 1;
                new Cinder(creature.World, sprite, emitPos, velocity, (int)Math.Min(Random.Next(90) + 90, 50));
            }
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                yield return user.CurrentAction;
                Consume();
                new ChaosSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 0, 15);
                user.VisualColor = user.Static(Color.Transparent);
                EmitFlare(user, 10);
                var nearbyTiles = targetCreature.Tile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).ToList();
                user.MoveTo(nearbyTiles.Pick(Random), 0);
                EmitFlare(user, 10);
                user.VisualColor = user.Flick(user.Flash(user.Static(Color.Transparent), user.Static(ColorMatrix.Chaos()), 1, 1), user.Static(Color.White), 10);
                var impact = user.Tile.GetAllNeighbors();
                SpriteReference chaosBall = SpriteLoader.Instance.AddSprite("content/projectile_chaos");
                List<Creature> targets = new List<Creature>();
                List<Tile> targetTiles = new List<Tile>();
                int emitTime = 15;
                int ballTime = 5;
                foreach (var tile in impact)
                {
                    if (tile.Creatures.Any())
                    {
                        new ProjectileEmitter(user.World, () => user.VisualTarget, () => tile.VisualTarget, emitTime, (start, end) => new Ball(user.World, chaosBall, start, end, LerpHelper.Linear, ballTime));
                        targets.AddRange(tile.Creatures);
                        targetTiles.Add(tile);
                    }
                }
                if (targets.Any())
                    yield return user.WaitSome(emitTime + ballTime);
                foreach (var blastTarget in targets)
                {
                    user.Attack(blastTarget, 0, 0, ExplosionAttack);
                    EmitFlare(blastTarget, 10);
                }
                foreach (var targetTile in targetTiles)
                {
                    new FireExplosion(user.World, targetTile.VisualTarget, Vector2.Zero, 0, 12);
                }
                if (targets.Any())
                {
                    new ScreenShakeRandom(user.World, 5, 10, LerpHelper.Linear);
                    yield return user.WaitSome(20);
                }
            }
        }

        protected Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Chaos, 1.0);
            return attack;
        }
    }
}
