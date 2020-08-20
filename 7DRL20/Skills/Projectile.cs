using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    abstract class ProjectileSpecial
    {
        public virtual bool HasCollision => false; //TODO: roll into an enum (nocollide, impact, fizzle, pierce)

        public virtual Wait Trail(Projectile projectile, Tile tile)
        {
            return Wait.NoWait;
        }
        public virtual Wait Impact(Projectile projectile, Tile tile)
        {
            return Wait.NoWait;
        }
        public virtual Wait Fizzle(Projectile projectile, Tile tile)
        {
            return Wait.NoWait;
        }

        public virtual bool CanCollide(Projectile projectile, Tile tile)
        {
            return false;
        }
    }

    class ProjectileImpactAttack : ProjectileSpecial
    {
        Func<Creature, IEffectHolder, Attack> AttackGenerator;

        public ProjectileImpactAttack(Func<Creature, IEffectHolder, Attack> attackGenerator)
        {
            AttackGenerator = attackGenerator;
        }

        public override Wait Impact(Projectile projectile, Tile tile)
        {
            Point velocity = projectile.Shooter.Facing.ToOffset();
            List<Wait> waits = new List<Wait>();
            foreach (Creature creature in tile.Creatures)
            {
                projectile.Shooter.Attack(creature, new Vector2(velocity.X, velocity.Y), AttackGenerator);
                waits.Add(creature.CurrentAction);
            }
            return new WaitAll(waits);
        }
    }

    class ProjectileImpactFunction : ProjectileSpecial
    {
        ImpactDelegate Function;

        public ProjectileImpactFunction(ImpactDelegate function)
        {
            Function = function;
        }

        public override Wait Impact(Projectile projectile, Tile tile)
        {
            return Scheduler.Instance.RunAndWait(Function(projectile, tile));
        }
    }

    class ProjectileTrailFunction : ProjectileSpecial
    {
        TrailDelegate Function;

        public ProjectileTrailFunction(TrailDelegate function)
        {
            Function = function;
        }

        public override Wait Trail(Projectile projectile, Tile tile)
        {
            return Scheduler.Instance.RunAndWait(Function(projectile, tile));
        }
    }

    class ProjectileCollideSolid : ProjectileSpecial
    {
        public override bool HasCollision => true;

        public override bool CanCollide(Projectile projectile, Tile tile)
        {
            return Projectile.CollideSolid(projectile, tile);
        }
    }

    class ProjectileCollideFunction : ProjectileSpecial
    {
        public override bool HasCollision => true;

        CanCollideDelegate Function;

        public ProjectileCollideFunction(CanCollideDelegate function)
        {
            Function = function;
        }

        public override bool CanCollide(Projectile projectile, Tile tile)
        {
            return Function(projectile, tile);
        }
    }

    delegate IEnumerable<Wait> TrailDelegate(Projectile projectile, Tile tile);
    delegate bool CanCollideDelegate(Projectile projectile, Tile tile);
    delegate IEnumerable<Wait> ImpactDelegate(Projectile projectile, Tile tile);

    class Projectile
    {
        public Bullet Bullet;

        public List<ProjectileSpecial> ExtraEffects = new List<ProjectileSpecial>();

        public Creature Shooter;
        public Tile Tile;

        public Projectile(Bullet bullet)
        {
            Bullet = bullet;
        }

        public IEnumerable<Wait> ShootStraight(Creature user, Tile tile, Point velocity, int time, int maxDistance)
        {
            Shooter = user;
            Tile = tile;
            Bullet?.Setup(Tile.VisualTarget, time * maxDistance);
            bool impacted = false;
            List<Wait> waits = new List<Wait>();
            for (int i = 0; i < maxDistance && !impacted; i++)
            {
                Tile nextTile = Tile.GetNeighbor(velocity.X, velocity.Y);
                impacted = CanCollide(nextTile);
                Bullet?.Move(nextTile.VisualTarget, time);
                if (impacted)
                {
                    if (time > 0)
                        yield return user.WaitSome(time / 2);
                    Bullet?.Destroy();
                    waits.Add(Scheduler.Instance.RunAndWait(Impact(nextTile)));
                }
                else
                {
                    if (time > 0)
                        yield return user.WaitSome(time);
                    waits.Add(Scheduler.Instance.RunAndWait(Trail(nextTile)));
                }
                Tile = nextTile;
            }
            yield return new WaitAll(waits);
        }

        private bool CanCollide(Tile tile)
        {
            return ExtraEffects.Any(x => x.HasCollision && x.CanCollide(this, tile));
        }

        private IEnumerable<Wait> Impact(Tile tile)
        {
            foreach (var effect in ExtraEffects)
            {
                yield return effect.Impact(this, tile);
            }
        }

        private IEnumerable<Wait> Fizzle(Tile tile)
        {
            foreach (var effect in ExtraEffects)
            {
                yield return effect.Fizzle(this, tile);
            }
        }

        private IEnumerable<Wait> Trail(Tile tile)
        {
            foreach (var effect in ExtraEffects)
            {
                yield return effect.Trail(this, tile);
            }
        }

        public static bool CollideSolid(Projectile projectile, Tile tile)
        {
            return tile.Solid || tile.Creatures.Any(x => x != projectile.Shooter && x.CurrentHP > 0);
        }
    }
}
