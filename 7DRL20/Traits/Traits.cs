using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.Traits
{
    class Trait : IEffectHolder
    {
        public ReusableID ObjectID
        {
            get;
            private set;
        }

        public string Name;
        public string Description;

        public Trait(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public static Trait Splintering = new TraitSplintering();
        public static Trait Holy = new TraitHoly();
        public static Trait Unstable = new TraitUnstable();
        public static Trait Softy = new TraitSofty();
        public static Trait Fragile = new TraitFragile();
        public static Trait Crumbling = new TraitCrumbling();
        public static Trait Pulverizing = new TraitPulverizing();
        public static Trait Alien = new TraitAlien();
        public static Trait Sharp = new TraitSharp();
        public static Trait Stiff = new TraitStiff();
        public static Trait Fuming = new TraitFuming();
        public static Trait Poxic = new TraitPoxic();
        public static Trait Slaughtering = new TraitSlaughtering();
        public static Trait LifeSteal = new TraitLifeSteal();
    }

    class TraitSplintering : Trait
    {
        public TraitSplintering() : base("Splintering", "Deals some damage to surrounding enemies.")
        {
            Effect.Apply(new OnStartAttack(this, UndeadKiller));
        }

        public IEnumerable<Wait> UndeadKiller(Attack attack)
        {
            var isUndead = attack.Defender.HasStatusEffect(x => x is Undead);
            if (isUndead)
            {
                attack.Damage *= 1.5f;
            }

            yield return Wait.NoWait;
        }
    }

    class TraitHoly : Trait
    {
        public TraitHoly() : base("Holy", "Extra damage to undead.")
        {
            Effect.Apply(new OnStartAttack(this, UndeadKiller));
        }

        public IEnumerable<Wait> UndeadKiller(Attack attack)
        {
            var isUndead = attack.Defender.HasStatusEffect(x => x is Undead);
            if (isUndead)
            {
                attack.Damage *= 1.5f;
            }

            yield return Wait.NoWait;
        }
    }

    class TraitUnstable : Trait
    {
        Random Random = new Random();

        public TraitUnstable() : base("Unstable", "Causes random explosions.")
        {
            Effect.Apply(new OnAttack(this, ExplodeAttack));
            Effect.Apply(new OnMine(this, ExplodeMine));
        }

        public IEnumerable<Wait> ExplodeAttack(Attack attack)
        {
            var attacker = attack.Attacker;
            if (Random.NextDouble() < 0.3 && attack.Defender is Creature defender)
            {
                new FireExplosion(defender.World, new Vector2(defender.X * 16 + 8, defender.Y * 18 + 8), Vector2.Zero, 0, 15);
                //attacker.TakeDamage(5, Element.Fire);
                //defender.TakeDamage(5, Element.Fire);
            }

            yield return Wait.NoWait;
        }

        public IEnumerable<Wait> ExplodeMine(MineEvent mine)
        {
            if (mine.Success && Random.NextDouble() < 0.3 && mine.Mineable is Tile tile)
            {
                new FireExplosion(mine.Miner.World, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8), Vector2.Zero, 0, 15);
                //mine.Miner.TakeDamage(5, Element.Fire);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitSofty : Trait
    {
        public TraitSofty() : base("Softy", "Breaking rock restores some HP.")
        {
            Effect.Apply(new OnMine(this, SoftyHeal));
        }

        public IEnumerable<Wait> SoftyHeal(MineEvent mine)
        {
            if (mine.Mineable is Tile tile)
            {
                if (mine.RequiredMiningLevel <= 1 && mine.Success)
                    mine.Miner.Heal(5);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitFragile : Trait
    {
        public TraitFragile() : base("Fragile", "Cracks nearby rock.")
        {
            Effect.Apply(new OnMine(this, Fracture));
        }

        public IEnumerable<Wait> Fracture(MineEvent mine)
        {
            if (mine.Mineable is Tile tile && mine.ReactionLevel <= 0 && mine.Success)
            {
                List<Wait> waitForMining = new List<Wait>();
                foreach (var neighbor in tile.GetAdjacentNeighbors().OfType<IMineable>())
                {
                    if (MineEvent.Random.NextDouble() < 0.7)
                    {
                        MineEvent fracture = new MineEvent(mine.Miner, mine.Pickaxe)
                        {
                            ReactionLevel = mine.ReactionLevel + 1
                        };
                        waitForMining.Add(neighbor.Mine(fracture));
                    }
                }
                yield return new WaitAll(waitForMining);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitCrumbling : Trait
    {
        public TraitCrumbling() : base("Crumbling", "Destroys lower level rock faster.")
        {
            Effect.Apply(new OnStartMine(this, Crumble));
        }

        public IEnumerable<Wait> Crumble(MineEvent mine)
        {
            var miningLevel = mine.Miner.GetStat(Stat.MiningLevel);
            double delta = miningLevel - mine.ReactionLevel;
            mine.Speed *= (1 + 0.15 * Math.Max(delta, 0));

            yield return Wait.NoWait;
        }
    }

    class TraitPulverizing : Trait
    {
        public TraitPulverizing() : base("Pulverizing", "No mining drops.")
        {
            Effect.Apply(new OnStartMine(this, Pulverize));
        }

        public IEnumerable<Wait> Pulverize(MineEvent mine)
        {
            var miningLevel = mine.Miner.GetStat(Stat.MiningLevel);
            double delta = miningLevel - mine.ReactionLevel;
            mine.LootFunction = (miner) => { };

            yield return Wait.NoWait;
        }
    }

    class TraitAlien : Trait
    {
        public TraitAlien() : base("Alien", "Randomize stats.")
        {
            Effect.Apply(new EffectStat.Randomized(this, Stat.Attack, -5, 20));
            Effect.Apply(new EffectStatPercent.Randomized(this, Stat.MiningSpeed, -0.5, 2.0));
        }
    }

    class TraitSharp : Trait
    {
        public TraitSharp() : base("Sharp", "Causes bleeding.")
        {
            Effect.Apply(new OnStartAttack(this, Bleed));
        }

        public IEnumerable<Wait> Bleed(Attack attack)
        {
            attack.StatusEffects.Add(new BleedLesser() { Buildup = 0.3, Duration = new Slider(20) });
            attack.StatusEffects.Add(new BleedGreater() { Buildup = 0.1, Duration = new Slider(10) });

            yield return Wait.NoWait;
        }
    }

    class TraitStiff : Trait
    {
        public TraitStiff() : base("Stiff", "Reduce damage taken.")
        {
            Effect.Apply(new OnStartDefend(this, Stiff));
        }

        public IEnumerable<Wait> Stiff(Attack attack)
        {
            attack.Defender.AddStatusEffect(new DefenseUp() { Buildup = 0.4, Duration = new Slider(10) });

            yield return Wait.NoWait;
        }
    }

    class TraitFuming : Trait
    {
        Random Random = new Random();

        public TraitFuming() : base("Fuming", "Sometimes produces smoke cloud.")
        {
            Effect.Apply(new OnAttack(this, EmitSmoke));
        }

        public IEnumerable<Wait> EmitSmoke(Attack attack)
        {
            var subject = attack.Defender;
            if (Random.NextDouble() < 0.2 && subject is Creature creature)
                new Smoke(creature.World, new Vector2(creature.X * 16 + 8, creature.Y * 18 + 8), Vector2.Zero, 0, 15);

            yield return Wait.NoWait;
        }
    }

    class TraitPoxic : Trait
    {
        public TraitPoxic() : base("Poxic", "Sometimes turns enemies into slime.")
        {
            Effect.Apply(new OnStartAttack(this, Slime));
        }

        public IEnumerable<Wait> Slime(Attack attack)
        {
            attack.StatusEffects.Add(new Slimed(attack.Attacker) { Buildup = 0.4 });

            yield return Wait.NoWait;
        }
    }

    class TraitSlaughtering : Trait
    {

        public TraitSlaughtering() : base("Slaughtering", "More drops, but no experience.")
        {
            //TODO
        }
    }

    class TraitLifeSteal : Trait
    {
        Random Random = new Random();

        public TraitLifeSteal() : base("Life Steal", "Sometimes steal life on attack.")
        {
            Effect.Apply(new OnAttack(this, LifeSteal));
        }

        public IEnumerable<Wait> LifeSteal(Attack attack)
        {
            var totalDamage = attack.FinalDamage.Sum(dmg => dmg.Value);
            if (Random.NextDouble() < 0.5 && totalDamage > 0)
            {
                attack.Attacker.Heal(totalDamage * 0.2);
            }

            yield return Wait.NoWait;
        }
    }
}
