using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.VisualEffects;

namespace RoguelikeEngine.Traits
{
    abstract class TraitDeathThroes : Trait
    {
        public TraitDeathThroes(string id, string name, string description, Color color) : base(id, name, description, color)
        {
            Effect.Apply(new OnDeath(this, RoutineExplode));
        }

        public abstract IEnumerable<Wait> RoutineExplode(DeathEvent death);
    }

    class TraitDeathThroesSkill : TraitDeathThroes
    {
        Skill Skill;

        public TraitDeathThroesSkill(Skill skill) : base("death_throes_skill", "Skill on Death", $"Uses skill on death.", new Color(192, 0, 0))
        {
            Skill = skill;
            Skill.IgnoreCanUse = true;
        }

        public override IEnumerable<Wait> RoutineExplode(DeathEvent death)
        {
            death.Creature.Control.AddImmediateSkill(Skill);
            yield return Wait.NoWait;
        }
    }

    class TraitDeathThroesDeathGolem : TraitDeathThroes
    {
        Random Random = new Random();

        public TraitDeathThroesDeathGolem() : base("death_throes_death_golem", "Separate on Death", $"Splits into a head and body on death.", new Color(192, 0, 0))
        {
        }

        public override IEnumerable<Wait> RoutineExplode(DeathEvent death)
        {
            Creature creature = death.Creature;

            yield return creature.WaitSome(4);

            new ScreenShakeRandom(creature.World, 5, 15, LerpHelper.Linear);
            new RingExplosion(creature.World, creature.VisualTarget, (pos, vel, angle, time) => new FireExplosion(creature.World, pos, vel, angle, time), 6, 24, 10);

            //Fire horns
            var waits = new List<Wait>();
            foreach (var targetCreature in GetPossibleTargets(creature).TakeLoop(2))
            {
                waits.Add(Scheduler.Instance.RunAndWait(RoutineHorn(creature, targetCreature)));
            }

            var spawnedBody = new DeathGolemBody(creature.World);
            spawnedBody.Facing = creature.Facing;
            spawnedBody.MoveTo(creature.Tile, 0);
            spawnedBody.VisualPosition = spawnedBody.Slide(creature.VisualPosition(), spawnedBody.ActualPosition, LerpHelper.Quadratic, 30);
            spawnedBody.AddControlTurn();

            creature.VisualColor = creature.Static(Color.Transparent);

            var targetTiles = SkillUtil.GetFrontierTiles(creature).Shuffle(Random);
            foreach (var targetTile in targetTiles)
            {
                if (!targetTile.Solid && !targetTile.Creatures.Any())
                {
                    var spawnedHead = new DeathGolemHead(creature.World);
                    spawnedHead.Facing = creature.Facing;
                    spawnedHead.MoveTo(targetTile, 0);
                    spawnedHead.VisualPosition = spawnedHead.Slide(creature.VisualTarget + new Vector2(0, -8) - spawnedHead.CenterOffset, spawnedHead.ActualPosition, LerpHelper.Quadratic, 30);
                    spawnedHead.AddControlTurn();
                    break;
                }
            }

            yield return creature.WaitSome(30);
        }

        public IEnumerable<Creature> GetPossibleTargets(Creature user)
        {
            List<Creature> enemies = new List<Creature>();
            foreach (var tile in user.Tile.GetNearby(user.Mask.GetRectangle(user.X, user.Y), 5))
            {
                enemies.AddRange(tile.Creatures);
            }
            return enemies.Where(x => x != user).Distinct().Shuffle(Random);
        }

        private IEnumerable<Wait> RoutineHorn(Creature creature, Creature targetCreature)
        {
            var horn = SpriteLoader.Instance.AddSprite("content/death_golem_sickle");

            var cutter = new EnergyBall(creature.World, horn, null, creature.VisualTarget + new Vector2(0, -8), targetCreature.VisualTarget, 1.0f, MathHelper.Pi * 0.1f, 60, LerpHelper.Quadratic, 50);
            yield return new WaitTime(50);
            var wait = creature.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - creature.VisualTarget), HornAttack);
            yield return wait;
        }

        protected Attack HornAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Elements.Add(Element.Emperor, 1.0);
            return attack;
        }
    }

    class TraitDeathThroesTendril : TraitDeathThroes
    {
        public TraitDeathThroesTendril() : base("death_throes_tendril", "Acid Spurt Death Throes", $"Spills it's blood on death, dealing {Element.Acid.FormatString} damage in a 1 tile radius.", new Color(64, 64, 64))
        {
        }

        public override IEnumerable<Wait> RoutineExplode(DeathEvent death)
        {
            Creature creature = death.Creature;
            List<Wait> waits = new List<Wait>();
            foreach(var minion in creature.GetSlaves().OfType<Creature>())
            {
                var explosion = new Skills.Explosion(minion, SkillUtil.GetCircularArea(minion, 1), minion.VisualTarget);
                explosion.Attack = ExplosionAttack;
                explosion.Fault = this;
                waits.Add(explosion.Run());
            }
            yield return new WaitAll(waits);
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Fault = this;
            attack.SetParameters(attacker.GetStat(Stat.HP) * 0.5, 0, 1);
            attack.Elements.Add(Element.Acid, 1.0);
            return attack;
        }
    }

    class TraitDeathThroesFireBlast : TraitDeathThroes
    {
        public TraitDeathThroesFireBlast() : base("death_throes_fire_blast", "Fire Blast Throes", $"Explodes on death, dealing {Element.Bludgeon.FormatString} and {Element.Fire.FormatString} damage in a 2 tile radius.", new Color(255, 64, 16))
        {
        }

        public override IEnumerable<Wait> RoutineExplode(DeathEvent death)
        {
            Creature creature = death.Creature;

            new ScreenShakeRandom(creature.World, 5, 15, LerpHelper.Linear);
            new FireExplosion(creature.World, creature.VisualTarget, Vector2.Zero, 0, 30);
            yield return creature.WaitSome(4);

            new RingExplosion(creature.World, creature.VisualTarget, (pos, vel, angle, time) => new FireExplosion(creature.World, pos, vel, angle, time), 12, 24, 10);
            var explosion = new Skills.Explosion(creature, SkillUtil.GetCircularArea(creature, 2), creature.VisualTarget);
            explosion.Attack = ExplosionAttack;
            explosion.Fault = this;
            yield return explosion.Run();
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Fault = this;
            attack.SetParameters(attacker.GetStat(Stat.HP), 0, 1);
            attack.Elements.Add(Element.Bludgeon, 0.5);
            attack.Elements.Add(Element.Fire, 0.5);
            return attack;
        }
    }

    class TraitDeathThroesCrimson : TraitDeathThroes
    {
        public TraitDeathThroesCrimson() : base("death_throes_crimson", "Crimson Throes", $"Explodes on death if slashed, dealing {Element.Dark.FormatString} and {Element.Fire.FormatString} damage in a 2 tile radius.", new Color(192,0,0))
        {
        }

        public override IEnumerable<Wait> RoutineExplode(DeathEvent death)
        {
            Creature creature = death.Creature;

            if (this.GetDamage(Element.Slash) > 0)
            {
                new ScreenShakeRandom(creature.World, 5, 15, LerpHelper.Linear);
                new FireExplosion(creature.World, creature.VisualTarget, Vector2.Zero, 0, 30);
                yield return creature.WaitSome(4);

                new RingExplosion(creature.World, creature.VisualTarget, (pos, vel, angle, time) => new BloodExplosion(creature.World, pos, vel, angle, time), 12, 24, 10);
                var explosion = new Skills.Explosion(creature, SkillUtil.GetCircularArea(creature, 2), creature.VisualTarget);
                explosion.Attack = ExplosionAttack;
                explosion.Fault = this;
                yield return explosion.Run();
            }
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Fault = this;
            attack.SetParameters(attacker.GetStat(Stat.HP) * 0.5, 0, 1);
            attack.Elements.Add(Element.Fire, 0.5);
            attack.Elements.Add(Element.Dark, 0.5);
            return attack;
        }
    }

    class TraitDeathThroesBlood : TraitDeathThroes
    {
        Element Element;

        public TraitDeathThroesBlood(string id, Element element, Color color) : base(id, "Blood Throes", "", color)
        {
            Element = element;
            Description = new DynamicString(() => $"Spills its blood on death, dealing {Element} damage in a 1 tile radius.");
        }

        public override IEnumerable<Wait> RoutineExplode(DeathEvent death)
        {
            Creature creature = death.Creature;

            var explosion = new Skills.Explosion(creature, SkillUtil.GetCircularArea(creature, 1), creature.VisualTarget);
            explosion.Attack = ExplosionAttack;
            explosion.Fault = this;
            yield return explosion.Run();
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Fault = this;
            attack.SetParameters(attacker.GetStat(Stat.HP) * 0.5, 0, 1);
            attack.Elements.Add(Element, 1.0);
            return attack;
        }
    }

    abstract class TraitSplit : Trait
    {
        protected Random Random = new Random();

        protected int Spawns;

        public TraitSplit(string id, string name, string description, Color color) : base(id, name, description, color)
        {
            Effect.Apply(new OnDeath(this, RoutineSplit));
        }

        protected abstract IEnumerable<Tile> GetSpawnLocations(Creature creature);

        protected abstract void OnBlocked(Creature spawner, Creature spawned, Tile tile);

        protected abstract Creature SpawnCreature(Creature creature, Tile tile);

        private IEnumerable<Wait> RoutineSplit(DeathEvent death)
        {
            Creature creature = death.Creature;
            List<Wait> waits = new List<Wait>();
            foreach (var neighbor in GetSpawnLocations(creature).Take(Spawns))
            {
                waits.Add(Scheduler.Instance.RunAndWait(RoutineSplitBranch(creature, neighbor)));
            }
            yield return new WaitAll(waits);
        }

        private IEnumerable<Wait> RoutineSplitBranch(Creature creature, Tile neighbor)
        {
            var spawned = SpawnCreature(creature, neighbor);
            spawned.MoveTo(creature.Tile, 0);
            spawned.AddControlTurn();
            spawned.MoveTo(neighbor, 20);
            yield return spawned.WaitSome(20);
            if (neighbor.Solid || neighbor.Creatures.Any(x => x != spawned))
            {
                OnBlocked(creature, spawned, neighbor);
            }
        }
    }

    class TraitSplitGreenSlime : TraitSplit
    {
        public TraitSplitGreenSlime() : base("split_green_amoeba", "Split", "Splits into Green Amoebas on death.", new Color(206, 221, 159))
        {
            Spawns = 4;
        }

        protected override IEnumerable<Tile> GetSpawnLocations(Creature creature)
        {
            return creature.Tile.GetAdjacentNeighbors().Shuffle(Random);
        }

        protected override Creature SpawnCreature(Creature creature, Tile tile)
        {
            return new GreenAmoeba(creature.World, 10);
        }

        protected override void OnBlocked(Creature spawner, Creature spawned, Tile tile)
        {
            new GreenBlobPop(spawned.World, spawned.VisualTarget, Vector2.Zero, 0, 10);
            spawned.Destroy();
        }

        
    }

    class TraitOverclock : Trait
    {
        public TraitOverclock() : base("overclock", "Overclock", $"When Oiled, gain extra turns.", new Color(255, 64, 16))
        {
            Effect.Apply(new EffectStat.Special(this, Stat.Speed, GetSpeedBonus, GetSpeedBonusLine, "OverclockBonus"));
        }

        private double GetSpeedBonus(Effect effect, IEffectHolder holder)
        {
            return holder.HasStatusEffect<Oiled>() ? holder.GetTrait(this) : 0;
        }

        private void GetSpeedBonusLine(ref string statBlock, IEnumerable<Effect> equalityGroup, bool isBase)
        {
            var baseText = isBase ? "Base" : string.Empty;
            var myStat = Stat.Speed;
            var amount = 1;
            if (amount != 0)
                statBlock += $"{Game.FormatStat(myStat)} {myStat.Name} {amount.ToString("+0;-#")} {baseText} (if Oiled)\n";
        }
    }

    class TraitLightningField : Trait
    {
        struct Ray {
            public Point Position;
            public Point Direction;
            public Point Origin => Position + Direction;

            public Ray(Point position, Point direction)
            {
                Position = position;
                Direction = direction;
            }

            public override int GetHashCode()
            {
                return Origin.GetHashCode() + Direction.GetHashCode() * 37;
            }

            public override bool Equals(object obj)
            {
                if(obj is Ray other)
                {
                    return Origin.Equals(other.Origin) && Direction.Equals(other.Direction);
                }
                return base.Equals(obj);
            }

            public override string ToString()
            {
                return Position + " " + Direction;
            }
        }

        public TraitLightningField() : base("lightning_field", "Lightning Field", $"Every turn, arc to nearby creatures of the same type, dealing {Element.Thunder.FormatString} damage to all targets in the arc.", new Color(128, 112, 128))
        {
            Effect.Apply(new OnTurn(this, RoutineField));
        }

        private IEnumerable<Wait> RoutineField(TurnEvent turn)
        {
            Creature user = turn.Creature;
            var rays = GetRays(user);

            var tileRays = rays.Select(r => GetBranch(user, r, 8)).Where(r => r != null);
            var waits = new List<Wait>();

            int traitLvl = user.GetTrait(this);

            foreach (var tileRay in tileRays.Take(traitLvl))
            {
                waits.Add(RoutineBranch(user, tileRay));
                yield return new WaitTime(3);
            }

            yield return new WaitAll(waits);
        }

        private Wait RoutineBranch(Creature user, IList<Tile> tileRay)
        {
            var lightning = SpriteLoader.Instance.AddSprite("content/lightning");
            var startTile = tileRay.First();
            var endTile = tileRay.Last();
            new LightningSpark(user.World, lightning, startTile.VisualTarget, endTile.VisualTarget, 5);
            //new Arc(user.World, lightning, () => startTile.VisualTarget, () => endTile.VisualTarget, Vector2.Zero, Vector2.Zero, 1, 20);

            var creatures = tileRay.SelectMany(t => t.Creatures).Distinct();
            var waits = new List<Wait>();
            foreach (var targetCreature in creatures.Where(c => !SameCreatureType(user, c)))
            {
                var wait = user.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - user.VisualTarget), BeamAttack);
                waits.Add(wait);
            }
            return new WaitAll(waits);
        }

        private static Attack BeamAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.Elements.Add(Element.Thunder, 1.0);
            return attack;
        }

        private IEnumerable<Ray> GetRays(Creature user)
        {
            List<Ray> rays = new List<Ray>();

            foreach(var point in user.Mask)
            {
                rays.Add(new Ray(point, new Point(+1, 0)));
                rays.Add(new Ray(point, new Point(-1, 0)));
                rays.Add(new Ray(point, new Point(0, +1)));
                rays.Add(new Ray(point, new Point(0, -1)));
                rays.Add(new Ray(point, new Point(+1, +1)));
                rays.Add(new Ray(point, new Point(-1, -1)));
                rays.Add(new Ray(point, new Point(-1, +1)));
                rays.Add(new Ray(point, new Point(+1, -1)));
            }

            rays.RemoveAll(p => user.Mask.PointLookup.Contains(p.Position + p.Direction));

            return rays.GroupBy(p => p.Origin).Select(FilterRay).Where(x => x.HasValue).Select(x => x.Value).ToList();
        }

        private Ray? FilterRay(IEnumerable<Ray> rays)
        {
            var straightRays = rays.Where(r => r.Direction.X == 0 || r.Direction.Y == 0);
            var diagonalRays = rays.Where(r => r.Direction.X != 0 && r.Direction.Y != 0);

            if (straightRays.Count() == 1)
                return straightRays.First();
            else if (diagonalRays.Count() == 1)
                return diagonalRays.First();
            else
                return null;
        }

        private IList<Tile> GetBranch(Creature user, Ray ray, int distance)
        {
            List<Tile> tiles = new List<Tile>();
            Tile origin = user.Tile.GetNeighbor(ray.Position.X, ray.Position.Y);
            tiles.Add(origin);
            for (int i = 1; i < distance; i++)
            {
                Tile currentTile = origin.GetNeighbor(ray.Direction.X * i, ray.Direction.Y * i);
                tiles.Add(currentTile);
                if (i > 1 && currentTile.Creatures.Any(c => SameCreatureType(user, c)))
                    return tiles;
                if (currentTile.Solid || currentTile.Creatures.Contains(user))
                    break;
            }
            return null;
        }

        private static bool SameCreatureType(Creature a, Creature b)
        {
            return a.GetType() == b.GetType(); //TODO: Change this, this is horrid.
        }
    }

    class TraitBroil : Trait
    {
        public TraitBroil() : base("broil", "Broil", $"When targetted by an attack that would deal {Element.Fire.FormatString} damage, activate each Boiling status effect once.", new Color(255, 64, 16))
        {
            Effect.Apply(new OnDefend(this, RoutineBroil));
        }

        private IEnumerable<Wait> RoutineBroil(Attack attack)
        {
            if(attack.SplitElements.Contains(Element.Fire))
            {
                foreach(var statusEffect in attack.Defender.GetStatusEffects().OfType<Boiling>())
                {
                    statusEffect.Broil();
                }
            }
            yield return Wait.NoWait;
        }
    }

    class TraitAcidBlood : Trait
    {
        public TraitAcidBlood() : base("acid_blood", "Acid Blood", $"When damaged, splash acid on attacker, dealing {Element.Acid.FormatString} damage.", new Color(227, 255, 34))
        {
            Effect.Apply(new OnDefend(this, RoutineAcidSplash));
        }

        private IEnumerable<Wait> RoutineAcidSplash(Attack attack)
        {
            if (attack.Defender is Creature defender && attack.ExtraEffects.Any(effect => effect is AttackPhysical))
            {
                int traitLvl = attack.Defender.GetTrait(this);

                defender.Attack(attack.Attacker, Vector2.Zero, (a,b) => AcidAttack(a, b, traitLvl * 20));
            }

            yield return Wait.NoWait;
        }

        private Attack AcidAttack(Creature attacker, IEffectHolder defender, double force)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(force, 0, 1);
            attack.Elements.Add(Element.Acid, 1.0);
            attack.ReactionLevel = 1;
            return attack;
        }
    }

    class TraitDeathMachine : Trait
    {
        Random Random = new Random();

        public TraitDeathMachine() : base("death_machine", "Death Machine", $"Every turn, one random skill resets its cooldown.", new Color(130, 100, 100))
        {
            Effect.Apply(new OnTurn(this, RoutineResetCooldown));
        }

        private IEnumerable<Wait> RoutineResetCooldown(TurnEvent turn)
        {
            if(turn.Creature is Enemy enemy && enemy.Skills.Any())
            {
                var skill = enemy.Skills.Pick(Random);
                skill.ResetCooldown();
            }

            yield return Wait.NoWait;
        }
    }
}
