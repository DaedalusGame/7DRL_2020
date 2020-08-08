using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
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
        public Color Color;

        public Trait(string name, string description, Color color)
        {
            ObjectID = EffectManager.NewID(this);
            Name = name;
            Description = description;
            Color = color;
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public static Trait Splintering = new TraitSplintering();
        public static Trait Holy = new TraitHoly();
        public static Trait Spotlight = new TraitSpotlight();
        public static Trait Unstable = new TraitUnstable();
        public static Trait Softy = new TraitSofty();
        public static Trait FrothingBlast = new TraitFrothingBlast();
        public static Trait Fragile = new TraitFragile();
        public static Trait Crumbling = new TraitCrumbling();
        public static Trait Pulverizing = new TraitPulverizing();
        public static Trait MeteorBash = new TraitMeteorBash();
        public static Trait Alien = new TraitAlien();
        public static Trait Sharp = new TraitSharp();
        public static Trait Stiff = new TraitStiff();
        public static Trait BloodShield = new TraitBloodShield();
        public static Trait Fuming = new TraitFuming();
        public static Trait Poxic = new TraitPoxic();
        public static Trait SlimeEater = new TraitSlimeEater();
        public static Trait SludgeArmor = new TraitSludgeArmor();
        public static Trait Slaughtering = new TraitSlaughtering();
        public static Trait LifeSteal = new TraitLifeSteal();

        public static Trait Undead = new TraitUndead();
        public static Trait SplitGreenSlime = new TraitSplitGreenSlime();
        public static Trait DeathThroesCrimson = new TraitDeathThroesCrimson();
    }

    class TraitSplintering : Trait
    {
        public TraitSplintering() : base("Splintering", "Deals some damage to surrounding enemies.", new Color(235, 235, 207))
        {
            Effect.Apply(new OnAttack(this, UndeadKiller));
        }

        public IEnumerable<Wait> UndeadKiller(Attack attack)
        {
            var isUndead = attack.Defender.HasFamily(Family.Undead);
            if (isUndead)
            {
                attack.Damage *= 1.5f;
            }

            yield return Wait.NoWait;
        }
    }

    class TraitHoly : Trait
    {
        public TraitHoly() : base("Holy", "Extra damage to undead.", new Color(255, 250, 155))
        {
            Effect.Apply(new OnStartAttack(this, UndeadKiller));
        }

        public IEnumerable<Wait> UndeadKiller(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);
            var isUndead = attack.Defender.HasFamily(Family.Undead);
            if (isUndead)
            {
                attack.Damage *= 1 + traitLvl * 0.5f;
            }

            yield return Wait.NoWait;
        }
    }

    class TraitSpotlight : Trait
    {
        public TraitSpotlight() : base("Spotlight", "Attacking undead take holy damage.", new Color(155, 255, 242))
        {
            Effect.Apply(new OnDefend(this, OnDefend));
        }

        public IEnumerable<Wait> OnDefend(Attack attack)
        {
            if (attack.ExtraEffects.Any(effect => effect is AttackPhysical))
            {
                int traitLvl = attack.Defender.GetTrait(this);

                if (attack.Attacker.HasFamily(Family.Undead))
                {
                    attack.Attacker.TakeDamage(10 * traitLvl, Element.Holy);
                }
            }

            yield return Wait.NoWait;
        }
    }

    class TraitUnstable : Trait
    {
        Random Random = new Random();

        public TraitUnstable() : base("Unstable", "Causes random explosions.", new Color(255, 64, 16))
        {
            Effect.Apply(new OnAttack(this, ExplodeAttack));
            Effect.Apply(new OnMine(this, ExplodeMine));
        }

        public IEnumerable<Wait> ExplodeAttack(Attack attack)
        {
            var attacker = attack.Attacker;
            int traitLvl = attacker.GetTrait(this);
            if (Random.NextDouble() < traitLvl * 0.3 && attack.Defender is Creature defender)
            {
                new FireExplosion(defender.World, defender.VisualTarget, Vector2.Zero, 0, 15);
                //attacker.TakeDamage(5, Element.Fire);
                //defender.TakeDamage(5, Element.Fire);
            }

            yield return Wait.NoWait;
        }

        public IEnumerable<Wait> ExplodeMine(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);
            if (mine.Success && Random.NextDouble() < 0.3 + (traitLvl - 1) * 0.4 && mine.Mineable is Tile tile)
            {
                new FireExplosion(mine.Miner.World, new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8), Vector2.Zero, 0, 15);
                //mine.Miner.TakeDamage(5, Element.Fire);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitSofty : Trait
    {
        public TraitSofty() : base("Softy", "Breaking rock restores some HP.", new Color(207, 179, 160))
        {
            Effect.Apply(new OnMine(this, SoftyHeal));
        }

        public IEnumerable<Wait> SoftyHeal(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);
            if (mine.Mineable is Tile tile)
            {
                if (mine.RequiredMiningLevel <= traitLvl && mine.Success)
                    mine.Miner.Heal(5 * traitLvl);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitFrothingBlast : Trait
    {
        Random Random = new Random();

        public TraitFrothingBlast() : base("Frothing Blast", "Create acid explosion on contact with water.", new Color(164, 247, 236))
        {
            Effect.Apply(new OnDefend(this, FrothingAttack));
            Effect.Apply(new OnTurn(this, FrothingTurn));
        }

        public IEnumerable<Wait> RoutineExplosion(Creature creature, int reactionLevel)
        {
            int n = 12;
            new AcidExplosion(creature.World, creature.VisualTarget, Vector2.Zero, 0, 20);
            yield return creature.WaitSome(10);
            new ScreenShakeRandom(creature.World, 5, 20, LerpHelper.Linear);
            for(int i = 0; i < n; i++)
            {
                float angle = i * MathHelper.TwoPi / n;
                Vector2 offset = Util.AngleToVector(angle) * 24;

                new SteamExplosion(creature.World, creature.VisualTarget + offset, Vector2.Zero, angle, 10 + Random.Next(5));
            }
            int radius = 2 * 16;
            int dryRadius = 1 * 16;
            HashSet<Tile> dryArea = new HashSet<Tile>(creature.Tiles);
            List<Creature> damageTargets = new List<Creature>();

            foreach(var tile in creature.Tile.GetNearby(creature.Mask.GetRectangle(creature.X, creature.Y), radius))
            {
                var distance = (tile.VisualTarget - creature.VisualTarget).LengthSquared();
                if (distance <= (radius + 8) * (radius + 8))
                {
                    damageTargets.AddRange(tile.Creatures);
                }
                if (distance <= (dryRadius + 8) * (dryRadius + 8))
                {
                    dryArea.Add(tile);
                }
            }

            foreach(var tile in dryArea)
            {
                if (tile is Water)
                    tile.Replace(new FloorCave());
            }

            List<Wait> waitForDamage = new List<Wait>();
            foreach(var target in damageTargets.Distinct().Shuffle(Random))
            {
                creature.Attack(target, 0, 0, (a,b) => ExplosionAttack(a,b,reactionLevel));
                waitForDamage.Add(target.CurrentAction);
            }
            yield return new WaitAll(waitForDamage);
        }

        private static Attack ExplosionAttack(Creature user, IEffectHolder target, int reactionLevel)
        {
            Attack attack = new Attack(user, target);
            attack.ReactionLevel = reactionLevel;
            attack.Elements.Add(Element.Steam, 1.0);
            return attack;
        }

        public IEnumerable<Wait> FrothingAttack(Attack attack)
        {
            int traitLvl = attack.Defender.GetTrait(this);
            
            if(attack.Defender is Creature creature && attack.FinalDamage.Any(x => x.Key == Element.Water))
            {
                yield return creature.WaitSome(10);
                yield return Scheduler.Instance.RunAndWait(RoutineExplosion(creature, attack.ReactionLevel + 1));
            }

            yield return Wait.NoWait;
        }

        public IEnumerable<Wait> FrothingTurn(TurnEvent turn)
        {
            int traitLvl = turn.Creature.GetTrait(this);

            Creature creature = turn.Creature;

            if(creature.Tiles.Any(x => x is Water))
            {
                yield return creature.WaitSome(10);
                yield return Scheduler.Instance.RunAndWait(RoutineExplosion(creature, 0));
            }

            yield return Wait.NoWait;
        }
    }

    class TraitFragile : Trait
    {
        public TraitFragile() : base("Fragile", "Cracks nearby rock.", new Color(181, 230, 193))
        {
            Effect.Apply(new OnMine(this, Fracture));
        }

        public IEnumerable<Wait> Fracture(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);
            if (mine.Mineable is Tile tile && mine.ReactionLevel <= 0 && mine.Success)
            {
                yield return new WaitTime(3);
                new SeismSmall(mine.Miner.World, tile, 15);
                List<Wait> waitForMining = new List<Wait>();
                foreach (var neighbor in tile.GetAdjacentNeighbors().OfType<IMineable>())
                {
                    if (MineEvent.Random.NextDouble() < 0.7 + (traitLvl - 1) * 0.1)
                    {
                        MineEvent fracture = new MineEvent(mine.Miner, mine.Pickaxe, 1000)
                        {
                            ReactionLevel = mine.ReactionLevel + 1
                        };
                        waitForMining.Add(neighbor.Mine(fracture));
                    }
                }
                mine.AddWait(new WaitAll(waitForMining));
            }

            yield return Wait.NoWait;
        }
    }

    class TraitCrumbling : Trait
    {
        public TraitCrumbling() : base("Crumbling", "Destroys lower level rock faster.", new Color(171, 184, 194))
        {
            Effect.Apply(new OnStartMine(this, Crumble));
        }

        public IEnumerable<Wait> Crumble(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);

            var miningLevel = mine.Miner.GetStat(Stat.MiningLevel);
            double delta = miningLevel - mine.ReactionLevel;
            mine.Speed *= (1 + traitLvl * 0.15 * Math.Max(delta, 0));

            yield return Wait.NoWait;
        }
    }

    class TraitPulverizing : Trait
    {
        public TraitPulverizing() : base("Pulverizing", "No mining drops.", new Color(194, 172, 172))
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

    class TraitMeteorBash : Trait
    {
        public TraitMeteorBash() : base("Meteor Bash", "Extra attack with shield, deals damage based on defense.", new Color(194, 172, 172))
        {
            Effect.Apply(new OnAttack(this, MeteorBash));
        }

        public IEnumerable<Wait> MeteorBash(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            if (attack.Defender is Creature targetCreature && attack.ReactionLevel <= 0 && targetCreature.CurrentHP > 0 && attack.ExtraEffects.Any(effect => effect is AttackPhysical)) {
                var bullet = new BulletRock(attack.Attacker.World, SpriteLoader.Instance.AddSprite("content/rock_big"), attack.Attacker.VisualTarget, Material.Meteorite.ColorTransform, 0.1f, 10);
                bullet.Move(targetCreature.VisualTarget, 10);
                yield return attack.Attacker.WaitSome(10);
                new FireExplosion(targetCreature.World, targetCreature.VisualTarget, Vector2.Zero, 0, 20);
                Point offset = attack.Attacker.Facing.ToOffset();
                attack.Attacker.Attack(targetCreature, offset.X, offset.Y, MeteorAttack);
                yield return targetCreature.CurrentAction;
            }

            yield return Wait.NoWait;
        }

        private static Attack MeteorAttack(Creature user, IEffectHolder target)
        {
            Attack attack = new Attack(user, target);
            attack.SetParameters(user.GetStat(Stat.Defense), 0.25, 1);
            attack.Elements.Add(Element.Bludgeon, 1.0);
            return attack;
        }
    }


    class TraitAlien : Trait
    {
        public TraitAlien() : base("Alien", "Randomize stats.", new Color(204, 91, 182))
        {
            Effect.Apply(new EffectStat.Randomized(this, Stat.Attack, -5, 20));
            Effect.Apply(new EffectStatPercent.Randomized(this, Stat.MiningSpeed, -0.5, 2.0));
        }
    }

    class TraitSharp : Trait
    {
        public TraitSharp() : base("Sharp", "Causes bleeding.", new Color(192, 0, 0))
        {
            Effect.Apply(new OnStartAttack(this, Bleed));
        }

        public IEnumerable<Wait> Bleed(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            attack.StatusEffects.Add(new BleedLesser() { Buildup = traitLvl * 0.3, Duration = new Slider(20) });
            attack.StatusEffects.Add(new BleedGreater() { Buildup = traitLvl * 0.1, Duration = new Slider(10) });

            yield return Wait.NoWait;
        }
    }

    class TraitStiff : Trait
    {
        public TraitStiff() : base("Stiff", "Reduce damage taken.", new Color(192, 192, 192))
        {
            Effect.Apply(new OnStartDefend(this, Stiff));
        }

        public IEnumerable<Wait> Stiff(Attack attack)
        {
            int traitLvl = attack.Defender.GetTrait(this);

            attack.Defender.AddStatusEffect(new DefenseUp() { Buildup = traitLvl * 0.4, Duration = new Slider(10) });

            yield return Wait.NoWait;
        }
    }

    class TraitBloodShield : Trait
    {
        public TraitBloodShield() : base("Blood Shield", "Attackers take pierce damage and start bleeding.", new Color(192, 0, 0))
        {
            Effect.Apply(new OnStartDefend(this, Stiff));
        }

        public IEnumerable<Wait> Stiff(Attack attack)
        {
            if (attack.ExtraEffects.Any(effect => effect is AttackPhysical))
            {
                int traitLvl = attack.Defender.GetTrait(this);

                attack.Attacker.TakeDamage(5 * traitLvl, Element.Pierce);
                attack.Attacker.AddStatusEffect(new BleedLesser() { Buildup = traitLvl * 0.4, Duration = new Slider(30) });
                attack.Attacker.AddStatusEffect(new BleedGreater() { Buildup = traitLvl * 0.1, Duration = new Slider(20) });
            }
            yield return Wait.NoWait;
        }
    }

    class TraitFuming : Trait
    {
        Random Random = new Random();

        public TraitFuming() : base("Fuming", "Sometimes produces smoke cloud.", new Color(255,255,255))
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
        public TraitPoxic() : base("Poxic", "Sometimes turns enemies into slime.", new Color(206, 221, 159))
        {
            Effect.Apply(new OnStartAttack(this, Slime));
        }

        public IEnumerable<Wait> Slime(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            if (!attack.Defender.HasFamily(Family.Slime))
                attack.StatusEffects.Add(new Slimed(attack.Attacker) { Buildup = 0.4 + (traitLvl - 1) * 0.1 });

            yield return Wait.NoWait;
        }
    }

    class TraitSlimeEater : Trait
    {
        public TraitSlimeEater() : base("Slime Eater", "Devour slime for health.", new Color(206, 221, 159))
        {
            Effect.Apply(new OnAttack(this, Devour));
        }

        public IEnumerable<Wait> Devour(Attack attack)
        {
            Creature attacker = attack.Attacker;
            IEffectHolder defender = attack.Defender;

            int traitLvl = attacker.GetTrait(this);

            if (defender is Creature targetCreature && defender.HasFamily(Family.Slime) && targetCreature.CurrentHP / targetCreature.GetStat(Stat.HP) <= traitLvl * 0.2)
            {
                attacker.Heal(20 * traitLvl);
                targetCreature.TakeDamage(targetCreature.GetStat(Stat.HP), Element.Healing, true);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitSludgeArmor : Trait
    {
        public TraitSludgeArmor() : base("Sludge Armor", "When hit by slime, reduce status buildup.", new Color(206, 221, 159))
        {
            Effect.Apply(new OnStartDefend(this, Armor));
        }

        public IEnumerable<Wait> Armor(Attack attack)
        {
            Creature attacker = attack.Attacker;
            IEffectHolder defender = attack.Defender;

            int traitLvl = defender.GetTrait(this);

            if (attacker.HasFamily(Family.Slime))
            {
                foreach(var status in attack.StatusEffects)
                {
                    status.Buildup -= 0.3 * traitLvl;
                }
                attack.StatusEffects.RemoveAll(x => x.Buildup <= 0);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitSlaughtering : Trait
    {

        public TraitSlaughtering() : base("Slaughtering", "More drops, but no experience.", new Color(128, 0, 0))
        {
            //TODO
        }
    }

    class TraitLifeSteal : Trait
    {
        Random Random = new Random();

        public TraitLifeSteal() : base("Life Steal", "Sometimes steal life on attack.", new Color(128, 0, 0))
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

    class TraitUndead : Trait
    {
        Random Random = new Random();

        public TraitUndead() : base("Undead", "Healing causes damage.", new Color(128, 112, 128))
        {
            Effect.Apply(new EffectFamily(this, Family.Undead));
            Effect.Apply(new EffectStatMultiply(this, Element.Healing.DamageRate, -1));
        }
    }
}
