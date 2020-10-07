using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class SkillImpact : SkillProjectileBase
    {
        int MaxDistance = 10;

        public SkillImpact() : base("Impact", "Ranged Bludgeon damage.", 1, 0, float.PositiveInfinity)
        {
            SkillType = SkillType.Spell;
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InLineOfSight(user, user.AggroTarget, MaxDistance, 0);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Facing facing)
            {
                Point velocity = facing.ToOffset();
                Consume();
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 40);
                yield return user.WaitSome(20);
                yield return Scheduler.Instance.RunAndWait(Shoot(user, user.Tile, velocity));
            }
        }

        private IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletTrail(user.World, SpriteLoader.Instance.AddSprite("content/bullet_wave2"), Vector2.Zero, ColorMatrix.TwoColor(Color.Blue, Color.PaleTurquoise), Color.DarkBlue, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            attack.ExtraEffects.Add(new AttackSkill(this));
            return attack;
        }
    }
}
