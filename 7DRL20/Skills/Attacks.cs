using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Skills
{
    class SkillAttack : Skill
    {
        public SkillAttack() : base("Attack", "Physical Attack", 0, 0, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && InMeleeRange(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            var offset = user.Facing.ToOffset();
            return user.RoutineAttack(offset.X, offset.Y, Creature.MeleeAttack);
        }
    }

    class SkillDrainTouch : Skill
    {
        public SkillDrainTouch() : base("Attack", "Drain Touch", 0, 3, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && InMeleeRange(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            var offset = user.Facing.ToOffset();
            return user.RoutineAttack(offset.X, offset.Y, DrainAttack);
        }

        private Attack DrainAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new AttackDrain(attacker, defender, 0.6);
            attack.Elements.Add(Element.Pierce, 2.0);
            return attack;
        }
    }

    class SkillAcidTouch : Skill
    {
        public SkillAcidTouch() : base("Attack", "Acid Touch", 0, 1, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && InMeleeRange(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            var offset = user.Facing.ToOffset();
            return user.RoutineAttack(offset.X, offset.Y, AcidAttack);
        }

        private Attack AcidAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.StatusEffects.Add(new DefenseDown()
            {
                Buildup = 0.2,
                Duration = new Slider(10)
            });
            return attack;
        }
    }

    class SkillPoisonTouch : Skill
    {
        public SkillPoisonTouch() : base("Attack", "Poison Touch", 0, 1, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && InMeleeRange(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            var offset = user.Facing.ToOffset();
            return user.RoutineAttack(offset.X, offset.Y, PoisonAttack);
        }

        private Attack PoisonAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.StatusEffects.Add(new Poison()
            {
                Buildup = 0.4,
                Duration = new Slider(15)
            });
            return attack;
        }
    }

    class SkillEnderClaw : Skill
    {
        public SkillEnderClaw() : base("Attack", "Ender Claw", 0, 1, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && InMeleeRange(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            var offset = user.Facing.ToOffset();
            return user.RoutineAttack(offset.X, offset.Y, EnderAttack);
        }

        private Attack EnderAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Slash, 0.5);
            attack.Elements.Add(Element.TheEnd, 1.0);
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

        //TODO: Probably add projectile class so we can have stuff like mirror spells
        protected IEnumerable<Wait> ShootStraight(Creature user, Tile tile, Point velocity, int time, int maxDistance, Bullet bullet, TrailDelegate trail, CanCollideDelegate canCollide, ImpactDelegate impact)
        {
            bullet.Setup(tile.VisualPosition, time * maxDistance);
            bool impacted = false;
            List<Wait> waits = new List<Wait>();
            for(int i = 0; i < maxDistance && !impacted; i++)
            {
                Tile nextTile = tile.GetNeighbor(velocity.X, velocity.Y);
                impacted = canCollide(user, nextTile);
                bullet.Move(nextTile.VisualPosition, time);
                if (impacted)
                {
                    yield return user.WaitSome(time/2);
                    bullet.Destroy();
                    waits.Add(Scheduler.Instance.RunAndWait(impact(user, nextTile)));
                }
                else
                {
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
        public SkillBloodSword() : base("Attack", "Blood Sword", 1, 3, float.PositiveInfinity)
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
            Point velocity = user.Facing.ToOffset();
            Consume();
            yield return user.WaitSome(20);
            var pos = new Vector2(user.X * 16, user.Y * 16);
            user.VisualPosition = user.Slide(pos + new Vector2(velocity.X * 8, velocity.Y * 8), pos, LerpHelper.Linear, 10);
            user.VisualPose = user.FlickPose(CreaturePose.Attack, CreaturePose.Stand, 5);
            yield return Scheduler.Instance.RunAndWait(Shoot(user, user.Tile, velocity));
        }

        private IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletTrail(user.World, SpriteLoader.Instance.AddSprite("content/bullet_sword"), Vector2.Zero, ColorMatrix.TwoColor(new Color(129, 166, 0), new Color(237, 255, 106)), Color.Red, 0);
            return ShootStraight(user, tile, velocity, 3, 10, bullet, NoTrail, CollideSolid, Impact);
            //new Color(225, 174, 210)
        }

        public IEnumerable<Wait> Impact(Creature user, Tile tile)
        {
            foreach (Creature creature in tile.Creatures)
                user.Attack(creature, 0, 0, BulletAttack);
            yield return user.WaitSome(0);
        }

        private Attack BulletAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new AttackDrain(attacker, defender, 1.0);
            attack.Elements.Add(Element.Slash, 1.0);
            return attack;
        }
    }

    class SkillDeathSword : SkillProjectileBase
    {
        public SkillDeathSword() : base("Attack", "Death Sword", 1, 3, float.PositiveInfinity)
        {
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Point velocity = user.Facing.ToOffset();
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

        private IEnumerable<Wait> Shoot(Creature user, Tile tile, Point velocity)
        {
            Bullet bullet = new BulletTrail(user.World, SpriteLoader.Instance.AddSprite("content/bullet_sword"), Vector2.Zero, ColorMatrix.Tint(new Color(225, 174, 210)), Color.Black, 0);
            return ShootStraight(user, tile, velocity, 3, 10, bullet, NoTrail, CollideSolid, Impact);
        }

        public IEnumerable<Wait> Impact(Creature user, Tile tile)
        {
            foreach (Creature creature in tile.Creatures)
                user.Attack(creature, 0, 0, BulletAttack);
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

        public override bool CanUse(Creature user)
        {
            if (CheckTarget && user is Enemy enemy && !InLineOfSight(user, enemy.AggroTarget, MaxDistance))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                Consume();
                var offset = user.Facing.ToOffset();
                yield return user.WaitSome(20);
                Tile lastSafeTile = user.Tile;
                var frontier = user.Mask.GetFrontier(offset.X, offset.Y);
                int creatureHits = 0;
                int wallHits = 0;
                PopupManager.StartCollect();
                for (int i = 0; i < MaxDistance; i++)
                {
                    if (!IsUnsafe(user))
                        lastSafeTile = user.Tile;
                    List<Wait> waitForDamage = new List<Wait>();
                    foreach (var tile in frontier.Select(o => user.Tile.GetNeighbor(o.X, o.Y)))
                    {
                        foreach (var creature in tile.Creatures)
                        {
                            waitForDamage.Add(user.Attack(creature, offset.X, offset.Y, RamAttack));
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
                yield return user.WaitSome(20);
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
        public SkillRam() : base("Attack", "Ram", 1, 1, float.PositiveInfinity)
        {

        }

        protected override Attack RamAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }

    class SkillEnderRam : SkillRamBase
    {
        public SkillEnderRam() : base("Attack", "Ender Ram", 1, 5, float.PositiveInfinity)
        {
            MaxDistance = 9;
            MaxTotalHits = 6;
            MaxWallHits = 4;
            MaxCreatureHits = 999999;
            DestroyWalls = true;
        }

        protected override Attack RamAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.Elements.Add(Element.TheEnd, 0.5);
            return attack;
        }
    }

    class SkillEnderMow : SkillRamBase
    {
        public SkillEnderMow() : base("Attack", "Ender Mow", 1, 3, float.PositiveInfinity)
        {
            MaxDistance = 3;
            MaxTotalHits = 2;
            MaxWallHits = 0;
            MaxCreatureHits = 999999;
            DestroyWalls = false;
            CheckTarget = false;
        }

        protected override Attack RamAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.Elements.Add(Element.Pierce, 0.5);
            attack.Elements.Add(Element.TheEnd, 0.5);
            return attack;
        }
    }

    class SkillCannon : Skill
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
            ShowSkill(user, SkillInfoTime);
            yield return user.WaitSome(50);
            user.VisualPosition = user.Slide(pos + new Vector2(offset.X * -8, offset.Y * -8), pos, LerpHelper.Linear, 10);
            yield return user.WaitSome(20);
            for(int i = 1; i <= 8; i++)
            {
                var shootTile = user.Tile.GetNeighbor(i * offset.X, i * offset.Y);
                foreach(var target in shootTile.Creatures)
                {
                    new FireExplosion(target.World, new Vector2(shootTile.X * 16 + 8, shootTile.Y * 16 + 8), Vector2.Zero, 0, 15);
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
    }

    class SkillLightning : Skill
    {
        public SkillLightning() : base("Lightning", "Ranged Thunder Attack", 2, 5, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && !InRange(user, enemy.AggroTarget, 4))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            ShowSkill(user, SkillInfoTime);
            yield return user.WaitSome(50);

            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 20);
            Creature aggroTarget = null;
            if (user is Enemy enemy)
                aggroTarget = enemy.AggroTarget;
            var nearbyTiles = user.Tile.GetNearby(user.Mask.GetRectangle(user.X,user.Y),6).Where(tile => tile != user.Tile).Shuffle();
            Tile shootTile = null;
            var trigger = Random.NextDouble();
            var nearbyTarget = nearbyTiles.FirstOrDefault(tile => tile.Creatures.Any(c => c == aggroTarget));
            var nearbyDragon = nearbyTiles.FirstOrDefault(tile => tile.Creatures.Any(c => c is BlueDragon));
            if (trigger < 0.5 && nearbyTarget != null)
                shootTile = nearbyTarget;
            if (shootTile == null && nearbyDragon != null)
                shootTile = nearbyDragon;
            if (shootTile == null)
                shootTile = nearbyTiles.First();
            new Lightning(user.World, user.VisualTarget, shootTile.VisualTarget, 10, 10);
            yield return user.WaitSome(20);

            foreach (var target in shootTile.Creatures)
            {
                yield return user.Attack(target, 0, 0, (attacker, defender) =>
                {
                    Attack attack = new Attack(user, target);
                    attack.Elements.Add(Element.Thunder, 1.0);
                    return attack;
                });
                break;
            }
            yield return user.WaitSome(20);
        }
    }

    abstract class SkillBreathBase : Skill
    {
        public SkillBreathBase(string name, string description, int warmup, int cooldown, float uses) : base(name, description, warmup, cooldown, uses)
        {
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user);
        }

        private float GetFacingAngle(Facing facing)
        {
            switch (facing)
            {
                default:
                case (Facing.North): return 0;
                case (Facing.East): return MathHelper.PiOver2;
                case (Facing.South): return MathHelper.Pi;
                case (Facing.West): return MathHelper.Pi + MathHelper.PiOver2;
            }
        }

        protected Tile GetImpactTile(Creature user, float angle, float radius)
        {
            Vector2 direction = Util.AngleToVector(angle);
            Vector2 offset = direction * radius;
            int tileX = (int)(user.VisualTarget.X / 16f + offset.X);
            int tileY = (int)(user.VisualTarget.Y / 16f + offset.Y);
            return user.Tile.Map.GetTile(tileX, tileY);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            float centerAngle = GetFacingAngle(user.Facing);

            float startAngle = centerAngle - MathHelper.PiOver2;
            float endAngle = centerAngle + MathHelper.PiOver2;
            float radius = 4;

            float arcLength = (endAngle - startAngle) * radius;
            float arcSpeed = 1;
            float increment = arcLength / arcSpeed;

            List<Wait> breaths = new List<Wait>();
            HashSet<Tile> tiles = new HashSet<Tile>();
            PopupManager.StartCollect();
            for (float slide = 0; slide <= arcLength; slide += arcSpeed)
            {
                float angle = MathHelper.Lerp(startAngle, endAngle, slide / arcLength);
                breaths.Add(Scheduler.Instance.RunAndWait(RoutineBreath(user, angle, radius, tiles)));
                yield return user.WaitSome(5);
            }
            yield return new WaitAll(breaths);
            PopupManager.FinishCollect();
            AfterBreath(user, tiles);
        }

        public abstract IEnumerable<Wait> RoutineBreath(Creature user, float angle, float radius, ICollection<Tile> tiles);

        public virtual void AfterBreath(Creature user, IEnumerable<Tile> tiles)
        {

        }
    }

    class SkillFireBreath : SkillBreathBase
    {
        public SkillFireBreath() : base("Fire Breath", "Description", 1, 1, float.PositiveInfinity)
        {
        }

        public override IEnumerable<Wait> RoutineBreath(Creature user, float angle, float radius, ICollection<Tile> tiles)
        {
            Tile tile = GetImpactTile(user, angle, radius);
            Vector2 direction = Util.AngleToVector(angle);
            Vector2 offset = direction * radius;
            new FireExplosion(user.World, user.VisualTarget, offset * 16f / 20f, angle, 20);
            if (tile != null)
                yield return Scheduler.Instance.RunAndWait(RoutineQuake(user, tile, 1, tiles));
        }

        private IEnumerable<Wait> RoutineQuake(Creature user, Tile impactTile, int radius, ICollection<Tile> tiles)
        {
            var tileSet = impactTile.GetNearby(radius).Where(tile => GetSquareDistance(impactTile, tile) <= radius * radius).Shuffle();
            int chargeTime = Random.Next(10) + 30;
            List<Tile> damageTiles = new List<Tile>();
            foreach (Tile tile in tileSet)
            {
                tile.VisualUnderColor = ChargeColor(user, chargeTime);
                if (!tiles.Contains(tile))
                    damageTiles.Add(tile);
                tiles.Add(tile);
            }
            new FireField(user.World, tileSet, chargeTime);
            new ScreenShakeRandom(user.World, 2, chargeTime + 30, LerpHelper.Invert(LerpHelper.Linear));
            yield return user.WaitSome(chargeTime);
            new ScreenShakeRandom(user.World, 4, 60, LerpHelper.Linear);
            foreach (Tile tile in tileSet)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.3)
                    new FireExplosion(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                else if (Random.NextDouble() < 0.7)
                    new FlameBig(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                tile.VisualUnderColor = () => Color.TransparentBlack;
            }
            foreach (Tile tile in damageTiles)
            {
                foreach(Creature target in tile.Creatures)
                {
                    user.Attack(target, 0, 0, ExplosionAttack);
                }
            }
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Fire, 1.0);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }

        private Func<Color> ChargeColor(Creature user, int time)
        {
            Color black = Color.TransparentBlack;
            Color red = new Color(117, 46, 11);
            Color orange = new Color(241, 153, 20);
            Color yellow = new Color(254, 241, 169);
            int startTime = user.Frame;
            return () =>
            {
                float slide = (float)(user.Frame - startTime) / time;
                if (slide < 0.25f)
                    return Color.Lerp(black, red, slide / 0.25f);
                else if (slide < 0.25f * 2)
                    return Color.Lerp(red, orange, (slide - 0.25f) / 0.25f);
                else if (slide < 0.25f * 3)
                    return Color.Lerp(orange, yellow, (slide - 0.25f * 2) / 0.25f);
                else
                {
                    Color glow = Color.Lerp(Color.Black, Color.White, 0.5f + (float)Math.Sin((user.Frame + time) * 0.4) * 0.5f);
                    return Color.Lerp(yellow, glow, (slide - 0.25f * 3) / 0.25f);
                }
            };
        }
    }

    abstract class SkillJumpBase : Skill
    {
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

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && !GetPossibleTiles(user,enemy.AggroTarget).Any())
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy) {
                Consume();
                yield return user.WaitSome(20);
                var tiles = GetPossibleTiles(user, enemy.AggroTarget);
                if (tiles.Any())
                {
                    TileDirection tile = tiles.First();
                    Vector2 startJump = user.VisualPosition();
                    enemy.MoveTo(tile.Tile, 20);
                    enemy.Facing = tile.Facing;
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
        public override bool WaitUse => true;

        public SkillDive() : base("Dive", "Move to tile nearby", 2, 3, float.PositiveInfinity)
        {
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            yield return user.CurrentAction;
            Consume();
            Vector2 pos = new Vector2(user.X * 16, user.Y * 16);
            new WaterSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 0, 12);
            user.VisualColor = user.Static(Color.Transparent);
            var nearbyTiles = user.Tile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).ToList();
            user.MoveTo(nearbyTiles.Pick(Random),0);
            yield return user.WaitSome(5);
            new WaterSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 0, 12);
            user.VisualColor = user.Static(Color.White);
        }
    }

    class SkillWarp : Skill
    {
        public override bool WaitUse => true;

        public SkillWarp() : base("Warp", "Move to chase enemy", 0, 2, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && InRange(user, enemy.AggroTarget, 4))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                yield return user.CurrentAction;
                Consume();
                user.VisualColor = user.Flick(user.Flash(user.Static(Color.Transparent), user.Static(Color.White), 2, 2), user.Static(Color.White), 20);
                yield return user.WaitSome(20);
                user.VisualColor = user.Static(Color.Transparent);
                var nearbyTiles = enemy.AggroTarget.Tile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).ToList();
                user.MoveTo(nearbyTiles.Pick(Random),0);
                yield return user.WaitSome(20);
                user.VisualColor = user.Flick(user.Flash(user.Static(Color.Transparent), user.Static(Color.White), 2, 2), user.Static(Color.White), 20);
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillIronMaiden : Skill
    {
        public SkillIronMaiden() : base("Iron Maiden", "Lowers enemy defense.", 4, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && !InRange(user, enemy.AggroTarget, 4))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                Consume();
                ShowSkill(user, SkillInfoTime);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var target = enemy.AggroTarget;
                var effect = new IronMaiden(user.World, () => user.VisualTarget, () => target.VisualTarget, 5, 7, 10);
                yield return new WaitEffect(effect);
                target.AddStatusEffect(new DefenseDown() {
                    Buildup = 0.4,
                    Duration = new Slider(20),
                });
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillEnderPowerUp : Skill
    {
        public SkillEnderPowerUp() : base("Power Up", "Enrage.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && user.HasStatusEffect(statusEffect => statusEffect is PoweredUp))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                Consume();
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                SpriteReference cinder = SpriteLoader.Instance.AddSprite("content/cinder_ender");
                new FlarePower(user.World, cinder, user, 50);
                new ScreenFlashPowerUp(user, () => ColorMatrix.Ender(), 100, 100, 50, 50);
                user.AddStatusEffect(new PoweredUp());
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillEnderFlare : Skill
    {
        public SkillEnderFlare() : base("Ender Flare", "Ranged The End Attack.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && (!InRange(user, enemy.AggroTarget, 4) || !user.HasStatusEffect(statusEffect => statusEffect is PoweredUp)))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                Consume();
                ShowSkill(user, SkillInfoTime);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var target = enemy.AggroTarget;
                var effect = new FlareCharge(user.World, SpriteLoader.Instance.AddSprite("content/cinder_ender"), user, () => target.VisualTarget, 200);
                
                yield return user.WaitSome(50);
                new ScreenShakeRandom(user.World, 2, 150, LerpHelper.Invert(LerpHelper.Linear));
                yield return new WaitEffect(effect);
                new ScreenFlashLocal(user.World, () => ColorMatrix.Ender(), target.VisualTarget, 60, 150, 80, 50);
                new EnderNuke(user.World, SpriteLoader.Instance.AddSprite("content/nuke_ender"), target.VisualTarget, 0.6f, 80);
                new ScreenShakeRandom(user.World, 8, 80, LerpHelper.QuarticIn);
                //new BigExplosion(user.World, () => target.VisualTarget, (pos, time) => new EnderExplosion(user.World, pos, Vector2.Zero, time));
                yield return user.WaitSome(10);
                yield return user.Attack(target, 0, 0, (attacker, defender) =>
                {
                    Attack attack = new Attack(user, target);
                    attack.Elements.Add(Element.TheEnd, 1.0);
                    return attack;
                });
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillEnderBlast : Skill
    {
        public SkillEnderBlast() : base("Ender Blast", "Frees Ender Erebizo from rock.", 0, 0, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user.Tiles.Any(tile => !tile.Opaque))
                return false;
            if (user is Enemy enemy && (!InRange(user, enemy.AggroTarget, 8)))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                Consume();
                yield return user.WaitSome(50);
                var tileSet = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 1).Shuffle();
                List<Wait> quakes = new List<Wait>();
                HashSet<Tile> tiles = new HashSet<Tile>();
                PopupManager.StartCollect();
                foreach (Tile tile in tileSet.Take(5))
                {
                    quakes.Add(Scheduler.Instance.RunAndWait(RoutineQuake(user, tile, 2, tiles)));
                }
                new ScreenFlashLocal(user.World, () => ColorMatrix.Ender(), user.VisualTarget, 60, 150, 100, 50);
                yield return new WaitAll(quakes);
                PopupManager.FinishCollect();
                yield return user.WaitSome(20);
            }
        }

        private IEnumerable<Wait> RoutineQuake(Creature user, Tile impactTile, int radius, ICollection<Tile> tiles)
        {
            var tileSet = impactTile.GetNearby(radius).Where(tile => tile.Opaque).Where(tile => GetSquareDistance(impactTile, tile) <= radius * radius).Shuffle();
            int chargeTime = 60;
            List<Tile> damageTiles = new List<Tile>();
            foreach (Tile tile in tileSet)
            {
                tile.VisualUnderColor = ChargeColor(user, chargeTime);
                if (!tiles.Contains(tile))
                    damageTiles.Add(tile);
                tiles.Add(tile);

            }
            new ScreenShakeRandom(user.World, 4, chargeTime + 60, LerpHelper.Invert(LerpHelper.Linear));
            yield return user.WaitSome(chargeTime);
            new LightningField(user.World, SpriteLoader.Instance.AddSprite("content/lightning_ender"), tileSet, 60);
            yield return user.WaitSome(60);
            new ScreenShakeRandom(user.World, 8, 60, LerpHelper.Linear);
            foreach (Tile tile in tileSet)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.7)
                    new EnderExplosion(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                tile.VisualUnderColor = () => Color.TransparentBlack;
                tile.MakeFloor();
            }
        }

        private Func<Color> ChargeColor(Creature user, int time)
        {
            Color black = Color.TransparentBlack;
            Color darkPurple = new Color(103, 21, 138);
            Color purple = new Color(174, 56, 224);
            Color blue = new Color(196, 223, 251);
            int startTime = user.Frame;
            return () =>
            {
                float slide = (float)(user.Frame - startTime) / time;
                if (slide < 0.25f)
                    return Color.Lerp(black, darkPurple, slide / 0.25f);
                else if (slide < 0.25f * 2)
                    return Color.Lerp(darkPurple, purple, (slide - 0.25f) / 0.25f);
                else if (slide < 0.25f * 3)
                    return Color.Lerp(purple, blue, (slide - 0.25f * 2) / 0.25f);
                else
                {
                    Color glow = Color.Lerp(Color.Black, Color.White, 0.5f + (float)Math.Sin((user.Frame + time) * 0.4) * 0.5f);
                    return Color.Lerp(blue, glow, (slide - 0.25f * 3) / 0.25f);
                }
            };
        }
    }

    class SkillEnderQuake : Skill
    {
        public SkillEnderQuake() : base("Ender Quake", "Ranged The End Attack.", 3, 10, float.PositiveInfinity)
        {
        }

        public override bool CanUse(Creature user)
        {
            if (user is Enemy enemy && (!InRange(user, enemy.AggroTarget, 8) || !user.HasStatusEffect(statusEffect => statusEffect is PoweredUp)))
                return false;
            return base.CanUse(user);
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            if (user is Enemy enemy)
            {
                Consume();
                ShowSkill(user, SkillInfoTime);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                //TODO: roar?
                //TODO: jump visual, screenshake, screen distort
                yield return user.WaitSome(50);
                user.VisualPosition = user.SlideJump(user.VisualPosition(), new Vector2(user.X, user.Y) * 16, 16, LerpHelper.Linear, 20);
                yield return user.WaitSome(20);
                new ScreenShakeRandom(user.World, 6, 30, LerpHelper.Linear);
                new SeismArea(user.World, user.Tiles, 10);
                var tileSet = user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 6).Shuffle();
                List<Wait> quakes = new List<Wait>();
                HashSet<Tile> tiles = new HashSet<Tile>();
                PopupManager.StartCollect();
                foreach (Tile tile in tileSet.Take(8))
                {
                    quakes.Add(Scheduler.Instance.RunAndWait(RoutineQuake(user, tile, 3, tiles)));
                }
                new ScreenFlashLocal(user.World, () => ColorMatrix.Ender(), user.VisualTarget, 60, 150, 100, 50);
                yield return new WaitAll(quakes);
                PopupManager.FinishCollect();
                yield return user.WaitSome(20);
            }
        }

        private IEnumerable<Wait> RoutineQuake(Creature user, Tile impactTile, int radius, ICollection<Tile> tiles)
        {
            var tileSet = impactTile.GetNearby(radius).Where(tile => GetSquareDistance(impactTile,tile) <= radius*radius).Shuffle();
            int chargeTime = Random.Next(10) + 60;
            List<Tile> damageTiles = new List<Tile>();
            foreach (Tile tile in tileSet)
            {
                tile.VisualUnderColor = ChargeColor(user, chargeTime);
                if (!tiles.Contains(tile))
                    damageTiles.Add(tile);
                tiles.Add(tile);
            }
            new ScreenShakeRandom(user.World, 4, chargeTime + 60, LerpHelper.Invert(LerpHelper.Linear));
            yield return user.WaitSome(chargeTime);
            new LightningField(user.World, SpriteLoader.Instance.AddSprite("content/lightning_ender"), tileSet, 60);
            yield return user.WaitSome(60);
            new ScreenShakeRandom(user.World, 8, 60, LerpHelper.Linear);
            foreach (Tile tile in tileSet)
            {
                Vector2 offset = new Vector2(-0.5f + Random.NextFloat(), -0.5f + Random.NextFloat()) * 16;
                if (Random.NextDouble() < 0.7)
                    new EnderExplosion(user.World, tile.VisualTarget + offset, Vector2.Zero, 0, Random.Next(14) + 6);
                tile.VisualUnderColor = () => Color.TransparentBlack;
            }
            foreach (Tile tile in damageTiles)
            {
                foreach(Creature creature in tile.Creatures)
                {
                    user.Attack(creature, 0, 0, AttackQuake);
                }
            }
        }

        private Attack AttackQuake(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.TheEnd, 1.0);
            return attack;
        }

        private Func<Color> ChargeColor(Creature user, int time)
        {
            Color black = Color.TransparentBlack;
            Color darkPurple = new Color(103, 21, 138);
            Color purple = new Color(174, 56, 224);
            Color blue = new Color(196, 223, 251);
            int startTime = user.Frame;
            return () =>
            {
                float slide = (float)(user.Frame - startTime) / time;
                if (slide < 0.25f)
                    return Color.Lerp(black, darkPurple, slide / 0.25f);
                else if (slide < 0.25f * 2)
                    return Color.Lerp(darkPurple, purple, (slide - 0.25f) / 0.25f);
                else if (slide < 0.25f * 3)
                    return Color.Lerp(purple, blue, (slide - 0.25f * 2) / 0.25f);
                else
                {
                    Color glow = Color.Lerp(Color.Black, Color.White, 0.5f + (float)Math.Sin((user.Frame + time) * 0.4) * 0.5f);
                    return Color.Lerp(blue, glow, (slide - 0.25f * 3) / 0.25f);
                }
            };
        }
    }
}
