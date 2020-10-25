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
    class Trait : IEffectHolderPersistent
    {
        public static List<Trait> AllTraits = new List<Trait>();

        public ReusableID ObjectID
        {
            get;
            private set;
        }

        public int Index;
        public string ID;
        public string Name;
        public CalculatedString Description;
        public Color Color;

        public Trait(string id, string name, string description, Color color)
        {
            Index = AllTraits.Count;
            ObjectID = EffectManager.SetID(this);
            ID = id;
            Name = name;
            Description = description;
            Color = color;
            AllTraits.Add(this);
        }

        public IEnumerable<T> GetEffects<T>() where T : Effect
        {
            return EffectManager.GetEffects<T>(this);
        }

        public static Trait GetTrait(string id)
        {
            return AllTraits.Find(x => x.ID == id);
        }

        public static Trait Splintering = new TraitSplintering();
        public static Trait Holy = new TraitHoly();
        public static Trait Spotlight = new TraitSpotlight();
        public static Trait Unstable = new TraitUnstable();
        public static Trait Charged = new TraitCharged();
        public static Trait Discharge = new TraitDischarge();
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
        public static Trait Sparking = new TraitSparking();

        public static Trait Undead = new TraitUndead();
        public static Trait SplitGreenSlime = new TraitSplitGreenSlime();
        public static Trait DeathThroesCrimson = new TraitDeathThroesCrimson();
        public static Trait BloodThroesAcid = new TraitDeathThroesBlood(Element.Acid);
        public static Trait Broiling = new TraitBroil();
        public static Trait Overclock = new TraitOverclock();
        public static Trait AcidBlood = new TraitAcidBlood();

        public static Trait Water = new TraitWater();
        public static Trait Lava = new TraitLava();
        public static Trait SuperLava = new TraitSuperLava();
        public static Trait HyperLava = new TraitHyperLava();
        public static Trait Acid = new TraitAcid();
        public static Trait Bog = new TraitBog();
    }

    class TraitSplintering : Trait
    {
        public TraitSplintering() : base("splintering", "Splintering", "Deals some damage to surrounding enemies.", new Color(235, 235, 207))
        {
            Effect.Apply(new OnAttack(this, Splinter));
        }

        public IEnumerable<Wait> Splinter(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            if (attack.Defender is Creature targetCreature && attack.ReactionLevel == 0)
            {
                var boneShards = SpriteLoader.Instance.AddSprite("content/shards");
                IEnumerable<Tile> targetTiles = SkillUtil.GetFrontierTiles(new[] { attack.Attacker, targetCreature });
                HashSet<Creature> targets = new HashSet<Creature>();
                yield return attack.Attacker.WaitSome(10);
                foreach (Tile tile in targetTiles)
                {
                    targets.AddRange(tile.Creatures.Where(creature => creature != attack.Attacker && creature != attack.Defender));
                    new Shards(attack.Attacker.World, boneShards, Vector2.Lerp(attack.Attacker.VisualTarget, targetCreature.VisualTarget, 0.5f), tile.VisualTarget, LerpHelper.CubicOut, 10);
                }
                List<Wait> waitForDamage = new List<Wait>();
                foreach (var target in targets)
                {
                    double splinterDamage = traitLvl * 0.2 * attack.FinalDamage.Sum(x => x.Value);
                    var wait = attack.Attacker.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - attack.Attacker.VisualTarget), (a,b) => SplinterAttack(a, b, splinterDamage));
                    waitForDamage.Add(wait);
                }
                yield return new WaitAll(waitForDamage);
            }
            yield return Wait.NoWait;
        }

        private Attack SplinterAttack(Creature attacker, IEffectHolder defender, double force)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(force, 0, 1);
            attack.Elements.Add(Element.Pierce, 1.0);
            attack.ReactionLevel = 1;
            return attack;
        }
    }

    class TraitHoly : Trait
    {
        public TraitHoly() : base("holy", "Holy", "Extra damage to undead.", new Color(255, 250, 155))
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
        public TraitSpotlight() : base("spotlight", "Spotlight", $"Attacking undead take {Element.Holy.FormatString} damage.", new Color(155, 255, 242))
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

        public TraitUnstable() : base("unstable", "Unstable", "Causes random explosions.", new Color(255, 64, 16))
        {
            Effect.Apply(new OnAttack(this, ExplodeAttack));
            Effect.Apply(new OnDefend(this, ExplodeChainReact));
            Effect.Apply(new OnMine(this, ExplodeMine));
        }

        public IEnumerable<Wait> ExplodeAttack(Attack attack)
        {
            var attacker = attack.Attacker;
            var defender = attack.Defender;
            int attackerLvl = attacker.GetTrait(this);
            int defenderLvl = defender.GetTrait(this);
            int traitLvl = attackerLvl + defenderLvl;
            if (attack.Fault != this && Random.NextDouble() < traitLvl * 0.3 && attack.IsWeaponAttack() && attack.ReactionLevel < Math.Max(attackerLvl, defenderLvl))
            {
                IEnumerable<Tile> targetTiles = GetExplosionTiles(attacker);
                if (targetTiles.Any())
                {
                    var explosionTarget = targetTiles.Shuffle(Random).First();
                    yield return Scheduler.Instance.RunAndWait(RoutineExplode(attacker, explosionTarget, 1, attack.ReactionLevel));
                }
            }

            yield return Wait.NoWait;
        }

        public IEnumerable<Wait> ExplodeChainReact(Attack attack)
        {
            var attacker = attack.Attacker;
            var defender = attack.Defender;
            int attackerLvl = attacker.GetTrait(this);
            int defenderLvl = defender.GetTrait(this);
            int traitLvl = attackerLvl + defenderLvl;
            if (Random.NextDouble() < traitLvl * 0.3 && (attack.HasElement(Element.Fire) || attack.Fault != this) && attack.ReactionLevel < Math.Max(attackerLvl,defenderLvl))
            {
                IEnumerable<Tile> targetTiles = GetExplosionTiles(attacker);
                if (targetTiles.Any())
                {
                    var explosionTarget = targetTiles.Shuffle(Random).First();
                    yield return Scheduler.Instance.RunAndWait(RoutineExplode(attacker, explosionTarget, 1, attack.ReactionLevel));
                }
            }

            yield return Wait.NoWait;
        }

        public IEnumerable<Wait> ExplodeMine(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);
            if (mine.ReactionLevel == 0 && mine.Fault != this && Random.NextDouble() < traitLvl * 0.3 && mine.Mineable is Tile tile)
            {
                IEnumerable<Tile> targetTiles = GetExplosionTiles(tile);
                if (targetTiles.Any())
                {
                    var explosionTarget = targetTiles.Shuffle(Random).First();
                    yield return Scheduler.Instance.RunAndWait(RoutineExplode(mine.Miner, explosionTarget, 1, mine.ReactionLevel));
                }
            }

            yield return Wait.NoWait;
        }

        private IEnumerable<Wait> RoutineExplode(Creature attacker, Tile explosionTarget, int radius, int reactionLevel)
        {
            yield return new WaitTime(5 + Random.Next(5));
            new FireExplosion(attacker.World, explosionTarget.VisualTarget, Vector2.Zero, 0, 15);
            new ScreenShakeRandom(attacker.World, 6, 30, LerpHelper.Linear);
            var explosion = new Skills.Explosion(attacker, SkillUtil.GetCircularArea(explosionTarget, radius), explosionTarget.VisualTarget);
            explosion.Attack = (a, b) => ExplosionAttack(a, b, reactionLevel + 1);
            explosion.Fault = this;
            explosion.ReactionLevel = reactionLevel + 1;
            explosion.MiningPower = 100;
            yield return explosion.Run();
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender, int reactionLevel)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Fault = this;
            attack.ReactionLevel = reactionLevel;
            attack.SetParameters(15, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            return attack;
        }

        private IEnumerable<Tile> GetExplosionTiles(Creature creature)
        {
            HashSet<Tile> targetTiles = new HashSet<Tile>();
            targetTiles.AddRange(creature.Mask.GetFullFrontier().Select(o => creature.Tile.GetNeighbor(o.X, o.Y)));
            targetTiles.AddRange(creature.Tiles);
            return targetTiles;
        }

        private IEnumerable<Tile> GetExplosionTiles(Tile tile)
        {
            HashSet<Tile> targetTiles = new HashSet<Tile>();
            targetTiles.Add(tile);
            targetTiles.AddRange(tile.GetAllNeighbors());
            return targetTiles;
        }
    }

    class TraitTantrum : Trait
    {
        Random Random = new Random();

        public TraitTantrum() : base("tantrum", "Tantrum", "Mined rock explodes.", new Color(236, 215, 66))
        {
            Effect.Apply(new OnMine(this, ExplodeMine));
        }

        public IEnumerable<Wait> ExplodeMine(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);
            if (mine.Success && mine.Fault != this && Random.NextDouble() < traitLvl * 0.3 && mine.Mineable is Tile tile)
            {
                IEnumerable<Tile> targetTiles = GetExplosionTiles(tile);
                if (targetTiles.Any())
                {
                    var explosionTarget = targetTiles.Shuffle(Random).First();
                    yield return Scheduler.Instance.RunAndWait(RoutineExplode(mine.Miner, explosionTarget, 1, mine.ReactionLevel));
                }
            }

            yield return Wait.NoWait;
        }

        private IEnumerable<Wait> RoutineExplode(Creature attacker, Tile explosionTarget, int radius, int reactionLevel)
        {
            yield return new WaitTime(5 + Random.Next(5));
            new FireExplosion(attacker.World, explosionTarget.VisualTarget, Vector2.Zero, 0, 15);
            new ScreenShakeRandom(attacker.World, 6, 30, LerpHelper.Linear);
            var waitForDamage = new List<Wait>();
            foreach (var explosionTile in SkillUtil.GetCircularArea(explosionTarget, radius))
            {
                foreach (var targetCreature in explosionTile.Creatures)
                {
                    waitForDamage.Add(attacker.Attack(targetCreature, SkillUtil.SafeNormalize(targetCreature.VisualTarget - explosionTarget.VisualTarget), (a, b) => ExplosionAttack(a, b, reactionLevel + 1)));
                }
                if (explosionTile is IMineable mineable)
                {
                    MineEvent fracture = new MineEvent(attacker, null, 100)
                    {
                        Fault = this,
                        ReactionLevel = reactionLevel + 1
                    };
                    waitForDamage.Add(mineable.Mine(fracture));
                }
            }
            yield return new WaitAll(waitForDamage);
        }

        private Attack ExplosionAttack(Creature attacker, IEffectHolder defender, int reactionLevel)
        {
            Attack attack = new Attack(attacker, defender);
            attack.Fault = this;
            attack.ReactionLevel = reactionLevel;
            attack.SetParameters(15, 0, 1);
            attack.Elements.Add(Element.Fire, 1);
            return attack;
        }

        private IEnumerable<Tile> GetExplosionTiles(Tile tile)
        {
            HashSet<Tile> targetTiles = new HashSet<Tile>();
            targetTiles.Add(tile);
            targetTiles.AddRange(tile.GetAllNeighbors());
            return targetTiles;
        }
    }

    class TraitCharged : Trait
    {
        public TraitCharged() : base("charged", "Charged", "Arcs to nearby enemies in flight.", new Color(192, 255, 16))
        {
            Effect.Apply(new OnShoot(this, ShootArrow));
        }

        private IEnumerable<Wait> ShootArrow(ShootEvent shoot)
        {
            shoot.Projectile.ExtraEffects.Add(new Skills.ProjectileArc(ArcAttack, 4));
            return Enumerable.Empty<Wait>();
        }

        public Attack ArcAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(20, 0, 1);
            attack.Elements.Add(Element.Thunder, 1.0);
            return attack;
        }
    }

    class TraitDischarge : Trait
    {
        Random Random = new Random();

        public TraitDischarge() : base("discharge", "Discharge", "Detonates on impact.", new Color(192, 255, 16))
        {
            Effect.Apply(new OnShoot(this, ShootArrow));
        }

        private IEnumerable<Wait> ShootArrow(ShootEvent shoot)
        {
            shoot.Projectile.ExtraEffects.Add(new Skills.ProjectileImpactExplosion(ExplosionAttack, 2));
            shoot.Projectile.ExtraEffects.Add(new Skills.ProjectileImpactFunction(ImpactExplosion));
            return Enumerable.Empty<Wait>();
        }

        private IEnumerable<Wait> ImpactExplosion(Skills.Projectile projectile, Tile tile)
        {
            new FireExplosion(tile.World, tile.VisualTarget, Vector2.Zero, 0, 20);
            yield return new WaitTime(5);
            new ScreenShakeRandom(tile.World, 8, 20, LerpHelper.Linear);
            new RingExplosion(tile.World, tile.VisualTarget, (pos, vel, angle, time) => new LightningExplosion(tile.World, pos, vel, angle, time), 12, 24, 10);
        }

        public Attack ExplosionAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(400, 0, 1);
            attack.Elements.Add(Element.Thunder, 0.5);
            attack.Elements.Add(Element.Fire, 0.5);
            return attack;
        }
    }

    class TraitSofty : Trait
    {
        public TraitSofty() : base("softy", "Softy", "Breaking rock restores some HP.", new Color(207, 179, 160))
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

        public TraitFrothingBlast() : base("frothing_blast", "Frothing Blast", $"Create acid explosion on contact with water.\nExplosion does half {Element.Acid.FormatString}, half {Element.Steam.FormatString} damage.", new Color(164, 247, 236))
        {
            Effect.Apply(new OnDefend(this, FrothingAttack));
            Effect.Apply(new OnTurn(this, FrothingTurn));
        }

        public IEnumerable<Wait> RoutineExplosion(Creature creature, double force, int reactionLevel)
        {
            new AcidExplosion(creature.World, creature.VisualTarget, Vector2.Zero, 0, 20);
            yield return creature.WaitSome(10);
            new ScreenShakeRandom(creature.World, 5, 20, LerpHelper.Linear);
            new RingExplosion(creature.World, creature.VisualTarget, (pos, vel, angle, time) => new SteamExplosion(creature.World, pos, vel, angle, time), 12, 24, 10);
            int radius = 2;
            int dryRadius = 1;
            IEnumerable<Tile> dryArea = SkillUtil.GetCircularArea(creature, dryRadius);
            IEnumerable<Tile> explosionArea = SkillUtil.GetCircularArea(creature, radius);

            foreach(var tile in dryArea)
            {
                if (tile is Water)
                    tile.Replace(new FloorCave());
            }

            var explosion = new Skills.Explosion(creature, explosionArea, creature.VisualTarget);
            explosion.Fault = this;
            explosion.ReactionLevel = reactionLevel;
            explosion.Attack = (a, b) => ExplosionAttack(a, b, force, reactionLevel);
            yield return explosion.Run();
        }

        private static Attack ExplosionAttack(Creature user, IEffectHolder target, double force, int reactionLevel)
        {
            Attack attack = new Attack(user, target);
            attack.SetParameters(force, 0, 1);
            attack.ReactionLevel = reactionLevel;
            attack.Elements.Add(Element.Steam, 0.5);
            attack.Elements.Add(Element.Acid, 0.5);
            return attack;
        }

        public IEnumerable<Wait> FrothingAttack(Attack attack)
        {
            int traitLvl = attack.Defender.GetTrait(this);
            
            if(attack.Defender is Creature creature && attack.FinalDamage.Any(x => x.Key == Element.Water))
            {
                yield return creature.WaitSome(10);
                yield return Scheduler.Instance.RunAndWait(RoutineExplosion(creature, 50 * Math.Pow(2, traitLvl-1), attack.ReactionLevel + 1));
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
                yield return Scheduler.Instance.RunAndWait(RoutineExplosion(creature, 50 * Math.Pow(2, traitLvl-1), 0));
            }

            yield return Wait.NoWait;
        }
    }

    class TraitFragile : Trait
    {
        public TraitFragile() : base("fragile", "Fragile", "Cracks nearby rock.", new Color(181, 230, 193))
        {
            Effect.Apply(new OnMine(this, Fracture));
        }

        public IEnumerable<Wait> Fracture(MineEvent mine)
        {
            int traitLvl = mine.Miner.GetTrait(this);
            if (mine.Mineable is Tile tile && mine.ReactionLevel <= (traitLvl - 1) && mine.Success)
            {
                yield return new WaitTime(3);
                new SeismSmall(mine.Miner.World, tile, 15);
                List<Wait> waitForMining = new List<Wait>();
                foreach (var neighbor in tile.GetAdjacentNeighbors())
                {
                    if (neighbor is IMineable mineable && MineEvent.Random.NextDouble() < 0.7 + (traitLvl - 1) * 0.1)
                    {
                        new SeismSmall(neighbor.World, neighbor, 10);
                        MineEvent fracture = new MineEvent(mine.Miner, mine.Pickaxe, 1000)
                        {
                            Fault = this,
                            ReactionLevel = mine.ReactionLevel + 1
                        };
                        waitForMining.Add(mineable.Mine(fracture));
                    }
                }
                mine.AddWait(new WaitAll(waitForMining));
            }

            yield return Wait.NoWait;
        }
    }

    class TraitCrumbling : Trait
    {
        public TraitCrumbling() : base("crumbling", "Crumbling", "Destroys lower level rock faster.", new Color(171, 184, 194))
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
        public TraitPulverizing() : base("pulverizing", "Pulverizing", "No mining drops.", new Color(194, 172, 172))
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
        public TraitMeteorBash() : base("meteor_bash", "Meteor Bash", $"Extra attack with shield, deals damage based on {Stat.Defense.FormatString}.", new Color(194, 172, 172))
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
                var wait = attack.Attacker.Attack(targetCreature, new Vector2(offset.X, offset.Y), MeteorAttack);
                yield return wait;
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
        public TraitAlien() : base("alien", "Alien", "Randomize stats.", new Color(204, 91, 182))
        {
            Effect.Apply(new EffectStat.Randomized(this, Stat.Attack, -5, 20));
            Effect.Apply(new EffectStatPercent.Randomized(this, Stat.MiningSpeed, -0.5, 2.0));
        }
    }

    class TraitSharp : Trait
    {
        public TraitSharp() : base("sharp", "Sharp", "Causes bleeding.", new Color(192, 0, 0))
        {
            Effect.Apply(new OnStartAttack(this, Bleed));
        }

        public IEnumerable<Wait> Bleed(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            if (!attack.Defender.HasFamily(Family.Bloodless))
            {
                attack.StatusEffects.Add(new BleedLesser() { Buildup = traitLvl * 0.3, Duration = new Slider(20) });
                attack.StatusEffects.Add(new BleedGreater() { Buildup = traitLvl * 0.1, Duration = new Slider(10) });
            }

            yield return Wait.NoWait;
        }
    }

    class TraitStiff : Trait
    {
        public TraitStiff() : base("stiff", "Stiff", "Reduce damage taken.", new Color(192, 192, 192))
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
        public TraitBloodShield() : base("blood_shield", "Blood Shield", $"Attackers take {Element.Pierce.FormatString} damage and start bleeding.", new Color(192, 0, 0))
        {
            Effect.Apply(new OnStartDefend(this, BloodShield));
        }

        public IEnumerable<Wait> BloodShield(Attack attack)
        {
            if (attack.ExtraEffects.Any(effect => effect is AttackPhysical))
            {
                int traitLvl = attack.Defender.GetTrait(this);

                attack.Attacker.TakeDamage(5 * traitLvl, Element.Pierce);
                if (!attack.Attacker.HasFamily(Family.Bloodless))
                {
                    attack.Attacker.AddStatusEffect(new BleedLesser() { Buildup = traitLvl * 0.4, Duration = new Slider(30) });
                    attack.Attacker.AddStatusEffect(new BleedGreater() { Buildup = traitLvl * 0.1, Duration = new Slider(20) });
                }
            }
            yield return Wait.NoWait;
        }
    }

    class TraitFuming : Trait
    {
        Random Random = new Random();

        public TraitFuming() : base("fuming", "Fuming", "Sometimes produces smoke cloud.", new Color(255,255,255))
        {
            Effect.Apply(new OnAttack(this, EmitSmoke));
        }

        public IEnumerable<Wait> EmitSmoke(Attack attack)
        {
            var subject = attack.Defender;
            if (Random.NextDouble() < 0.2 && subject is Creature creature)
            {
                ExplodeSmoke(creature);
            }

            yield return Wait.NoWait;
        }

        private void ExplodeSmoke(Creature creature)
        {
            int n = 12;
            new ScreenShakeRandom(creature.World, 5, 20, LerpHelper.Linear);
            for (int i = 0; i < n; i++)
            {
                float angle = i * MathHelper.TwoPi / n;
                Vector2 offset = Util.AngleToVector(angle) * 24;

                new Smoke(creature.World, creature.VisualTarget + offset, Vector2.Zero, angle, 10 + Random.Next(5));
            }

            int radius = 2;

            Cloud cloud = creature.Tile.Map.AddCloud(map => new CloudSmoke(map));

            foreach (var tile in SkillUtil.GetCircularArea(creature, radius))
            {
                cloud.Add(tile, 15);
            }
        }
    }

    class TraitPoxic : Trait
    {
        public TraitPoxic() : base("poxic", "Poxic", "Sometimes turns enemies into slime.", new Color(206, 221, 159))
        {
            Effect.Apply(new OnStartAttack(this, Slime));
        }

        public IEnumerable<Wait> Slime(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            if (!attack.Defender.HasFamily(Family.Slime))
                attack.StatusEffects.Add(new Slimed() { Buildup = 0.4 + (traitLvl - 1) * 0.1 });

            yield return Wait.NoWait;
        }
    }

    class TraitSlimeEater : Trait
    {
        public TraitSlimeEater() : base("slime_eater", "Slime Eater", "Devour slime for health.", new Color(206, 221, 159))
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
                targetCreature.TakeDamage(targetCreature.GetStat(Stat.HP), Element.Healing, null);
            }

            yield return Wait.NoWait;
        }
    }

    class TraitSludgeArmor : Trait
    {
        public TraitSludgeArmor() : base("sludge_armor", "Sludge Armor", "When hit by slime, reduce status buildup.", new Color(206, 221, 159))
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

        public TraitSlaughtering() : base("slaughtering", "Slaughtering", "More drops, but no experience.", new Color(128, 0, 0))
        {
            //TODO
        }
    }

    class TraitLifeSteal : Trait
    {
        Random Random = new Random();

        public TraitLifeSteal() : base("life_steal", "Life Steal", "Sometimes steal life on attack.", new Color(128, 0, 0))
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

        public TraitUndead() : base("undead", "Undead", $"{Element.Healing.FormatString} causes damage.", new Color(128, 112, 128))
        {
            Effect.Apply(new EffectFamily(this, Family.Undead));
            Effect.Apply(new EffectStatMultiply(this, Element.Healing.DamageRate, -1));
            Effect.Apply(new OnDeath(this, CreateCryptDust));
        }

        private IEnumerable<Wait> CreateCryptDust(DeathEvent death)
        {
            var creature = death.Creature;
            var cloud = creature.Map.AddCloud(map => new CloudCryptDust(map));
            foreach (var neighbor in SkillUtil.GetCircularArea(creature, 1))
                if(!neighbor.Solid)
                    cloud.Add(neighbor, 30);
            yield return Wait.NoWait;
        }
    }

    class TraitSparking : Trait
    {
        Random Random = new Random();

        public TraitSparking() : base("sparking", "Sparking", $"Spark to adjacent tiles, dealing {Element.Thunder.FormatString} damage.", new Color(128, 112, 128))
        {
            Effect.Apply(new OnAttack(this, Spark));
        }

        public IEnumerable<Wait> Spark(Attack attack)
        {
            int traitLvl = attack.Attacker.GetTrait(this);

            if (attack.Defender is Creature targetCreature && attack.ReactionLevel == 0)
            {
                yield return Scheduler.Instance.RunAndWait(SkillUtil.Spark(attack.Attacker, Random, (attacker,defender) => SparkAttack(attacker, defender, traitLvl * 20)));
            }
            yield return Wait.NoWait;
        }

        private Attack SparkAttack(Creature attacker, IEffectHolder defender, double force)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(force, 0, 1);
            attack.Elements.Add(Element.Thunder, 1.0);
            attack.ReactionLevel = 1;
            return attack;
        }
    }
}
