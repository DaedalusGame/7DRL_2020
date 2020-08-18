using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class Projectile
    {
        public delegate IEnumerable<Wait> TrailDelegate(Creature user, Tile tile);
        public delegate bool CanCollideDelegate(Creature user, Tile tile);
        public delegate IEnumerable<Wait> ImpactDelegate(Creature user, Tile tile);

        Bullet Bullet;
        TrailDelegate Trail;
        CanCollideDelegate CanCollide;
        ImpactDelegate Impact;

        public Projectile(Bullet bullet, TrailDelegate trail, CanCollideDelegate canCollide, ImpactDelegate impact)
        {
            Bullet = bullet;
            Trail = trail;
            CanCollide = canCollide;
            Impact = impact;
        }

        public IEnumerable<Wait> ShootStraight(Creature user, Tile tile, Point velocity, int time, int maxDistance)
        {
            Bullet?.Setup(tile.VisualTarget, time * maxDistance);
            bool impacted = false;
            List<Wait> waits = new List<Wait>();
            for (int i = 0; i < maxDistance && !impacted; i++)
            {
                Tile nextTile = tile.GetNeighbor(velocity.X, velocity.Y);
                impacted = CanCollide(user, nextTile);
                Bullet?.Move(nextTile.VisualTarget, time);
                if (impacted)
                {
                    if (time > 0)
                        yield return user.WaitSome(time / 2);
                    Bullet?.Destroy();
                    waits.Add(Scheduler.Instance.RunAndWait(Impact(user, nextTile)));
                }
                else
                {
                    if (time > 0)
                        yield return user.WaitSome(time);
                    waits.Add(Scheduler.Instance.RunAndWait(Trail(user, nextTile)));
                }
                tile = nextTile;
            }
            yield return new WaitAll(waits);
        }

        public static IEnumerable<Wait> NoTrail(Creature user, Tile tile)
        {
            return Enumerable.Empty<Wait>();
        }

        public static bool CollideSolid(Creature user, Tile tile)
        {
            return tile.Solid || tile.Creatures.Any(x => x != user && x.CurrentHP > 0);
        }
    }
}
