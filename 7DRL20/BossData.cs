using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class BossData
    {
        public delegate IEnumerable<Enemy> SpawnDelegate(Tile tile);
        public delegate bool SpawnCondition(BossData data);

        EnemySpawner Parent;

        public Random Random => Parent.Random;
        public SceneGame World => Parent.World;
        public List<Enemy> Bosses => Parent.Bosses;

        public int Kills;
        public List<Enemy> AliveBosses = new List<Enemy>();

        public SpawnDelegate Spawn;
        public SpawnCondition ConditionFast;
        public SpawnCondition ConditionSlow;
        public double FastChance;
        public double SlowChance;
        public bool ConditionFastLast;
        public bool ConditionSlowLast;
        public Func<Tile, bool> TileConditionSimple;
        public Func<Tile, bool> TileConditionComplex;

        public BossData(EnemySpawner parent, SpawnDelegate spawn)
        {
            Parent = parent;
            Spawn = spawn;
        }

        public BossData SetSlowChance(SpawnCondition condition, double chance)
        {
            ConditionSlow = condition;
            SlowChance = chance;
            return this;
        }

        public BossData SetFastChance(SpawnCondition condition, double chance)
        {
            ConditionFast = condition;
            FastChance = chance;
            return this;
        }

        public BossData SetTile(Func<Tile, bool> simple, Func<Tile, bool> complex)
        {
            TileConditionSimple = simple;
            TileConditionComplex = complex;
            return this;
        }

        /// <summary>
        /// Called every tick
        /// </summary>
        public void RuminateFast()
        {
            ConditionFastLast = ConditionFast?.Invoke(this) ?? false;
            if (ConditionFastLast && Random.NextDouble() < FastChance)
                DoSpawn();
            Cleanup();
        }

        /// <summary>
        /// Called every boss spawn tick
        /// </summary>
        public void RuminateSlow()
        {
            ConditionSlowLast = ConditionSlow?.Invoke(this) ?? false;
            if (ConditionSlowLast && Random.NextDouble() < SlowChance)
                DoSpawn();
        }

        public void DoSpawn()
        {
            Tile tile = null;
            IEnumerable<Tile> tiles = Parent.GetValidSpawnLocations(World.Player.Tile, TileConditionSimple, 0);

            foreach(Tile candidate in tiles.Take(80))
            {
                if (TileConditionComplex(candidate))
                {
                    tile = candidate;
                    break;
                }
            }

            if (tile != null)
            {
                var bosses = Spawn(tile);
                AliveBosses.AddRange(bosses);
                Bosses.AddRange(bosses);
            }
        }

        private void Cleanup()
        {
            foreach (Enemy boss in AliveBosses)
            {
                if (boss.Dead)
                {
                    Kills++;
                }
            }
            AliveBosses.RemoveAll(boss => boss.Dead || boss.Destroyed);
        }
    }
}
