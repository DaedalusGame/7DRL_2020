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
    abstract class SkillAttackBase : Skill
    {
        public override bool Hidden(Creature user) => true;

        public SkillAttackBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InMeleeRange(user, user.IsHostile);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.Facing;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Facing facing)
            {
                Consume();
                var offset = facing.ToOffset();
                return user.RoutineAttack(offset.X, offset.Y, Attack);
            }
            return Enumerable.Empty<Wait>();
        }

        protected abstract Attack Attack(Creature attacker, IEffectHolder defender);
    }

    class SkillAttack : SkillAttackBase
    {
        public SkillAttack() : base("Attack", "Physical Attack", 0, 0, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            return Creature.MeleeAttack(attacker, defender);
        }
    }

    class SkillMudTouch : SkillAttackBase
    {
        public SkillMudTouch() : base("Mud Touch", "Attack", 0, 0, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.Elements.Add(Element.Mud, 0.5);
            return attack;
        }
    }

    class SkillDrainTouch : SkillAttackBase
    {
        public SkillDrainTouch() : base("Drain Touch", "Attack", 0, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.ExtraEffects.Add(new AttackDrain(0.6));
            attack.Elements.Add(Element.Pierce, 2.0);
            return attack;
        }
    }

    class SkillDrainTouch2 : SkillDrainTouch
    {
        public SkillDrainTouch2()
        {
            InstantUses = new Slider(2);
        }
    }

    class SkillLightningClaw : SkillAttackBase
    {
        public SkillLightningClaw() : base("Lightning Claw", "Attack", 0, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Slash, 0.5);
            attack.Elements.Add(Element.Thunder, 0.5);
            return attack;
        }
    }

    class SkillHagsKnife : SkillAttackBase
    {
        public SkillHagsKnife() : base("Hag's Knife", "Attack", 0, 0, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.ExtraEffects.Add(new AttackEndFunction(RoutineHagKnife));
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }

        private IEnumerable<Wait> RoutineHagKnife(Attack attack)
        {
            if(attack.FinalDamage.GetOrDefault(Element.Slash, 0) > 0)
            {
                attack.Attacker.AddStatusEffect(new HagsFlesh()
                {
                    Buildup = 1,
                });
            }
            yield return Wait.NoWait;
        }
    }

    class SkillRendingClaw : SkillAttackBase
    {
        public SkillRendingClaw() : base("Rending Claw", "Attack", 0, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Slash, 1.0);
            //TODO: Durability damage
            return attack;
        }
    }

    class SkillViperBite : SkillAttackBase
    {
        public SkillViperBite() : base("Viper Bite", "Attack", 0, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Slash, 0.5);
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.StatusEffects.Add(new Poison()
            {
                Buildup = 1.0,
                Duration = new Slider(40),
            });
            return attack;
        }
    }

    class SkillTentacleSlash : SkillAttackBase
    {
        public SkillTentacleSlash() : base("Tentacle Slash", "Attack", 0, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }
    }

    class SkillTentacleBash : SkillAttackBase
    {
        public SkillTentacleBash() : base("Tentacle Bash", "Attack", 0, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    class SkillAcidTouch : SkillAttackBase
    {
        public SkillAcidTouch() : base("Acid Touch", "Attack", 0, 1, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.StatusEffects.Add(new DefenseDown()
            {
                Buildup = 0.2,
                Duration = new Slider(10)
            });
            return attack;
        }
    }

    class SkillPoisonTouch : SkillAttackBase
    {
        public SkillPoisonTouch() : base("Poison Touch", "Attack", 0, 1, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.StatusEffects.Add(new Poison()
            {
                Buildup = 0.4,
                Duration = new Slider(15)
            });
            return attack;
        }
    }

    class SkillSlimeTouch : SkillAttackBase
    {
        public SkillSlimeTouch() : base("Slime Touch", "Attack", 0, 1, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 0.5);
            if(!defender.HasFamily(Family.Slime))
                attack.StatusEffects.Add(new Slimed()
                {
                    Buildup = 0.4,
                    Duration = new Slider(15)
                });
            return attack;
        }
    }

    abstract class SkillProjectileBase : Skill
    {
        public SkillProjectileBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.Facing;
        }
    }

    class SkillBloodSword : SkillProjectileBase
    {
        int MaxDistance = 10;

        public SkillBloodSword() : base("Blood Sword", "Drain HP", 1, 3, float.PositiveInfinity)
        {
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
                yield return user.WaitSome(20);
                var pos = new Vector2(user.X * 16, user.Y * 16);
                user.VisualPosition = user.Slide(pos + new Vector2(velocity.X * 8, velocity.Y * 8), pos, LerpHelper.Linear, 10);
                user.VisualPose = user.FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
                yield return Scheduler.Instance.RunAndWait(Shoot(user, user.Tile, velocity));
            }
        }

        private IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletTrail(user.World, SpriteLoader.Instance.AddSprite("content/bullet_sword"), Vector2.Zero, ColorMatrix.TwoColor(new Color(129, 166, 0), new Color(237, 255, 106)), Color.Red, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackDrain(1.0));
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }
    }

    abstract class SkillVolleyBase : SkillProjectileBase
    {
        protected int MaxDistance = 10;
        protected int VolleysMin = 1;
        protected int VolleysMax = 1;

        public SkillVolleyBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
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
                yield return user.WaitSome(20);
                var pos = new Vector2(user.X * 16, user.Y * 16);
                var waits = new List<Wait>();
                int volleys = Random.Next(VolleysMin, VolleysMax + 1);
                for (int i = 0; i < volleys; i++)
                {
                    user.VisualPosition = user.Slide(pos + new Vector2(velocity.X * 8, velocity.Y * 8), pos, LerpHelper.Linear, 10);
                    user.VisualPose = user.FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
                    waits.Add(Scheduler.Instance.RunAndWait(Shoot(user, user.Tile, velocity)));
                    yield return new WaitTime(10);
                }
                yield return new WaitAll(waits);
            }
        }

        protected abstract IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity);
    }

    class SkillPetrolVolley : SkillVolleyBase
    {
        public SkillPetrolVolley() : base("Petrol Volley", "Inflicts Oiled", 1, 3, float.PositiveInfinity)
        {
        }

        protected override IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletNormal(user.World, SpriteLoader.Instance.AddSprite("content/bullet_oil"), Vector2.Zero, ColorMatrix.Identity, 0, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            attack.StatusEffects.Add(new Oiled()
            {
                Buildup = 1.0,
                Duration = new Slider(20),
            });
            return attack;
        }
    }

    class SkillSaltVolley : SkillVolleyBase
    {
        public SkillSaltVolley() : base("Salt Blast Volley", "", 1, 3, float.PositiveInfinity)
        {
            VolleysMin = 3;
            VolleysMax = 6;
        }

        protected override IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletRandom(user.World, SpriteLoader.Instance.AddSprite("content/bullet_salt"), Vector2.Zero, ColorMatrix.Identity, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    class SkillPoisonVolley : SkillVolleyBase
    {
        public SkillPoisonVolley() : base("Poison Spit Volley", "", 1, 3, float.PositiveInfinity)
        {
            VolleysMin = 1;
            VolleysMax = 3;
        }

        protected override IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletNormal(user.World, SpriteLoader.Instance.AddSprite("content/bullet_poison"), Vector2.Zero, ColorMatrix.Identity, 0, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            attack.StatusEffects.Add(new Poison()
            {
                Duration = new Slider(15),
                Buildup = 0.4,
            });
            return attack;
        }
    }

    class SkillAcidVolley : SkillVolleyBase
    {
        public SkillAcidVolley() : base("Acid Arrow Volley", "", 1, 3, float.PositiveInfinity)
        {
            VolleysMin = 2;
            VolleysMax = 7;
        }

        protected override IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletAngular(user.World, SpriteLoader.Instance.AddSprite("content/bullet_acid_bolt"), Vector2.Zero, ColorMatrix.Identity, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Pierce, 1.0);
            attack.StatusEffects.Add(new DefenseDown()
            {
                Duration = new Slider(15),
                Buildup = 0.3,
            });
            return attack;
        }
    }

    class SkillBoneVolley : SkillVolleyBase
    {
        public SkillBoneVolley() : base("Bone Dart Volley", "", 1, 3, float.PositiveInfinity)
        {
            VolleysMin = 3;
            VolleysMax = 5;
        }

        protected override IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletAngular(user.World, SpriteLoader.Instance.AddSprite("content/bullet_bone_dart"), Vector2.Zero, ColorMatrix.Identity, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.Elements.Add(Element.Dark, 0.5);
            return attack;
        }
    }

    class SkillChainLightningVolley : SkillVolleyBase
    {
        public SkillChainLightningVolley() : base("Chain Lightning", "", 1, 3, float.PositiveInfinity)
        {
        }

        protected override IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletNormal(user.World, SpriteLoader.Instance.AddSprite("content/bullet_lightning"), Vector2.Zero, ColorMatrix.Identity, 0.5f, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactFunction(Impact));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
            //new Color(225, 174, 210)
        }

        private IEnumerable<Wait> Impact(Projectile projectile, Tile tile)
        {
            Creature targetCreature = tile.Creatures.FirstOrDefault();
            if (targetCreature != null)
            {
                CascadeHelper cascade = new CascadeHelper(CascadeNearby, Arc, 3)
                {
                    ArcDelay = 1,
                };
                cascade.Hits.Add(projectile.Shooter, 10000);
                yield return Scheduler.Instance.RunAndWait(cascade.Start(targetCreature));
                var waits = new List<Wait>();
                foreach (var hit in cascade.Hits)
                {
                    if (hit.Key == projectile.Shooter) //Don't damage the originator
                        continue;
                    var wait = projectile.Shooter.Attack(hit.Key, Vector2.Zero, (a,b) => ThunderAttack(a, b, hit.Value));
                    waits.Add(wait);
                }
            }
        }

        private IEnumerable<Creature> CascadeNearby(Creature creature, CascadeHelper helper)
        {
            return CascadeHelper.GetNearbyCircular(creature, helper, 3, 2);
        }

        private IEnumerable<Wait> Arc(Creature start, Creature end)
        {
            //new Lightning(start.World, start.VisualTarget, end.VisualTarget, 10, 10);
            //new LightningSpark(start.World, SpriteLoader.Instance.AddSprite("content/lightning"), start.VisualTarget, end.VisualTarget, 3);
            new Beam(start.World, SpriteLoader.Instance.AddSprite("content/lightning"), start.VisualTarget, end.VisualTarget, 5, 10);
            yield return new WaitTime(5);
            //var wait = origin.Attack(end, SkillUtil.SafeNormalize(end.VisualTarget - start.VisualTarget), ThunderAttack);
            //yield return wait;
        }

        private Attack ThunderAttack(Creature attacker, IEffectHolder defender, int hits)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(hits * 10, 1, 1);
            attack.Elements.Add(Element.Thunder, 1.0);
            return attack;
        }
    }

    class SkillCannonShot : SkillProjectileBase
    {
        int MaxDistance = 8;

        public SkillCannonShot() : base("Cannon", $"Ranged {Element.Fire.FormatString} Attack", 2, 3, float.PositiveInfinity)
        {
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
                Vector2 pos = new Vector2(user.X * 16, user.Y * 16);
                ShowSkill(user);
                yield return user.WaitSome(50);
                user.VisualPosition = user.Slide(pos + new Vector2(velocity.X * -8, velocity.Y * -8), pos, LerpHelper.Linear, 10);
                yield return user.WaitSome(20);
                yield return Scheduler.Instance.RunAndWait(Shoot(user, user.Tile, velocity));
            }
        }

        private IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Projectile projectile = new Projectile(null);
            projectile.ExtraEffects.Add(new ProjectileImpactFunction(Impact));
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 0, MaxDistance);
            //new Color(225, 174, 210)
        }

        private IEnumerable<Wait> Impact(Projectile projectile, Tile tile)
        {
            new FireExplosion(tile.World, tile.VisualTarget, Vector2.Zero, 0, 15);
            return Enumerable.Empty<Wait>();
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Fire, 1.0);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    class SkillDeathSword : SkillProjectileBase
    {
        int MaxDistance = 10;

        public SkillDeathSword() : base("Death Blade", $"Wide-range {Element.Slash.FormatString} attack", 1, 3, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InLineOfSight(user, user.AggroTarget, MaxDistance, 1);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Facing facing)
            {
                Point velocity = facing.ToOffset();
                Consume();
                yield return user.WaitSome(20);
                var pos = new Vector2(user.X * 16, user.Y * 16);
                user.VisualPosition = user.Slide(pos + new Vector2(velocity.X * 8, velocity.Y * 8), pos, LerpHelper.Linear, 10);
                user.VisualPose = user.FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
                List<Wait> bulletWaits = new List<Wait>();
                Point sideOffset = user.Facing.TurnRight().ToOffset();
                for (int i = -1; i <= 1; i++)
                {
                    bulletWaits.Add(Scheduler.Instance.RunAndWait(Shoot(user, user.Tile.GetNeighbor(sideOffset.X * i, sideOffset.Y * i), velocity)));
                }
                yield return new WaitAll(bulletWaits);
            }
        }

        private IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletTrail(user.World, SpriteLoader.Instance.AddSprite("content/bullet_sword"), Vector2.Zero, ColorMatrix.Tint(new Color(225, 174, 210)), Color.Black, 0);
            Projectile projectile = new Projectile(bullet);
            projectile.ExtraEffects.Add(new ProjectileImpactAttack(BulletAttack));
            projectile.ExtraEffects.Add(new ProjectileCollideSolid());
            return projectile.ShootStraight(user, tile, velocity, 3, MaxDistance);
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }
    }


    abstract class SkillRamBase : Skill
    {
        protected int MaxDistance;
        protected int MaxCreatureHits;
        protected int MaxWallHits;
        protected int MaxTotalHits;
        protected bool DestroyWalls;
        protected bool CheckTarget = true;

        public SkillRamBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && user.AggroTarget != null && (!CheckTarget || InLineOfSight(user, user.AggroTarget, MaxDistance, 0));
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.Facing;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Facing facing)
            {
                Consume();
                var offset = facing.ToOffset();
                yield return user.WaitSome(20);
                Tile lastSafeTile = user.Tile;
                var frontier = user.Mask.GetFrontier(offset.X, offset.Y);
                int creatureHits = 0;
                int wallHits = 0;
                List<Wait> waitForDamage = new List<Wait>();
                for (int i = 0; i < MaxDistance; i++)
                {
                    if (!IsUnsafe(user))
                        lastSafeTile = user.Tile;
                    foreach (var tile in frontier.Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
                    {
                        foreach (var creature in tile.Creatures)
                        {
                            var wait = user.Attack(creature, new Vector2(offset.X, offset.Y), RamAttack);
                            waitForDamage.Add(wait);
                            creatureHits++;
                        }
                        if (tile.Solid)
                        {
                            if (DestroyWalls)
                                tile.MakeFloor();
                            wallHits++;
                        }
                    }
                    if (creatureHits > MaxCreatureHits || wallHits > MaxWallHits || creatureHits + wallHits > MaxTotalHits)
                        break;
                    new SeismArea(user.World, user.Tiles, 8);
                    user.ForceMove(offset.X, offset.Y, 3);
                    yield return user.WaitSome(3);
                }
                if (IsUnsafe(user))
                    user.MoveTo(lastSafeTile,10);
                yield return new WaitAll(waitForDamage);
            }
        }

        private static bool IsUnsafe(Creature user)
        {
            return user.Mask.Select(o => user.Tile.GetNeighbor(o.X, o.Y)).Any(front => front.Solid || front.Creatures.Any(creature => creature != user));
        }

        protected abstract Attack RamAttack(Creature attacker, IEffectHolder defender);
    }

    class SkillRam : SkillRamBase
    {
        public override bool Hidden(Creature user) => true;

        public SkillRam() : base("Attack", "Ram", 1, 1, float.PositiveInfinity)
        {

        }

        protected override Attack RamAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    abstract class SkillSpinBase : Skill
    {
        public int Rotations = 1;
        public int RotationTime = 3;

        public SkillSpinBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InFrontier(user, user.IsHostile);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();

            Facing visualFacing = user.Facing;
            user.VisualPose = user.Static(CreaturePose.Attack);
            user.VisualFacing = () => visualFacing;

            List<Wait> waits = new List<Wait>();
            HashSet<Creature> targets = new HashSet<Creature>();
            for (int i = 0; i < Rotations; i++)
            {
                CreateVisual(user);
                visualFacing = visualFacing.TurnLeft();
                yield return new WaitTime(RotationTime);
                visualFacing = visualFacing.TurnLeft();
                yield return new WaitTime(RotationTime);
                visualFacing = visualFacing.TurnLeft();
                yield return new WaitTime(RotationTime);
                visualFacing = visualFacing.TurnLeft();
                yield return new WaitTime(RotationTime);
                waits.Add(Scheduler.Instance.RunAndWait(RoutineDamage(user, targets, i)));
            }

            user.VisualPose = user.Static(CreaturePose.Stand);
            user.VisualFacing = () => user.Facing;

            yield return new WaitAll(waits);
        }

        public abstract IEnumerable<Wait> RoutineDamage(Creature user, HashSet<Creature> targets, int n);

        public IEnumerable<Wait> RoutineDamageSurrounding(Creature user, HashSet<Creature> targets, bool multihit, AttackDelegate attack)
        {
            List<Wait> waits = new List<Wait>();
            foreach (var tile in user.Mask.GetFullFrontier().Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
            {
                foreach (var target in tile.Creatures)
                {
                    if (multihit || !targets.Contains(target))
                    {
                        var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), attack);
                        waits.Add(wait);
                    }
                    targets.Add(target);
                }
            }
            yield return new WaitAll(waits);
        }

        public abstract void CreateVisual(Creature user);

        public void CreateWhirl(Creature user, float size, Color colorA, Color colorB)
        {
            int totaltime = RotationTime * 4;
            new Cutter(user.World, () => user.VisualTarget, -MathHelper.TwoPi / totaltime, -(MathHelper.TwoPi / totaltime) * 0.5f, LerpHelper.Linear, size, Random.NextFloat() * MathHelper.TwoPi, colorA, totaltime + Random.Next(4));
            new Cutter(user.World, () => user.VisualTarget, -MathHelper.TwoPi / totaltime, -(MathHelper.TwoPi / totaltime) * 0.5f, LerpHelper.Linear, size * 0.80f, Random.NextFloat() * MathHelper.TwoPi, Color.Lerp(colorA, colorB, 0.5f), totaltime + Random.Next(4));
            new Cutter(user.World, () => user.VisualTarget, -MathHelper.TwoPi / totaltime, -(MathHelper.TwoPi / totaltime) * 0.5f, LerpHelper.Linear, size * 0.60f, Random.NextFloat() * MathHelper.TwoPi, colorB, totaltime + Random.Next(4));
        }
    }

    class SkillSpinSlash : SkillSpinBase
    {
        public SkillSpinSlash() : base("Spin Slash", "Attacks all surrounding enemies.", 0, 0, float.PositiveInfinity)
        {
            Rotations = 1;
            RotationTime = 3;
        }

        public override void CreateVisual(Creature user)
        {
            CreateWhirl(user, 1.0f, Color.White, Color.Gray);
        }

        public override IEnumerable<Wait> RoutineDamage(Creature user, HashSet<Creature> targets, int n)
        {
            return RoutineDamageSurrounding(user, targets, false, SpinAttack);
        }

        protected Attack SpinAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }
    }

    class SkillWildSpin : SkillSpinBase
    {
        public SkillWildSpin() : base("Wild Spin", "Attacks all surrounding enemies two times.", 0, 5, float.PositiveInfinity)
        {
            Rotations = 2;
            RotationTime = 4;
        }

        public override void CreateVisual(Creature user)
        {
            CreateWhirl(user, 1.0f, Color.White, Color.Gray);
        }

        public override IEnumerable<Wait> RoutineDamage(Creature user, HashSet<Creature> targets, int n)
        {
            return RoutineDamageSurrounding(user, targets, true, SpinAttack);
        }

        protected Attack SpinAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    abstract class SkillStompBase : Skill
    {
        public SkillStompBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
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
            user.VisualPosition = user.SlideJump(user.VisualPosition(), new Vector2(user.X, user.Y) * 16, 16, LerpHelper.Linear, 20);
            yield return user.WaitSome(20);
            yield return Scheduler.Instance.RunAndWait(RoutineImpact(user));
        }

        public abstract IEnumerable<Wait> RoutineImpact(Creature user);
    }

    class SkillPuddleStomp : SkillStompBase
    {
        struct AttackTile
        {
            public Tile Tile;
            public AttackDelegate Attack;

            public AttackTile(Tile tile, AttackDelegate attack)
            {
                Tile = tile;
                Attack = attack;
            }
        }

        public SkillPuddleStomp() : base("Puddle Stomp", "Stomp ground and affect nearby enemies with ground effects.", 3, 3, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InFrontier(user, user.IsHostile) && user.Tiles.Any(tile => SkillUtil.GetTerrainAttack(user,tile) != null);
        }

        public override IEnumerable<Wait> RoutineImpact(Creature user)
        {
            new ScreenShakeRandom(user.World, 6, 30, LerpHelper.Linear);
            new SeismArea(user.World, user.Tiles, 10);
            HashSet<Tile> affectedTiles = new HashSet<Tile>();
            List<AttackTile> attackTiles = new List<AttackTile>();
            foreach(var tile in user.Tiles)
            {
                AttackDelegate attack = SkillUtil.GetTerrainAttack(user, tile);
                if (attack != null)
                {
                    attackTiles.AddRange(tile.GetAllNeighbors().Select(target => new AttackTile(target, attack)));
                    affectedTiles.AddRange(tile.GetAllNeighbors());
                }
            }
            affectedTiles.RemoveRange(user.Tiles);

            List<Wait> waits = new List<Wait>();
            foreach(var attackTile in attackTiles)
            {
                if (!affectedTiles.Contains(attackTile.Tile))
                    continue;
                foreach(var target in attackTile.Tile.Creatures)
                {
                    var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), attackTile.Attack);
                    waits.Add(wait);
                }
            }
            yield return new WaitAll(waits);
        }
    }

    /*class SkillCannon : Skill
    {
        public SkillCannon() : base("Cannon", "Ranged Fire Attack", 2, 3, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && !InLineOfSight(user, enemy.AggroTarget, 8))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            var offset = user.Facing.ToOffset();
            bool hit = false;
            Vector2 pos = new Vector2(user.X*16, user.Y*16);
            ShowSkill(user);
            yield return user.WaitSome(50);
            user.VisualPosition = user.Slide(pos + new Vector2(offset.X * -8, offset.Y * -8), pos, LerpHelper.Linear, 10);
            yield return user.WaitSome(20);
            for(int i = 1; i <= 8; i++)
            {
                var shootTile = user.Tile.GetNeighbor(i * offset.X, i * offset.Y);
                foreach(var target in shootTile.Creatures)
                {
                    
                    yield return user.Attack(target, offset.X, offset.Y, ExplosionAttack);
                    hit = true;
                    break;
                }
                if (hit)
                    break;
            }
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Fire, 1.0);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }*/

    class SkillSoulMissile : Skill
    {
        public int SalvoCount = 3;
        public int Missiles = 9;

        public SkillSoulMissile() : base("Soul Missile", $"Ranged {Element.Arcane.FormatString} Attack", 2, 5, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 5);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return GetPossibleTargets(user);
        }

        public IEnumerable<Creature> GetPossibleTargets(Enemy user)
        {
            List<Creature> enemies = new List<Creature>();
            foreach(var tile in user.Tile.GetNearby(user.Mask.GetRectangle(user.X,user.Y), 5))
            {
                enemies.AddRange(tile.Creatures);
            }
            return enemies.Where(x => x != user).Distinct().Shuffle(Random);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            int salvoCount;
            int missiles;
            double powerMod;

            var trigger = Random.NextDouble();

            if (trigger < 0.1)
            {
                salvoCount = Missiles * SalvoCount;
                missiles = 1;
                powerMod = 3;
            }
            else if(trigger < 0.3)
            {
                salvoCount = Missiles;
                missiles = SalvoCount;
                powerMod = 1.5;
            }
            else
            {
                salvoCount = SalvoCount;
                missiles = Missiles;
                powerMod = 1;
            }

            if (target is IEnumerable<Creature> targetCreatures)
            {
                Consume();
                ShowSkill(user);
                yield return user.WaitSome(50);

                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 20);
                List<Wait> waits = new List<Wait>();
                var targetSet = new List<Creature>();
                for (int i = 0; i < salvoCount; i++)
                {
                    targetSet.AddRange(targetCreatures.Take(missiles));
                }
                int missile = 0;
                foreach(var targetCreature in targetSet)
                {
                    float missileSlide = Missiles > 1 ? (missile % Missiles) / (float)(Missiles - 1) : 0.5f;
                    waits.Add(Scheduler.Instance.RunAndWait(RoutineMissile(user, targetCreature, missileSlide, powerMod)));
                    missile++;
                    if (missile % Missiles == 0)
                        yield return new WaitTime(20);
                    else
                        yield return new WaitTime(2);
                }
                yield return new WaitAll(waits);
            }
        }

        private IEnumerable<Wait> RoutineMissile(Creature user, Creature target, float missileSlide, double powerMod)
        {
            var missile = SpriteLoader.Instance.AddSprite("content/beam_soul");
            var emitVector = Util.AngleToVector(MathHelper.Lerp(-MathHelper.PiOver2 * 0.8f, MathHelper.PiOver2 * 0.8f, missileSlide)) * 100;
            var impactVector = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * Random.Next(25, 75);
            int duration = Random.Next(30, 50);
            new Missile(user.World, missile, () => user.VisualTarget, () => target.VisualTarget, emitVector, impactVector, duration, 10);
            yield return user.WaitSome(duration + 5);

            var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), (a,b) => MissileAttack(a,b,powerMod));
            yield return wait;
        }

        private static Attack MissileAttack(Creature user, IEffectHolder target, double powerMod)
        {
            Attack attack = new Attack(user, target);
            attack.SetParameters(20, powerMod, 1.0);
            attack.Elements.Add(Element.Arcane, 1.0);
            return attack;
        }
    }

    abstract class SkillBeamBase : Skill
    {
        public int Bolts = 1;

        public SkillBeamBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 4);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return GetPossibleTargets(user);
        }

        public IEnumerable<Creature> GetPossibleTargets(Enemy user)
        {
            var angle = Util.VectorToAngle(user.Facing.ToOffset().ToVector2());
            List<Creature> enemies = new List<Creature>();
            foreach (var tile in user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 5).Where(tile => InArc(user.ActualTarget, tile.VisualTarget, angle - MathHelper.PiOver4, angle + MathHelper.PiOver4)))
            {
                enemies.AddRange(tile.Creatures);
            }
            return enemies.Where(x => x != user).Distinct().Shuffle(Random);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            List<Creature> targetCreatures = new List<Creature>();
            if (target is IEnumerable<Creature> creatures)
                targetCreatures.AddRange(creatures.Take(Bolts));
            if (target is Creature creature)
                targetCreatures.Add(creature);

            if (targetCreatures.Count > 0)
            {
                Consume();
                ShowSkill(user);
                yield return user.WaitSome(50);

                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 20);
                List<Wait> waits = new List<Wait>();
                foreach (var targetCreature in targetCreatures)
                {
                    waits.Add(Scheduler.Instance.RunAndWait(RoutineBeam(user, targetCreature)));
                }
                yield return new WaitAll(waits);
            }
        }

        public abstract IEnumerable<Wait> RoutineBeam(Creature user, Creature target);
    }
    
    class SkillBeamFire : SkillBeamBase
    {
        public SkillBeamFire() : base("Fire Beam", $"Ranged {Element.Fire.FormatString} Attack. Sets all targets Aflame for 20 turns.", 2, 5, float.PositiveInfinity)
        {
            Bolts = 3;
        }

        public override IEnumerable<Wait> RoutineBeam(Creature user, Creature target)
        {
            var beam = SpriteLoader.Instance.AddSprite("content/beam_fire");
            new Arc(user.World, beam, () => user.VisualTarget, () => target.VisualTarget, Vector2.Zero, Vector2.Zero, 1, 20);
            yield return user.WaitSome(20);
            var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), BeamAttack);
            yield return wait;
        }

        private static Attack BeamAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.Fire, 1.0);
            attack.StatusEffects.Add(new Aflame()
            {
                Duration = new Slider(20),
                Buildup = 1.0,
            });
            return attack;
        }
    }

    class SkillBeamIce : SkillBeamBase
    {
        public SkillBeamIce() : base("Ice Beam", $"Ranged {Element.Ice.FormatString} Attack. Freezes all targets for 3 turns.", 2, 5, float.PositiveInfinity)
        {
            Bolts = 3;
        }

        public override IEnumerable<Wait> RoutineBeam(Creature user, Creature target)
        {
            var beam = SpriteLoader.Instance.AddSprite("content/beam_ice");
            new Arc(user.World, beam, () => user.VisualTarget, () => target.VisualTarget, Vector2.Zero, Vector2.Zero, 1, 20);
            yield return user.WaitSome(20);
            var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), BeamAttack);
            yield return wait;
        }

        private static Attack BeamAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.Ice, 1.0);
            attack.StatusEffects.Add(new Freeze()
            {
                Duration = new Slider(3),
                Buildup = 1,
            });
            return attack;
        }
    }

    class SkillBeamDisintegrate : SkillBeamBase
    {
        public SkillBeamDisintegrate() : base("Disintegration Beam", $"Ranged {Element.TheEnd} Attack", 2, 5, float.PositiveInfinity)
        {
        }

        public override IEnumerable<Wait> RoutineBeam(Creature user, Creature target)
        {
            var beam = SpriteLoader.Instance.AddSprite("content/beam_disintegrate");
            new Arc(user.World, beam, () => user.VisualTarget, () => target.VisualTarget, Vector2.Zero, Vector2.Zero, 1, 20);
            new DisintegrateEffect(user.World, SpriteLoader.Instance.AddSprite("content/cinder_ender"), target, 40);
            yield return user.WaitSome(20);
            var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), BeamAttack);
            yield return wait;
        }

        private static Attack BeamAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.TheEnd, 1.0);
            return attack;
        }
    }

    class SkillKamikaze : Skill
    {
        public SkillKamikaze() : base("Kamikaze", $"Destroys self. Triggers Death Throes.", 5, 0, float.PositiveInfinity)
        {

        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 1);
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
            user.TakeDamage(user.GetStat(Stat.HP), Element.Healing, null);
            user.CheckDead(Vector2.Zero);
            //user.AttackSelf(DeathAttack);
        }

        private static Attack DeathAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.SetParameters(user.GetStat(Stat.HP), 0, 0);
            attack.Elements.Add(Element.Healing, 1.0);
            return attack;
        }
    }

    class SkillWhirlwind : Skill
    {
        public SkillWhirlwind() : base("Whirlwind", $"Ranged {Element.Wind.FormatString} Attack", 2, 5, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InCircle(user, user.AggroTarget, 3);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            ShowSkill(user);
            yield return user.WaitSome(50);

            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 20);
            new WhirlWindEffect(user.World, user, 40);
            yield return new WaitTime(10);
            List<Wait> waits = new List<Wait>();
            var tiles = SkillUtil.GetCircularArea(user, 3).Except(user.Mask.Select(o => user.Tile.GetNeighbor(o.X, o.Y)));
            var targetCreatures = tiles.SelectMany(tile => tile.Creatures).Distinct();
            
            foreach(var targetCreature in targetCreatures)
            {
                var wait = user.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - user.VisualTarget), WindAttack);
                waits.Add(wait);
            }
            yield return new WaitTime(30);
            yield return new WaitAll(waits);
        }

        private static Attack WindAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.Wind, 1.0);
            return attack;
        }
    }

    class SkillLightning : Skill
    {
        public int Bolts = 1;

        public SkillLightning() : base("Lightning", $"Ranged {Element.Thunder.FormatString} Attack", 2, 5, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 4);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            ShowSkill(user);
            yield return user.WaitSome(50);

            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 20);
            List<Wait> waits = new List<Wait>();
            HashSet<Creature> targets = new HashSet<Creature>();
            for(int i = 0; i < Bolts; i++)
            {
                waits.Add(Scheduler.Instance.RunAndWait(RoutineThunder(user, targets)));
                yield return new WaitTime(5);
            }
            yield return new WaitAll(waits);
        }

        private int GetDistance(Rectangle a, Rectangle b)
        {
            int dx = Util.GetDeltaX(a, b);
            int dy = Util.GetDeltaY(a, b);

            return Math.Max(Math.Abs(dx), Math.Abs(dy));
        }

        private IEnumerable<Wait> RoutineThunder(Creature user, HashSet<Creature> targets)
        {
            Rectangle userMask = user.Mask.GetRectangle(user.X, user.Y);
            var nearbyTiles = user.Tile.GetNearby(userMask, 6).Where(tile => GetDistance(new Rectangle(tile.X, tile.Y, 1, 1), userMask) > 0).Shuffle(Random);
            Tile shootTile = null;
            var trigger = Random.NextDouble();
            var nearbyTarget = nearbyTiles.FirstOrDefault(tile => tile.Creatures.Any(c => !targets.Contains(c) && user.IsHostile(c)));
            var nearbyDragon = nearbyTiles.FirstOrDefault(tile => tile.Creatures.Any(c => !targets.Contains(c) && c is BlueDragon));
            if (trigger < 0.5 && nearbyTarget != null)
                shootTile = nearbyTarget;
            if (shootTile == null && nearbyDragon != null)
                shootTile = nearbyDragon;
            if (shootTile == null)
                shootTile = nearbyTiles.First();
            targets.AddRange(shootTile.Creatures);
            new Lightning(user.World, user.VisualTarget, shootTile.VisualTarget, 10, 10);
            yield return user.WaitSome(20);

            foreach (var chainTarget in shootTile.Creatures)
            {
                var wait = user.Attack(chainTarget, SkillUtil.SafeNormalize(chainTarget.VisualTarget - user.VisualTarget), ThunderAttack);
                yield return wait;
                break;
            }
        }

        private static Attack ThunderAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.Thunder, 1.0);
            return attack;
        }
    }

    class SkillLightningDance : SkillLightning
    {
        public SkillLightningDance()
        {
            Name = "Lightning Dance";
            Description = "5 Ranged Thunder Attacks. Does not strike targets twice.";
            Bolts = 5;
        }
    }

    class SkillRainDance : Skill
    {
        public SkillRainDance() : base("Rain Dance", "Starts a rainstorm for 20 turns.", 5, 35, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 6);
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
            var rain = user.Map.AddCloud(map => new WeatherRain(map));
            rain.ProvideEffect();
            rain.Duration = Math.Max(20, rain.Duration);
            yield return user.WaitSome(20);
        }
    }

    class SkillIronMaiden : Skill
    {
        public SkillIronMaiden() : base("Iron Maiden", $"Lowers enemy {Stat.Defense.FormatString}.", 4, 10, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 4);
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
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var effect = new IronMaiden(user.World, () => user.VisualTarget, () => targetCreature.VisualTarget, 5, 7, 10);
                yield return new WaitEffect(effect);
                targetCreature.AddStatusEffect(new DefenseDown() {
                    Buildup = 0.4,
                    Duration = new Slider(20),
                });
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillEnergyBall : Skill
    {
        public SkillEnergyBall() : base("Energy Ball", $"Ranged {Element.Thunder} damage.", 4, 10, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 4);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                var ball = SpriteLoader.Instance.AddSprite("content/blob_fire");
                var volt = SpriteLoader.Instance.AddSprite("content/lightning_spark_energy");

                Consume();
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var effect = new EnergyBall(user.World, ball, volt, user.VisualTarget, targetCreature.VisualTarget, 1.0f, 20, LerpHelper.Quadratic, 30);
                yield return new WaitEffect(effect);
                var wait = user.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - user.VisualTarget), Attack);
                yield return wait;
            }
        }

        protected Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Thunder, 1.0);
            return attack;
        }
    }

    class SkillMudBath : Skill
    {
        int MaxDistance = 4;

        public SkillMudBath() : base("Mud Bath", $"Replaces a 4 tile long carpet in front of the caster with Bog tiles.", 5, 5, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InLineOfSight(user, user.AggroTarget, 4, 1);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.Facing;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Facing facing)
            {
                Point offset = facing.ToOffset();
                Point lateral = facing.ToLateral();
                Consume();
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                Vector2 pos = new Vector2(user.X * 16, user.Y * 16);
                user.VisualPosition = user.Slide(pos + new Vector2(offset.X * -8, offset.Y * -8), pos, LerpHelper.Linear, 10);
                Rectangle rect = user.Mask.GetRectangle(user.X, user.Y);
                for(int dx = 1; dx <= 4; dx++)
                {
                    Point left = rect.GetLeft(facing) - lateral;
                    Point right = rect.GetRight(facing) + lateral;
                    foreach (Point dy in Util.Between(left,right))
                    {
                        Point totalPos = dy + new Point(offset.X * dx, offset.Y * dx);
                        var tile = user.Map.GetTile(totalPos.X, totalPos.Y);
                        if (tile.Tags.Contains(TileFlag.Floor) && !tile.Tags.Contains(TileFlag.Artificial))
                        {
                            tile.Replace(new Bog());
                        }
                    }
                    yield return user.WaitSome(10);
                }
            }
        }
    }

    class SkillBoilTallow : Skill
    {
        public SkillBoilTallow() : base("Boil Tallow", "Boils stolen flesh in a nearby Walking Cauldron.", 0, 0, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && user.GetStatusStacks<HagsFlesh>() >= 1 && GetValidTargets(user).Any();
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return GetValidTargets(user).Shuffle(Random).First();
        }

        private IEnumerable<WalkingCauldron> GetValidTargets(Creature user)
        {
            IEnumerable<Tile> tiles = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 4);
            return tiles.SelectMany(x => x.Creatures.OfType<WalkingCauldron>().Where(creature => !creature.HasStatusEffect<Boiling>())).Distinct();
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if(target is WalkingCauldron cauldron)
            {
                var pos = new Vector2(user.X * 16, user.Y * 16);
                var offset = user.Facing.ToOffset();
                Consume();
                ShowSkill(user);
                yield return user.WaitSome(50);
                user.VisualPosition = user.Slide(pos + new Vector2(offset.X * 8, offset.Y * 8), pos, LerpHelper.Linear, 10);
                user.VisualPose = user.FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
                cauldron.AddStatusEffect(new BoilingFlesh()
                {
                    Buildup = 1,
                });
                user.GetStatusEffect<HagsFlesh>().AddBuildup(-1);
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillTallowCurse : Skill
    {
        struct Target
        {
            public Creature CurseTarget;
            public Creature Cauldron;

            public Target(Creature curseTarget, Creature cauldron)
            {
                CurseTarget = curseTarget;
                Cauldron = cauldron;
            }
        }

        public SkillTallowCurse() : base("Tallow Curse", $"Consumes Boiling Flesh from a nearby Walking Cauldron. Restores all HP and curses enemy with {Stat.HP.FormatString} down.", 0, 0, float.PositiveInfinity)
        {
        }

        private IEnumerable<Creature> GetValidCauldrons(Creature user)
        {
            IEnumerable<Tile> tiles = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 4);
            return tiles.SelectMany(x => x.Creatures.Where(creature => creature.GetStatusStacks<BoilingFlesh>() >= 15)).Distinct();
        }


        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 6) && GetValidCauldrons(user).Any();
        }

        public override object GetEnemyTarget(Enemy user)
        {
            var cauldron = GetValidCauldrons(user).Shuffle(Random).First();
            return new Target(user.AggroTarget, cauldron);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Target tgt)
            {
                var curseTarget = tgt.CurseTarget;
                var cauldron = tgt.Cauldron;
                Consume();
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                cauldron.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                cauldron.ClearStatusEffects(statusEffect => statusEffect is BoilingFlesh && statusEffect.Buildup >= 15);
                Effect.Apply(new EffectStat(curseTarget, Stat.HP, -10));
                curseTarget.TakeStatDamage(90, Stat.HP);
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillIgniteBog : Skill
    {
        public SkillIgniteBog() : base("Ignite Bog", $"Ignite a random Bog tile within 4 tiles radius of the caster and deals {Element.Fire.FormatString} damage in a 1 tile radius. Ignited bog tile will produce fire clouds for 20 turns.", 5, 3, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 4) && GetValidTargets(user).Any() && user.CurrentHP < user.GetStat(Stat.HP) / 2;
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return null;
        }

        private IEnumerable<Tile> GetValidTargets(Creature user)
        {
            IEnumerable<Tile> tiles = user.Tile.GetNearby(user.Mask.GetRectangle(user.X,user.Y), 4);
            return tiles.Where(tile => tile is Bog);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Consume();
            ShowSkill(user);
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            yield return user.WaitSome(50);
            var targetTile = GetValidTargets(user).Shuffle(Random).First();
            new FireExplosion(user.World, targetTile.VisualTarget, Vector2.Zero, 0, 15);
            new ScreenShakeRandom(user.World, 6, 30, LerpHelper.Linear);
            if (!targetTile.Items.Any(x => x is BurningBog))
            {
                BurningBog burningBog = new BurningBog(targetTile.World)
                {
                    Duration = new Slider(20),
                };
                burningBog.MoveTo(targetTile);
            }
            var explosion = new Skills.Explosion(user, SkillUtil.GetCircularArea(targetTile, 1), targetTile.VisualTarget);
            explosion.Attack = ExplosionAttack;
            explosion.Fault = user;
            yield return explosion.Run();
            yield return user.WaitSome(20);
        }

        private static Attack ExplosionAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.SetParameters(40, 1, 1);
            attack.Elements.Add(Element.Fire, 1.0);
            return attack;
        }
    }

    abstract class SkillCloseStrikeBase : Skill
    {
        public override bool Hidden(Creature user) => true;

        public int Radius = 1;

        public SkillCloseStrikeBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InCircle(user, user.AggroTarget, Radius);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            var attackOffset = new List<Point>();
            var pA = new Point(user.X, user.Y);
            var pB = new Point(user.AggroTarget.X, user.AggroTarget.Y);
            foreach (var oA in user.Mask)
            {
                foreach(var oB in user.AggroTarget.Mask)
                {
                    attackOffset.Add(oB - oA + pB - pA);
                }
            }

            return attackOffset.Pick(Random);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Point offset)
            {
                Consume();
                return user.RoutineAttack(offset.X, offset.Y, Attack);
            }
            return Enumerable.Empty<Wait>();
        }

        protected abstract Attack Attack(Creature attacker, IEffectHolder defender);
    }

    class SkillVenomBite : SkillCloseStrikeBase
    {
        public SkillVenomBite() : base("Venom Bite", $"Attack", 3, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.Elements.Add(Element.Slash, 0.5);
            attack.StatusEffects.Add(new Poison() { Duration = new Slider(20), Buildup = 1 });
            return attack;
        }
    }

    class SkillLick : SkillCloseStrikeBase
    {
        public SkillLick() : base("Lick", $"Attack", 3, 3, float.PositiveInfinity)
        {
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.StatusEffects.Add(new Stun() { Duration = new Slider(1), Buildup = 1 });
            return attack;
        }
    }

    class SkillWallop : SkillCloseStrikeBase
    {
        struct WallopTarget
        {
            public Point offset;
            public Creature target;

            public WallopTarget(Point offset, Creature target)
            {
                this.offset = offset;
                this.target = target;
            }
        }

        public SkillWallop() : base("Wallop", $"Attack", 3, 3, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && GetMasters(user).Any(x => x is Creature);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            Point offset = (Point)base.GetEnemyTarget(user); //Yes it is.
            var master = GetMasters(user).First(x => x is Creature);
            return new WallopTarget(offset, (Creature)master);
        }

        private IEnumerable<IEffectHolder> GetMasters(Creature creature) //TODO: Move to util
        {
            return creature.GetEffects<EffectSummon>().Where(summon => summon.Slave == creature).Select(summon => summon.Master);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is WallopTarget wallopTarget)
            {
                Consume();
                var frontier = user.Mask.GetFrontier(wallopTarget.offset.X, wallopTarget.offset.Y);
                List<Wait> waitForDamage = new List<Wait>();
                var targetCreatures = new List<Creature>();
                foreach (var tile in frontier.Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
                {
                    targetCreatures.AddRange(tile.Creatures);
                }
                var targetTiles = GetWallopTargets(user, wallopTarget.target);
                var waits = new List<Wait>();
                int i = 0;
                foreach (var targetTuple in targetCreatures.Zip(targetTiles, (c,t) => new Tuple<Creature,Tile>(c,t)))
                {
                    waits.Add(Scheduler.Instance.RunAndWait(Wallop(user, targetTuple.Item1, targetTuple.Item2)));
                }
                user.AttackAnimation(wallopTarget.offset.X, wallopTarget.offset.Y);
                yield return new WaitTime(30);
                yield return new WaitAll(waits);
            }
        }

        private IEnumerable<Tile> GetWallopTargets(Creature user, Creature close)
        {
            var targets = new List<Tile>();
            var frontier = user.Mask.GetFullFrontier();
            var midDist = (user.VisualTarget - close.VisualTarget).LengthSquared();
            foreach(var tile in frontier.Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
            {
                var dist = (tile.VisualTarget - close.VisualTarget).LengthSquared();
                if(dist < midDist && !tile.Solid && !tile.Creatures.Any())
                {
                    targets.Add(tile);
                }
            }
            return targets.Shuffle(Random);
        }

        private IEnumerable<Wait> Wallop(Creature user, Creature targetCreature, Tile targetTile)
        {
            Vector2 startJump = targetCreature.VisualPosition();
            targetCreature.MoveTo(targetTile, 15);
            targetCreature.VisualPosition = targetCreature.SlideJump(startJump, new Vector2(targetCreature.X, targetCreature.Y) * 16, 16, LerpHelper.Linear, 15);
            yield return new WaitTime(15);
            var wait = user.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - user.VisualTarget), Attack);
            yield return wait;
        }

        protected override Attack Attack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackPhysical());
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    class SkillGrabbingTentacle : Skill
    {
        public SkillGrabbingTentacle() : base("Grabbing Tentacle", $"Draws target closer to brazil.", 5, 10, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 3) && !InRange(user, user.AggroTarget, 1) && GetValidDestinations(user).Any();
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public IEnumerable<Tile> GetValidDestinations(Creature user)
        {
            return SkillUtil.GetCircularArea(user, 1).Where(tile => !tile.Solid && !tile.Creatures.Any()).Shuffle(Random);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                var tentacle = SpriteLoader.Instance.AddSprite("content/beam_tendril");
                Consume();
                ShowSkill(user);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var destinationTile = GetValidDestinations(user).First();
                new TentacleBeam(user.World, tentacle, () => user.VisualTarget, () => targetCreature.VisualTarget, 10, 50);
                yield return user.WaitSome(10);
                targetCreature.MoveTo(destinationTile, 30);
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillOblivion : Skill
    {
        public SkillOblivion() : base("Oblivion", $"Immense {Element.Dark.FormatString} damage.", 16, 15, float.PositiveInfinity)
        {
            Priority = 10;
        }

        public override object GetEnemyTarget(Enemy user)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            throw new NotImplementedException();
        }
    }
}
