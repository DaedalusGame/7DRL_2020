using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Attacks
{
    class SkillPhalange : Skill
    {
        public SkillPhalange() : base("Phalange", "Ranged Physical damage.", 0, 3, float.PositiveInfinity)
        {
            InstantUses = new Slider(2);
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 8);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                List<Creature> targets = new List<Creature>();
                Consume();
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(10);
                int chiralStacks = user.GetStatusStacks<Chirality>();
                int hits = 0;
                List<Wait> waits = new List<Wait>();
                for (int i = 0; i < chiralStacks + 1; i++)
                {
                    targets.Add(targetCreature);
                    waits.Add(Scheduler.Instance.RunAndWait(RoutineHand(user, targetCreature, hits)));
                    hits++;
                    yield return user.WaitSome(7);
                }
                yield return new WaitAll(waits);
                double chiralBuildup = 0.2 * hits;
                if (targetCreature.HasStatusEffect<DeltaMark>())
                    chiralBuildup *= 3;
                //chiralBuildup = Math.Min(chiralBuildup, 5);
                user.AddStatusEffect(new Chirality()
                {
                    Buildup = chiralBuildup,
                    Duration = new Slider(float.PositiveInfinity),
                });
                yield return new WaitAll(targets.Select(GetCurrentAction));
            }
        }

        private IEnumerable<Wait> RoutineHand(Creature user, Creature target, int hits)
        {
            List<Vector2> wingPositions = Wallhach.GetWingPositions(user.VisualTarget, 1.0f);
            Vector2 velocity = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * 160;
            Vector2 emitPos = wingPositions.Pick(Random);
            int moveTime = 30 + Random.Next(30);
            var bullet = new MissileHand(user.World, emitPos, target.VisualTarget, velocity, ColorMatrix.Tint(Color.Goldenrod), moveTime, moveTime);
            yield return new WaitBullet(bullet);
            if (hits % 3 >= 2)
                user.Attack(target, 0, 0, AttackSlap);
        }

        private Attack AttackSlap(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.3);
            return attack;
        }
    }

    class SkillHeptablast : Skill
    {
        public SkillHeptablast() : base("Heptablast", "Physical damage to 10 random targets. -5 Chirality stacks.", 9, 0, float.PositiveInfinity)
        {
            Priority = 5;
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && user.GetStatusStacks<Chirality>() >= 10;
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && !InRange(user, user.AggroTarget, 3);
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override void Update(Creature user)
        {
            base.Update(user);
            if (user.GetStatusStacks<Chirality>() < 10)
                Warmup.Time = 0;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            if (target is Creature targetCreature)
            {
                List<Creature> targets = new List<Creature>();
                Consume();
                ShowSkill(user);
                var chirality = user.GetStatusEffect<Chirality>();
                chirality?.AddBuildup(-5);
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                yield return user.WaitSome(50);
                var bullet = new BulletSpeed(user.World, SpriteLoader.Instance.AddSprite("content/highspeed"), user.VisualTarget, ColorMatrix.Tint(Color.Black), 15);
                bullet.Move(targetCreature.VisualTarget, 15);
                yield return new WaitBullet(bullet);
                SpriteReference triangle = SpriteLoader.Instance.AddSprite("content/triangle");
                for (int i = 0; i < 50; i++)
                {
                    Vector2 emitPos = new Vector2(targetCreature.X * 16, targetCreature.Y * 16) + targetCreature.Mask.GetRandomPixel(Random);
                    Vector2 centerPos = targetCreature.VisualTarget;
                    Vector2 velocity = Vector2.Normalize(emitPos - centerPos) * (Random.NextFloat() + 0.5f) * 40f;
                    new Triangle(user.World, triangle, emitPos, velocity, MathHelper.ToRadians(40 * (Random.NextFloat() - 0.5f)), Color.Black, Random.Next(80) + 5);
                }
                yield return user.WaitSome(20);
                List<Wait> waits = new List<Wait>();
                PopupManager.StartCollect();
                for (int i = 0; i < 10; i++)
                {
                    targets.Add(targetCreature);
                    waits.Add(Scheduler.Instance.RunAndWait(RoutineSlap(user, targetCreature)));
                    yield return user.WaitSome(4);
                }
                yield return new WaitAll(waits);
                PopupManager.FinishCollect();
                yield return new WaitAll(targets.Select(GetCurrentAction));
            }
        }

        private IEnumerable<Wait> RoutineSlap(Creature user, Creature target)
        {
            var offset = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * (40 + Random.Next(30));
            var bullet = new BulletSpeed(user.World, SpriteLoader.Instance.AddSprite("content/highspeed"), target.VisualTarget + offset, ColorMatrix.Tint(Color.Black), 6);
            bullet.Move(target.VisualTarget, 6);
            yield return new WaitBullet(bullet);
            user.Attack(target, 0, 0, AttackSlap);
        }

        private Attack AttackSlap(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            return attack;
        }
    }

    class SkillDeltaAttack : Skill
    {
        public SkillDeltaAttack() : base("Delta Attack", "Ranged Earth damage.", 3, 3, float.PositiveInfinity)
        {
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && InRange(user, user.AggroTarget, 5);
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
                var bullet = new BulletDelta(user.World, SpriteLoader.Instance.AddSprite("content/delta"), user.VisualTarget, ColorMatrix.Tint(Color.Goldenrod), MathHelper.ToRadians(20), 50);
                bullet.Move(targetCreature.VisualTarget, 30);
                yield return user.WaitSome(30);
                new RockTremor(user.World, targetCreature, 30);
                yield return new WaitBullet(bullet);
                user.Attack(targetCreature, 0, 0, AttackDelta);
                yield return targetCreature.CurrentAction;
            }
        }

        private Attack AttackDelta(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Earth, 1.5);
            attack.StatusEffects.Add(new DeltaMark()
            {
                Duration = new Slider(3),
                Buildup = 1.0,
            });
            return attack;
        }
    }

    class SkillWedlock : Skill
    {
        public SkillWedlock() : base("Wedlock", "Prevents Quick-swapping and Unequipping. -8 Chirality stacks.", 3, 3, float.PositiveInfinity)
        {
            Priority = 10;
        }

        public override bool CanUse(Creature user)
        {
            return base.CanUse(user) && user.GetStatusStacks<Chirality>() >= 16;
        }

        public override bool CanEnemyUse(Enemy user)
        {
            return base.CanEnemyUse(user) && !user.AggroTarget.HasStatusEffect<Wedlock>();
        }

        public override object GetEnemyTarget(Enemy user)
        {
            return user.AggroTarget;
        }

        public override IEnumerable<Wait> RoutineUse(Creature user, object target)
        {
            Wallhach wallhach = user as Wallhach;
            if (target is Creature targetCreature)
            {
                Consume();
                ShowSkill(user);
                var chirality = user.GetStatusEffect<Chirality>();
                chirality?.AddBuildup(-8);
                if (wallhach != null)
                {
                    yield return Scheduler.Instance.RunAndWait(wallhach.RoutineOpenWing(0.6f, 50, LerpHelper.Quadratic));
                }
                user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
                if (wallhach != null)
                {
                    Scheduler.Instance.Run(wallhach.RoutineFlashWing(15));
                }
                yield return user.WaitSome(10);
                int chiralStacks = user.GetStatusStacks<Chirality>();
                List<Wait> waits = new List<Wait>();
                int hands = chiralStacks + 1;
                hands = 16;
                int timeLeft = hands + 90;
                for (int i = 0; i < hands; i++)
                {
                    List<Vector2> wingPositions = Wallhach.GetWingPositions(user.VisualTarget, 1.0f);
                    Vector2 velocity = Util.AngleToVector(Random.NextFloat() * MathHelper.TwoPi) * 160;
                    Vector2 emitPos = wingPositions.Pick(Random);
                    var bullet = new MissileHand(user.World, emitPos, targetCreature.VisualTarget, velocity, ColorMatrix.Tint(Color.Goldenrod), 60, timeLeft - i);
                    waits.Add(new WaitBullet(bullet));
                    yield return user.WaitSome(1);
                }
                yield return new WaitAll(waits);
                targetCreature.AddStatusEffect(new Wedlock(user)
                {
                    Duration = new Slider(float.PositiveInfinity),
                    Buildup = 1.0,
                });
                if (wallhach != null)
                {
                    Scheduler.Instance.Run(wallhach.RoutineOpenWing(1f, 50, LerpHelper.Linear));
                }
                yield return user.WaitSome(50);
            }
        }
    }

    class SkillGeomancy : Skill
    {
        public SkillGeomancy() : base("Geomancy", "Everybody gains stat bonuses based on occupied tiles.", 0, 0, 1)
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
            ShowSkill(user);
            yield return user.WaitSome(20);
            user.VisualPose = user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 70);
            int radius = 20;
            var tileSet = user.Tile.GetNearby(radius).Where(tile => GetSquareDistance(user.Tile, tile) <= radius * radius);
            new GeomancyField(user.World, user.Tile, tileSet, 50);
            yield return user.WaitSome(50);
            foreach (Creature creature in user.World.Entities.ToList())
            {
                creature.AddStatusEffect(new Geomancy(user)
                {
                    Buildup = 1.0,
                    Duration = new Slider(float.PositiveInfinity),
                });
                yield return user.WaitSome(5);
            }

            yield return user.WaitSome(20);
        }
    }
}
