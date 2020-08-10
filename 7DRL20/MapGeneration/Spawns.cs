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

        public static EnemySpawn Skeleton = new SingleSpawn((world) => new PeatMummy(world));
        public static EnemySpawn DeathKnight = new SingleSpawn((world) => new DeathKnight(world));

        public static EnemySpawn BlastCannon = new SingleSpawn((world) => new BlastCannon(world));

        public static EnemySpawn GoreVala = new SingleSpawn((world) => new GoreVala(world));
        public static EnemySpawn Vorrax = new SingleSpawn((world) => new Vorrax(world));
        public static EnemySpawn Ctholoid = new SingleSpawn((world) => new Ctholoid(world));

        public static EnemySpawn BlueDragon = new SingleSpawn((world) => new BlueDragon(world));
        public static EnemySpawn YellowDragon = new SingleSpawn((world) => new YellowDragon(world));
        public static EnemySpawn RedDragon = new SingleSpawn((world) => new RedDragon(world));

        public static EnemySpawn PoisonBlob = new SingleSpawn((world) => new PoisonBlob(world));
        public static EnemySpawn AcidBlob = new SingleSpawn((world) => new AcidBlob(world));
    }

    class SingleSpawn : EnemySpawn
    {
        Func<SceneGame, Enemy> EnemyFunction;

        public SingleSpawn(Func<SceneGame, Enemy> enemyFunction)
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
