using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
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

        SceneGame World;
        Slider Encounter;
        List<Enemy> Enemies = new List<Enemy>();
        List<Enemy> Bosses = new List<Enemy>();

        public EnemySpawner(SceneGame world, int time)
        {
            World = world;
            Encounter = new Slider(time);
        }

        public Wait TakeTurn(ActionQueue queue)
        {
            Encounter += 1;
            Cleanup();
            if (Encounter.Done && Enemies.Count <= 1)
            {
                var baseTile = World.Player.Tile;
                int spawnAmount = 4;

                foreach (var spawnTile in baseTile.GetNearby(4).Where(tile => !tile.Solid && !tile.Creatures.Any()).Shuffle().Take(spawnAmount))
                {
                    foreach (var enemy in spawnTile.Group.Spawn(World, spawnTile))
                    {
                        Enemies.Add(enemy);
                        enemy.MakeAggressive(World.Player);
                        World.ActionQueue.Add(enemy);
                        new Smoke(World, new Vector2(spawnTile.X * 16 + 8, spawnTile.Y * 16 + 8), Vector2.Zero, 0, 15);
                    }
                }

                Encounter.Time = 0;
            }
            this.ResetTurn();
            return Wait.NoWait;
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
        }
    }
}
