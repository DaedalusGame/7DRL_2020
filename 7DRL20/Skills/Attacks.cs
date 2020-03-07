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
            ShowSkill(user, 100);
            yield return user.WaitSome(50);
            user.VisualPosition = user.Slide(pos + new Vector2(offset.X * -8, offset.Y * -8), pos, LerpHelper.Linear, 10);
            yield return user.WaitSome(20);
            for(int i = 1; i <= 8; i++)
            {
                var shootTile = user.Tile.GetNeighbor(i * offset.X, i * offset.Y);
                foreach(var target in shootTile.Creatures)
                {
                    new Explosion(target.World, new Vector2(shootTile.X * 16 + 8, shootTile.Y * 16 + 8), Vector2.Zero, 15);
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
            ShowSkill(user, 100);
            yield return user.WaitSome(50);

            user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 20);
            Creature aggroTarget = null;
            if (user is Enemy enemy)
                aggroTarget = enemy.AggroTarget;
            var nearbyTiles = user.Tile.GetNearby(6).Where(tile => tile != user.Tile).Shuffle();
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
            new Lightning(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), new Vector2(shootTile.X * 16 + 8, shootTile.Y * 16 + 8), 10, 10);
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

    class SkillDive : Skill
    {
        public SkillDive() : base("Dive", "Move to tile nearby", 2, 3, float.PositiveInfinity)
        {
        }

        public override IEnumerable<Wait> RoutineUse(Creature user)
        {
            Consume();
            Vector2 pos = new Vector2(user.X * 16, user.Y * 16);
            yield return user.WaitSome(20);
            new WaterSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 12);
            user.VisualColor = user.Static(Color.Transparent);
            var nearbyTiles = user.Tile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).ToList();
            user.MoveTo(nearbyTiles.Pick(Random));
            yield return user.WaitSome(20);
            new WaterSplash(user.World, new Vector2(user.X * 16 + 8, user.Y * 16 + 8), Vector2.Zero, 12);
            user.VisualColor = user.Static(Color.White);
            yield return user.WaitSome(20);
        }
    }

    class SkillWarp : Skill
    {
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
                Consume();
                user.VisualColor = user.Flash(Color.Transparent, Color.White, 20);
                yield return user.WaitSome(20);
                user.VisualColor = user.Static(Color.Transparent);
                var nearbyTiles = enemy.AggroTarget.Tile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).ToList();
                user.MoveTo(nearbyTiles.Pick(Random));
                yield return user.WaitSome(20);
                user.VisualColor = user.Flash(Color.Transparent, Color.White, 20);
                yield return user.WaitSome(20);
            }
        }
    }

    class SkillIronMaiden : Skill
    {
        public SkillIronMaiden() : base("Iron Maiden", "Lowers enemy defense.", 2, 5, float.PositiveInfinity)
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
                ShowSkill(user, 100);
                user.FlickPose(CreaturePose.Cast, CreaturePose.Stand, 50);
                yield return user.WaitSome(50);
                enemy.AggroTarget.AddStatusEffect(new DefenseDown() {
                    Buildup = 0.4,
                    Duration = new Slider(20),
                });
                yield return user.WaitSome(20);
            }
        }
    }
}
