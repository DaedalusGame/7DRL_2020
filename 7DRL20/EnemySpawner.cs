using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.MapGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    delegate IEnumerable<Enemy> EnemySpawnDelegate(SceneGame world, Tile tile);

    class EnemySpawner : ITurnTaker
    {
        public double TurnSpeed => 1;
        public double TurnBuildup { get; set; }
        public bool TurnReady => TurnBuildup >= 1;
        public bool RemoveFromQueue => false;

        public Random Random = new Random();

        List<BossData> BossDatabase = new List<BossData>();

        public SceneGame World;
        public Slider Encounter;
        public Slider EncounterBoss;
        public List<Enemy> Enemies = new List<Enemy>();
        public List<Enemy> Bosses = new List<Enemy>();

        public int SpawnRadius = 6;
        public int DespawnRadius = 16;

        public EnemySpawner(SceneGame world, int time)
        {
            World = world;
            Encounter = new Slider(time);
            EncounterBoss = new Slider(time);

            BossDatabase.Add(new BossData(this, (tile) =>
            {
                var boss = new EnderErebizo(World);
                boss.MoveTo(tile, 0);
                boss.MakeAggressive(World.Player);
                World.ActionQueue.Add(boss);
                return new[] { boss };
            })
            .SetSlowChance(data => data.AliveBosses.Count < 2, 0.0)
            .SetTile(tile => tile.Opaque, tile => {
                var rectangle = new Rectangle(tile.X, tile.Y, 2, 2);
                var checkTiles = tile.GetNearby(rectangle, 0);
                return checkTiles.All(t => t.Opaque);
            }));

            BossDatabase.Add(new BossData(this, (tile) =>
            {
                int radius = 2;
                var tileSet = tile.GetNearby(radius).Where(x => GetSquareDistance(tile, x) <= radius * radius).Shuffle();
                new HeavenRay(World, tile, 10);
                new TileExplosion(World, tileSet);
                var boss = new Wallhach(World);
                boss.MoveTo(tile, 0);
                boss.MakeAggressive(World.Player);
                World.ActionQueue.Add(boss);
                return new[] { boss };
            })
            .SetSlowChance(data => data.AliveBosses.Count < 2, 1.0)
            .SetTile(tile => !tile.Opaque && tile.Creatures.Empty()));
        }

        protected int GetSquareDistance(Tile a, Tile b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;

            return dx * dx + dy * dy;
        }

        public IEnumerable<Tile> GetValidSpawnLocations(Tile center, Func<Tile,bool> condition, int minRadius)
        {
            return center.GetNearby(SpawnRadius)
                .Where(tile => Math.Abs(tile.X - center.X) >= minRadius && Math.Abs(tile.Y - center.Y) >= minRadius)
                .Where(condition)
                .Shuffle(Random);
        }

        public Wait TakeTurn(ActionQueue queue)
        {
            Encounter += 1;
            EncounterBoss += 1;
            Cleanup();
            //SpawnEnemies();
            //SpawnBosses();
            this.ResetTurn();
            return Wait.NoWait;
        }

        private void SpawnEnemies()
        {
            if (Encounter.Done && Enemies.Count <= 1)
            {
                var baseTile = World.Player.Tile;
                int spawnAmount = 4;

                foreach (var spawnTile in baseTile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).Shuffle().Take(spawnAmount))
                {
                    EnemySpawn spawn = spawnTile.Group.Spawns.Pick(Random);
                    foreach (var enemy in spawn.Spawn(World,spawnTile))
                    {
                        Enemies.Add(enemy);
                        enemy.MakeAggressive(World.Player);
                        World.ActionQueue.Add(enemy);
                        new Smoke(World, enemy.VisualTarget, Vector2.Zero, 0, 15);
                    }
                }

                Encounter.Time = 0;
            }
        }

        private void SpawnBosses()
        {
            if (EncounterBoss.Done)
            {
                foreach (BossData bossData in BossDatabase)
                {
                    bossData.RuminateSlow();
                }
                EncounterBoss.Time = 0;
            }
            foreach (BossData bossData in BossDatabase)
            {
                bossData.RuminateFast();
            }
        }

        private void Cleanup()
        {
            foreach (var enemy in Enemies)
            {
                if (enemy.Destroyed)
                    continue;
                int dx = enemy.X - World.Player.X;
                int dy = enemy.Y - World.Player.Y;
                if (Math.Abs(dx) > 15 || Math.Abs(dy) > 10)
                    enemy.Destroy();
            }
            Enemies.RemoveAll(enemy => enemy.Destroyed);
            Bosses.RemoveAll(enemy => enemy.Destroyed);
        }
    }
}
