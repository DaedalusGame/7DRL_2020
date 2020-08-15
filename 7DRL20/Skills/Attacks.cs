using Microsoft.Xna.Framework;
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
                attack.StatusEffects.Add(new Slimed(attacker)
                {
                    Buildup = 0.4,
                    Duration = new Slider(15)
                });
            return attack;
        }
    }

    abstract class SkillProjectileBase : Skill
    {
        protected delegate IEnumerable<Wait> TrailDelegate(Creature user, Tile tile);
        protected delegate bool CanCollideDelegate(Creature user, Tile tile);
        protected delegate IEnumerable<Wait> ImpactDelegate(Creature user, Tile tile);

        public SkillProjectileBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.Facing;
        }

        //TODO: Probably add projectile class so we can have stuff like mirror spells
        protected IEnumerable<Wait> ShootStraight(Creature user, Tile tile, Point velocity, int time, int maxDistance, Bullet bullet, TrailDelegate trail, CanCollideDelegate canCollide, ImpactDelegate impact)
        {
            bullet?.Setup(tile.VisualTarget, time * maxDistance);
            bool impacted = false;
            List<Wait> waits = new List<Wait>();
            for(int i = 0; i < maxDistance && !impacted; i++)
            {
                Tile nextTile = tile.GetNeighbor(velocity.X, velocity.Y);
                impacted = canCollide(user, nextTile);
                bullet?.Move(nextTile.VisualTarget, time);
                if (impacted)
                {
                    if(time > 0)
                        yield return user.WaitSome(time/2);
                    bullet?.Destroy();
                    waits.Add(Scheduler.Instance.RunAndWait(impact(user, nextTile)));
                }
                else
                {
                    if (time > 0)
                        yield return user.WaitSome(time);
                    waits.Add(Scheduler.Instance.RunAndWait(trail(user, nextTile)));
                }
                tile = nextTile;
            }
            yield return new WaitAll(waits);
        }

        protected IEnumerable<Wait> NoTrail(Creature user, Tile tile)
        {
            return Enumerable.Empty<Wait>();
        }

        protected bool CollideSolid(Creature user, Tile tile)
        {
            return tile.Solid || tile.Creatures.Any(x => x != user);
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
            return base.CanEnemyUse(user) && InLineOfSight(user, user.AggroTarget, MaxDistance);
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
            return ShootStraight(user, tile, velocity, 3, MaxDistance, bullet, NoTrail, CollideSolid, Impact);
            //new Color(225, 174, 210)
        }

        public IEnumerable<Wait> Impact(Creature user, Tile tile)
        {
            Point velocity = user.Facing.ToOffset();
            List<Wait> waits = new List<Wait>();
            foreach (Creature creature in tile.Creatures)
            {
                user.Attack(creature, velocity.X, velocity.Y, BulletAttack);
                waits.Add(creature.CurrentAction);
            }
            yield return new WaitAll(waits);
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.ExtraEffects.Add(new AttackDrain(1.0));
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }
    }

    class SkillCannonShot : SkillProjectileBase
    {
        int MaxDistance = 8;

        public SkillCannonShot() : base("Cannon", "Ranged Fire Attack", 2, 3, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InLineOfSight(user, user.AggroTarget, MaxDistance);
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
            return ShootStraight(user, tile, velocity, 0, MaxDistance, null, NoTrail, CollideSolid, Impact);
            //new Color(225, 174, 210)
        }

        public IEnumerable<Wait> Impact(Creature user, Tile tile)
        {
            Point velocity = user.Facing.ToOffset();
            new FireExplosion(user.World, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8), Vector2.Zero, 0, 15);
            List<Wait> waits = new List<Wait>();
            foreach (Creature creature in tile.Creatures)
            {
                user.Attack(creature, velocity.X, velocity.Y, BulletAttack);
                waits.Add(creature.CurrentAction);
            }
            yield return new WaitAll(waits);
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
        public SkillDeathSword() : base("Death Blade", "Wide-range slashing attack", 1, 3, float.PositiveInfinity)
        {
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
            return ShootStraight(user, tile, velocity, 3, 10, bullet, NoTrail, CollideSolid, Impact);
        }

        public IEnumerable<Wait> Impact(Creature user, Tile tile)
        {
            Point velocity = user.Facing.ToOffset();
            List<Wait> waits = new List<Wait>();
            foreach (Creature creature in tile.Creatures)
            {
                user.Attack(creature, velocity.X, velocity.Y, BulletAttack);
                waits.Add(creature.CurrentAction);
            }
            yield return user.WaitSome(0);
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
            return base.CanEnemyUse(user) && user.AggroTarget != null && (!CheckTarget || InLineOfSight(user, user.AggroTarget, MaxDistance));
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
                PopupManager.StartCollect();
                for (int i = 0; i < MaxDistance; i++)
                {
                    if (!IsUnsafe(user))
                        lastSafeTile = user.Tile;
                    foreach (var tile in frontier.Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
                    {
                        foreach (var creature in tile.Creatures)
                        {
                            user.Attack(creature, offset.X, offset.Y, RamAttack);
                            waitForDamage.Add(creature.CurrentAction);
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
                PopupManager.FinishCollect();
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

        public IEnumerable<Wait> RoutineDamageSurrounding(Creature user, HashSet<Creature> targets, bool multihit, Func<Creature, IEffectHolder, Attack> attack)
        {
            List<Wait> waits = new List<Wait>();
            foreach (var tile in user.Mask.GetFullFrontier().Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
            {
                foreach (var target in tile.Creatures)
                {
                    if (multihit || !targets.Contains(target))
                    {
                        user.Attack(target, 0, 0, attack);
                    }
                    targets.Add(target);
                    waits.Add(target.CurrentAction);
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
            public Func<Creature,IEffectHolder,Attack> Attack;

            public AttackTile(Tile tile, Func<Creature, IEffectHolder, Attack> attack)
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
            return base.CanEnemyUse(user) && InFrontier(user, user.IsHostile) && user.Tiles.Any(tile => GetAttack(user,tile) != null);
        }

        public override IEnumerable<Wait> RoutineImpact(Creature user)
        {
            new ScreenShakeRandom(user.World, 6, 30, LerpHelper.Linear);
            new SeismArea(user.World, user.Tiles, 10);
            HashSet<Tile> affectedTiles = new HashSet<Tile>();
            List<AttackTile> attackTiles = new List<AttackTile>();
            foreach(var tile in user.Tiles)
            {
                Func<Creature, IEffectHolder, Attack> attack = GetAttack(user, tile);
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
                    user.Attack(target, 0, 0, attackTile.Attack);
                    waits.Add(target.CurrentAction);
                }
            }
            yield return new WaitAll(waits);
        }

        private Func<Creature, IEffectHolder, Attack> GetAttack(Creature user, Tile tile)
        {
            if(tile is Water)
            {
                return WaterAttack;
            }
            if(tile is Lava)
            {
                return LavaAttack;
            }

            return null;
        }

        private Attack WaterAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(25, 1, 1);
            attack.Elements.Add(Element.Water, 1.0);
            attack.StatusEffects.Add(new Wet()
            {
                Duration = new Slider(10),
                Buildup = 1,
            });
            return attack;
        }

        private Attack LavaAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(50, 1, 1);
            attack.Elements.Add(Element.Fire, 1.0);
            return attack;
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

    class SkillLightning : Skill
    {
        public int Bolts = 1;

        public SkillLightning() : base("Lightning", "Ranged Thunder Attack", 2, 5, float.PositiveInfinity)
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
                user.Attack(chainTarget, 0, 0, ThunderAttack);
                yield return chainTarget.CurrentAction;
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
            rain.Duration = Math.Max(20, rain.Duration);
            yield return user.WaitSome(20);
        }
    }

    class SkillIronMaiden : Skill
    {
        public SkillIronMaiden() : base("Iron Maiden", "Lowers enemy defense.", 4, 10, float.PositiveInfinity)
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

    class SkillForcefield : Skill
    {
        public SkillForcefield() : base("Forcefield", "Select 1 random base Element. Become weak to this element. Become immune to all the others.", 0, 0, 1)
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

    class SkillAgeOfDragons : Skill
    {
        public SkillAgeOfDragons() : base("Age of Dragons", "10 Extra Turns.", 15, 15, float.PositiveInfinity)
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

    class SkillOblivion : Skill
    {
        public SkillOblivion() : base("Oblivion", "Immense Dark damage.", 16, 15, float.PositiveInfinity)
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
