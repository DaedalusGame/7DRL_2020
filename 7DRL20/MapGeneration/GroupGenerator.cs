using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    class GroupGenerator
    {
        public delegate GeneratorGroup GroupDelegate(MapGenerator generator);

        List<GroupDelegate> Groups = new List<GroupDelegate>();

        public GroupGenerator(IEnumerable<GroupDelegate> groups)
        {
            Groups.AddRange(groups);
        }

        public GroupGenerator(GroupDelegate group)
        {
            Groups.Add(group);
        }

        public static GeneratorGroup Home(MapGenerator generator)
        {
            return new Home(generator) //Home
            {
                CaveColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                BrickColor = new TileColor(new Color(129, 64, 41), new Color(224, 175, 158)),
                WoodColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                Spawns = {},
            };
        }
        public static GeneratorGroup FireCave(MapGenerator generator)
        {
            return new CaveLava(generator) //Fire Cave
            {
                CaveColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                BrickColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                WoodColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                Spawns = { EnemySpawn.Skeleton },
            };
        }
        public static GeneratorGroup AdamantCave(MapGenerator generator)
        {
            return new Cave(generator) //Adamant Cave
            {
                CaveColor = new TileColor(new Color(128, 160, 160), new Color(32, 64, 32)),
                BrickColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                WoodColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.PoisonBlob },
            };
        }
        public static GeneratorGroup AcidCave(MapGenerator generator)
        {
            return new CaveAcid(generator) //Acid Cave
            {
                CaveColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                GlowColor = (time) => Color.Lerp(Color.Black, Color.GreenYellow, 0.75f + 0.25f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.AcidBlob, EnemySpawn.Ctholoid, EnemySpawn.YellowDragon },
            };
        }
        public static GeneratorGroup SeaOfDirac(MapGenerator generator)
        {
            return new CaveWater(generator) //Sea of Dirac
            {
                CaveColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                BrickColor = new TileColor(new Color(80, 80, 150), new Color(253, 234, 248)),
                GlowColor = (time) => Color.Lerp(Color.Black, new Color(34, 255, 255), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.PoisonBlob, EnemySpawn.GoreVala, EnemySpawn.BlueDragon, EnemySpawn.Ctholoid },
            };
        }
        public static GeneratorGroup MagmaMine(MapGenerator generator)
        {
            return new CaveMagma(generator) //Magma Mine
            {
                CaveColor = new TileColor(new Color(247, 211, 70), new Color(160, 35, 35)),
                BrickColor = new TileColor(new Color(160, 35, 35), new Color(247, 211, 70)),
                Spawns = { EnemySpawn.BlastCannon, EnemySpawn.AcidBlob, EnemySpawn.Skeleton },
            };
        }
        public static GeneratorGroup Dungeon(MapGenerator generator)
        {
            return new Castle(generator) //Dungeon
            {
                CaveColor = new TileColor(new Color(128, 128, 128), new Color(160, 160, 160)),
                BrickColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.Vorrax, EnemySpawn.DeathKnight },
            };
        }
        public static GeneratorGroup IvoryTower(MapGenerator generator)
        {
            return new Tower(generator) //Ivory Tower
            {
                CaveColor = new TileColor(new Color(108, 106, 79), new Color(188, 173, 139)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.DeathKnight, EnemySpawn.BlueDragon },
            };
        }
        public static GeneratorGroup DarkCastle(MapGenerator generator)
        {
            return new CastleDark(generator) //Dark Castle
            {
                CaveColor = new TileColor(new Color(54, 72, 101), new Color(109, 197, 112)),
                BrickColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                GlowColor = (time) => Color.Lerp(new Color(62, 79, 2), new Color(227, 253, 138), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.DeathKnight, EnemySpawn.BlastCannon, EnemySpawn.Ctholoid },
            };
        }

        public virtual IEnumerable<GeneratorGroup> Generate(MapGenerator generator)
        {
            foreach(var group in Groups)
            {
                yield return group(generator);
            }
        }
    }
}
