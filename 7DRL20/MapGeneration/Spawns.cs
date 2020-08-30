using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    abstract class EnemySpawn
    {
        public abstract IEnumerable<Enemy> Spawn(SceneGame world, Tile tile);

        static List<EnemySpawn> AllSpawns = new List<EnemySpawn>();

        public string ID;

        public EnemySpawn(string id)
        {
            ID = id;
            AllSpawns.Add(this);
        }

        public static EnemySpawn GetSpawn(string id)
        {
            return AllSpawns.Find(spawn => spawn.ID == id);
        }

        public static EnemySpawn Skeleton = new SingleSpawn("skeleton", (world) => new Skeleton(world));
        public static EnemySpawn PeatMummy = new SingleSpawn("peat_mummy", (world) => new PeatMummy(world));
        public static EnemySpawn DeathKnight = new SingleSpawn("death_knight", (world) => new DeathKnight(world));

        public static EnemySpawn BlastCannon = new SingleSpawn("blast_cannon", (world) => new BlastCannon(world));

        public static EnemySpawn GoreVala = new SingleSpawn("gore_vala", (world) => new GoreVala(world));
        public static EnemySpawn Vorrax = new SingleSpawn("vorrax", (world) => new Vorrax(world));
        public static EnemySpawn Ctholoid = new SingleSpawn("cthuloid", (world) => new Ctholoid(world));

        public static EnemySpawn BlueDragon = new SingleSpawn("blue_dragon", (world) => new BlueDragon(world));
        public static EnemySpawn YellowDragon = new SingleSpawn("yellow_dragon", (world) => new YellowDragon(world));
        public static EnemySpawn RedDragon = new SingleSpawn("red_dragon", (world) => new RedDragon(world));

        public static EnemySpawn PoisonBlob = new SingleSpawn("poison_blob", (world) => new PoisonBlob(world));
        public static EnemySpawn AcidBlob = new SingleSpawn("acid_blob", (world) => new AcidBlob(world));
    }

    class SingleSpawn : EnemySpawn
    {
        Func<SceneGame, Enemy> EnemyFunction;

        public SingleSpawn(string id, Func<SceneGame, Enemy> enemyFunction) : base(id)
        {
            EnemyFunction = enemyFunction;
        }

        public override IEnumerable<Enemy> Spawn(SceneGame world, Tile tile)
        {
            Enemy enemy = EnemyFunction(world);
            enemy.MoveTo(tile,0);
            yield return enemy;
        }
    }
}
