using Microsoft.Xna.Framework;
using RoguelikeEngine.Effects;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class CryptDustSpawn
    {
        public static List<CryptDustSpawn> AllSpawns = new List<CryptDustSpawn>();

        public double Weight;
        public int Size;
        public int CloudsMin = 1;
        public int CloudsMax = int.MaxValue;
        public float TimeMin = 0;
        public float TimeMax = float.PositiveInfinity;
        Func<SceneGame, Enemy> EnemyFunction;

        public CryptDustSpawn(double weight, int size, Func<SceneGame, Enemy> enemyFunction)
        {
            Weight = weight;
            Size = size;
            EnemyFunction = enemyFunction;
            AllSpawns.Add(this);
        }

        public IEnumerable<Enemy> Spawn(SceneGame world, Tile tile)
        {
            Enemy enemy = EnemyFunction(world);
            enemy.MoveTo(tile, 0);
            yield return enemy;
        }

        public static CryptDustSpawn SpawnSkeleton = new CryptDustSpawn(10, 1, world => new Skeleton(world))
        {
            TimeMin = 15,
            CloudsMin = 5,
        };
    }

    class CascadeHelper
    {
        public delegate IEnumerable<Creature> GetNearbyDelegate(Creature creature, CascadeHelper cascade);
        public delegate IEnumerable<Wait> ArcDelegate(Creature start, Creature end);

        GetNearbyDelegate GetNearby;
        ArcDelegate Arc;
        public Dictionary<Creature, int> Hits = new Dictionary<Creature, int>();
        public int StrikesMax = 1; //How often we can hit the same target total
        public int LoopsMax = 1; //How often we can hit the same target on the same branch
        public int TotalMax = int.MaxValue; //How often we can hit total
        int MaxDepth; //Maximum recursion depth
        public int ArcDelay; //Delay before shooting off the next arc

        static Random Random = new Random();

        public CascadeHelper(GetNearbyDelegate getNearby, ArcDelegate arc, int maxDepth)
        {
            GetNearby = getNearby;
            Arc = arc;
            MaxDepth = maxDepth;
        }

        public IEnumerable<Wait> Start(Creature start)
        {
            AddHit(start);
            List<Wait> waitForArcs = new List<Wait>();
            foreach(var nearby in GetNearby(start, this))
            {
                if (!CanHit(nearby) || Hits.Sum(pair => pair.Value) >= TotalMax)
                    continue;
                waitForArcs.Add(Scheduler.Instance.RunAndWait(Branch(start, nearby, new Dictionary<Creature, int>(), 0)));
                yield return new WaitTime(ArcDelay);
            }
            yield return new WaitAll(waitForArcs);
        }

        public IEnumerable<Wait> Branch(Creature start, Creature end, Dictionary<Creature, int> branchHits, int depth)
        {
            AddHit(end, branchHits);
            yield return Scheduler.Instance.RunAndWait(Arc(start, end));
            if (depth < MaxDepth) //We can blast more targets, cascade
            {
                List<Wait> waitForArcs = new List<Wait>();
                foreach (var nearby in GetNearby(end, this))
                {
                    if (!CanHit(nearby, branchHits) || Hits.Sum(pair => pair.Value) >= TotalMax)
                        continue;
                    var hits = new Dictionary<Creature, int>(branchHits);
                    waitForArcs.Add(Scheduler.Instance.RunAndWait(Branch(end, nearby, hits, depth + 1)));
                    yield return new WaitTime(ArcDelay);
                }
                yield return new WaitAll(waitForArcs);
            }
        }

        private void AddHit(Creature target)
        {
            Hits.Operate(target, 1, (a, b) => a + b);
        }

        private void AddHit(Creature target, Dictionary<Creature, int> branchHits)
        {
            AddHit(target);
            branchHits.Operate(target, 1, (a, b) => a + b);
        }

        private bool CanHit(Creature target)
        {
            return Hits.GetOrDefault(target, 0) < StrikesMax;
        }

        private bool CanHit(Creature target, Dictionary<Creature, int> branchHits)
        {
            return branchHits.GetOrDefault(target, 0) < LoopsMax && CanHit(target);
        }

        public static IEnumerable<Creature> GetNearbyCircular(Creature creature, CascadeHelper cascade, int radius, int count)
        {
            var creatures = SkillUtil.GetCircularArea(creature, radius).SelectMany(x => x.Creatures).Distinct().Where(x => cascade.CanHit(x));
            return creatures.Shuffle(Random).Take(count);
        }
    }

    static class SkillUtil
    {
        class CryptDustSpawnPos
        {
            public Tile Tile;
            public int Count;
            public CryptDustSpawn Spawn;

            public int Size => Spawn.Size;

            public bool Valid => Count >= Spawn.CloudsMin && Count <= Spawn.CloudsMax;

            public CryptDustSpawnPos(CryptDustSpawn spawn, Tile tile)
            {
                Tile = tile;
                Spawn = spawn;
            }
        }

        
        static SkillUtil()
        {

        }

        public static void TrySpawnCryptDust(CloudCryptDust cloud, IEnumerable<Tile> tiles, Random random)
        {
            foreach (var tile in tiles)
            {
                TrySpawnCryptDust(cloud, tile, random);
            }
        }

        public static void TrySpawnCryptDust(CloudCryptDust cloud, Tile tile, Random random)
        {
            var part = cloud.Get(tile);
            List<CryptDustSpawnPos> spawnList = new List<CryptDustSpawnPos>();

            if (part == null)
                return;

            foreach (var spawn in CryptDustSpawn.AllSpawns)
            {
                if (part.Duration >= spawn.TimeMin && part.Duration <= spawn.TimeMax)
                {
                    spawnList.Add(new CryptDustSpawnPos(spawn, part.Tile));
                }
            }

            foreach (var partOther in cloud.Parts)
            {
                int dist = Math.Max(Math.Abs(partOther.MapTile.X - part.MapTile.X), Math.Abs(partOther.MapTile.Y - part.MapTile.Y));
                foreach (var spawn in spawnList)
                {
                    if (spawn.Size >= dist)
                        spawn.Count++;
                }
            }

            spawnList.RemoveAll(spawn => !spawn.Valid);
            if (spawnList.Any())
            {
                var picked = spawnList.Pick(random);
                cloud.Remove(tile.GetNearby(picked.Size));

                foreach (var enemy in picked.Spawn.Spawn(cloud.World, tile))
                {
                    enemy.AddControlTurn();
                    new Smoke(cloud.World, enemy.VisualTarget, Vector2.Zero, 0, 15);
                }
            }
        }

        public static Vector2 SafeNormalize(Vector2 vec)
        {
            if (vec == Vector2.Zero)
                return vec;
            return Vector2.Normalize(vec);
        }

        //Checks non-random composites
        public static bool HasElement(this Attack attack, Element element)
        {
            return attack.Elements.Any(e => IsElement(e.Key, element));
        }

        public static bool IsElement(Element a, Element b)
        {
            if (a == b)
                return true;
            else if (a is ElementRandom)
                return false;
            else if (a.CanSplit())
                return a.Split().Any(elem => IsElement(elem.Key, b));
            else
                return false;
        }

        public static bool IsWeaponAttack(this Attack attack)
        {
            return attack.ExtraEffects.Any(x => x is AttackWeapon);
        }

        public static bool IsSpell(this Attack attack)
        {
            return attack.ExtraEffects.Any(x => x is AttackSkill skill && skill.Skill.SkillType == SkillType.Spell);
        }

        public static IEnumerable<Tile> GetCircularArea(Creature origin, int radius)
        {
            return GetCircularArea(origin.Tile, origin.Mask.GetRectangle(origin.X, origin.Y), origin.ActualTarget, radius);
        }

        public static IEnumerable<Tile> GetCircularArea(Tile origin, int radius)
        {
            return GetCircularArea(origin, new Rectangle(origin.X, origin.Y, 1, 1), origin.VisualTarget, radius);
        }

        public static IEnumerable<Tile> GetCircularArea(Tile origin, Rectangle rectangle, Vector2 center, int radius)
        {
            double pixelRadius = radius * 16 + 8;

            foreach (var tile in origin.GetNearby(rectangle, radius))
            {
                var distance = (tile.VisualTarget - center).LengthSquared();
                if (distance <= pixelRadius * pixelRadius)
                {
                    yield return tile;
                }
            }
        }

        public static IEnumerable<Tile> GetFrontierTiles(Creature creature)
        {
            return GetFrontierTiles(new[] { creature });
        }

        public static IEnumerable<Tile> GetFrontierTiles(IEnumerable<Creature> creatures)
        {
            HashSet<Tile> targetTiles = new HashSet<Tile>();
            foreach (var creature in creatures)
            {
                targetTiles.AddRange(creature.Mask.GetFullFrontier().Select(o => creature.Tile.GetNeighbor(o.X, o.Y)));
            }
            foreach (var creature in creatures)
            {
                targetTiles.RemoveRange(creature.Tiles);
            }
            return targetTiles;
        }

        struct Circle
        {
            public Vector2 Position;
            public float Radius;

            public Circle(Vector2 position, float radius)
            {
                Position = position;
                Radius = radius;
            }

            public bool Contains(Vector2 pos)
            {
                return (pos - Position).LengthSquared() <= Radius * Radius;
            }
        }

        public static IEnumerable<Tile> GetShieldedExplosionTiles(Creature origin, int radius, int shieldRadius)
        {
            return GetShieldedExplosionTiles(origin.Tile, origin.Mask.GetRectangle(origin.X, origin.Y), origin.ActualTarget, radius, shieldRadius);
        }

        public static IEnumerable<Tile> GetShieldedExplosionTiles(Tile origin, int radius, int shieldRadius)
        {
            return GetShieldedExplosionTiles(origin, new Rectangle(origin.X, origin.Y, 1, 1), origin.VisualTarget, radius, shieldRadius);
        }

        public static IEnumerable<Tile> GetShieldedExplosionTiles(Tile origin, Rectangle rectangle, Vector2 center, int radius, int shieldRadius)
        {
            var circle = GetCircularArea(origin, rectangle, center, radius).ToList();
            var shields = new List<Circle>();
            foreach (var shieldTile in circle.Where(tile => tile.Solid))
            {
                var normal = Vector2.Normalize(shieldTile.VisualTarget - center);
                var circleCenter = shieldTile.VisualTarget + normal * shieldRadius;
                shields.Add(new Circle(circleCenter, shieldRadius + 0.5f));
            }
            foreach (var tile in circle.Where(tile => !tile.Solid))
            {
                var tileCenter = tile.VisualTarget;
                var shielded = shields.Any(shield => shield.Contains(tileCenter));
                if (!shielded)
                    yield return tile;
            }
        }

        public static IEnumerable<Wait> Spark(Creature user, Random random, AttackDelegate attack)
        {
            var lightning = SpriteLoader.Instance.AddSprite("content/lightning");
            IEnumerable<Tile> targetTiles = GetFrontierTiles(user);
            HashSet<Creature> targets = new HashSet<Creature>();
            yield return user.WaitSome(10);
            foreach (Tile tile in targetTiles)
            {
                targets.AddRange(tile.Creatures.Where(creature => creature != user));
            }
            List<Wait> waitForDamage = new List<Wait>();
            foreach (var target in targets)
            {
                new LightningSpark(user.World, lightning, user.VisualPosition() + user.Mask.GetRandomPixel(random), target.VisualPosition() + target.Mask.GetRandomPixel(random), 5);
                var wait = user.Attack(target, SkillUtil.SafeNormalize(target.VisualTarget - user.VisualTarget), attack);
                waitForDamage.Add(wait);
            }
            yield return new WaitAll(waitForDamage);
        }

        public static AttackDelegate GetTerrainAttack(Creature creature, Tile tile)
        {
            if (tile is Water)
            {
                return WaterAttack;
            }
            if (tile is Lava)
            {
                return LavaAttack;
            }

            return null;
        }

        private static Attack WaterAttack(Creature attacker, IEffectHolder defender)
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

        private static Attack LavaAttack(Creature attacker, IEffectHolder defender)
        {
            Attack attack = new Attack(attacker, defender);
            attack.SetParameters(50, 1, 1);
            attack.Elements.Add(Element.Fire, 1.0);
            return attack;
        }
    }
}
