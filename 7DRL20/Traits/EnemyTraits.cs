using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;

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

    class TraitDeathThroesCrimson : TraitDeathThroes
    {
        public TraitDeathThroesCrimson() : base("death_throes_crimson", "Crimson Throes", $"Explodes on death if slashed, dealing {Element.Dark.FormatString} and {Element.Fire.FormatString} damage.", new Color(192,0,0))
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
}
