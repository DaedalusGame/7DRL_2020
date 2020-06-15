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

        public static List<GroupGenerator> Generators = new List<GroupGenerator>();

        public GroupDelegate Generate;
        public Type GroupType;
        public int Weight = 1;
        public int DifficultyLower = int.MinValue;
        public int DifficultyUpper = int.MaxValue;

        public GroupGenerator(GroupDelegate generate)
        {
            Generate = generate;
            Generators.Add(this);
        }

        public static GroupGenerator Home = new GroupGenerator(MakeHome)
        {
            Weight = 0,
        };
        public static GroupGenerator FireCave = new GroupGenerator(MakeFireCave)
        {
            DifficultyLower = 0,
            DifficultyUpper = 20,
        };
        public static GroupGenerator AdamantCave = new GroupGenerator(MakeAdamantCave)
        {
            DifficultyLower = 0,
            DifficultyUpper = 20,
        };
        public static GroupGenerator AcidCave = new GroupGenerator(MakeAcidCave)
        {
            DifficultyLower = 5,
            DifficultyUpper = 25,
        };
        public static GroupGenerator SeaOfDirac = new GroupGenerator(MakeSeaOfDirac)
        {
            DifficultyLower = 5,
            DifficultyUpper = 15,
        };
        public static GroupGenerator MagmaMine = new GroupGenerator(MakeMagmaMine)
        {
            DifficultyLower = 10,
            DifficultyUpper = 40,
        };
        public static GroupGenerator Dungeon = new GroupGenerator(MakeDungeon)
        {
            DifficultyLower = 10,
            DifficultyUpper = 80,
        };
        public static GroupGenerator IvoryTower = new GroupGenerator(MakeIvoryTower)
        {
            DifficultyLower = 40,
            DifficultyUpper = 100,
        };
        public static GroupGenerator DarkCastle = new GroupGenerator(MakeDarkCastle)
        {
            DifficultyLower = 60,
            DifficultyUpper = 200,
        };

        public static GeneratorGroup MakeHome(MapGenerator generator)
        {
            return new Home(generator) //Home
            {
                CaveColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                BrickColor = new TileColor(new Color(129, 64, 41), new Color(224, 175, 158)),
                WoodColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                Spawns = { },
            };
        }
        public static GeneratorGroup MakeFireCave(MapGenerator generator)
        {
            return new CaveLava(generator) //Fire Cave
            {
                CaveColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                BrickColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                WoodColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                Spawns = { EnemySpawn.Skeleton },
                Template = FireCave,
            };
        }
        public static GeneratorGroup MakeAdamantCave(MapGenerator generator)
        {
            return new Cave(generator) //Adamant Cave
            {
                CaveColor = new TileColor(new Color(128, 160, 160), new Color(32, 64, 32)),
                BrickColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                WoodColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.PoisonBlob },
                Template = AdamantCave
            };
        }
        public static GeneratorGroup MakeAcidCave(MapGenerator generator)
        {
            return new CaveAcid(generator) //Acid Cave
            {
                CaveColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                WoodColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                GlowColor = (time) => Color.Lerp(Color.Black, Color.GreenYellow, 0.75f + 0.25f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.AcidBlob, EnemySpawn.Ctholoid, EnemySpawn.YellowDragon },
                Template = AcidCave
            };
        }
        public static GeneratorGroup MakeSeaOfDirac(MapGenerator generator)
        {
            return new CaveWater(generator) //Sea of Dirac
            {
                CaveColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                BrickColor = new TileColor(new Color(80, 80, 150), new Color(253, 234, 248)),
                WoodColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                GlowColor = (time) => Color.Lerp(Color.Black, new Color(34, 255, 255), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.PoisonBlob, EnemySpawn.GoreVala, EnemySpawn.BlueDragon, EnemySpawn.Ctholoid },
                Template = SeaOfDirac
            };
        }
        public static GeneratorGroup MakeMagmaMine(MapGenerator generator)
        {
            return new CaveMagma(generator) //Magma Mine
            {
                CaveColor = new TileColor(new Color(247, 211, 70), new Color(160, 35, 35)),
                BrickColor = new TileColor(new Color(160, 35, 35), new Color(247, 211, 70)),
                WoodColor = new TileColor(new Color(160, 35, 35), new Color(247, 211, 70)),
                Spawns = { EnemySpawn.BlastCannon, EnemySpawn.AcidBlob, EnemySpawn.Skeleton },
                Template = MagmaMine
            };
        }
        public static GeneratorGroup MakeDungeon(MapGenerator generator)
        {
            return new Castle(generator) //Dungeon
            {
                CaveColor = new TileColor(new Color(128, 128, 128), new Color(160, 160, 160)),
                BrickColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                WoodColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.Vorrax, EnemySpawn.DeathKnight },
                Template = Dungeon
            };
        }
        public static GeneratorGroup MakeIvoryTower(MapGenerator generator)
        {
            return new Tower(generator) //Ivory Tower
            {
                CaveColor = new TileColor(new Color(108, 106, 79), new Color(188, 173, 139)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                WoodColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.DeathKnight, EnemySpawn.BlueDragon },
                Template = IvoryTower
            };
        }
        public static GeneratorGroup MakeDarkCastle(MapGenerator generator)
        {
            return new CastleDark(generator) //Dark Castle
            {
                CaveColor = new TileColor(new Color(54, 72, 101), new Color(109, 197, 112)),
                BrickColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                WoodColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                GlowColor = (time) => Color.Lerp(new Color(62, 79, 2), new Color(227, 253, 138), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.DeathKnight, EnemySpawn.BlastCannon, EnemySpawn.Ctholoid },
                Template = DarkCastle
            };
        }
    }

    class GroupSet
    {
        protected List<GroupGenerator> Groups = new List<GroupGenerator>();

        public GroupSet(IEnumerable<GroupGenerator> groups)
        {
            Groups.AddRange(groups);
        }

        public GroupSet(GroupGenerator group)
        {
            Groups.Add(group);
        }

        public GroupSet()
        {
        }

        public virtual IEnumerable<GeneratorGroup> Generate(MapGenerator generator)
        {
            foreach(var group in Groups)
            {
                yield return group.Generate(generator);
            }
        }
    }

    class GroupRandom : GroupSet
    {
        public GroupRandom() : base()
        {
        }

        public GroupRandom(GroupGenerator group) : base(group)
        {
        }

        public GroupRandom(IEnumerable<GroupGenerator> groups) : base(groups)
        {
        }

        public override IEnumerable<GeneratorGroup> Generate(MapGenerator generator)
        {
            Random random = generator.Random;
            double x = random.NextDouble();
            var selection = GroupGenerator.Generators.Where(gen => gen.Weight > 0).ToList();

            bool canStripe = true;

            if (Groups.Any() && x < 0.1) //Just the required one
            {
                canStripe = false;
            }
            else if (x < 0.8) //Multiple
            {
                int groups = random.Next(2, 5);
                for(int i = 0; i < groups; i++)
                {
                    if (selection.Count > 0)
                        Groups.Add(selection.PickAndRemove(random));
                }

                canStripe = true;
            }
            else //Only one
            {
                if(selection.Count > 0)
                    Groups.Add(selection.PickAndRemove(random));

                canStripe = Groups.Count >= 2;
            }

            if(canStripe && random.NextDouble() < 0.3)
            {
                int n = Groups.Count;
                int repeats = random.Next(1, 4);
                for(int e = 0; e < repeats; e++)
                    for (int i = 0; i < n; i++)
                        Groups.Add(Groups[i]);
            }

            return base.Generate(generator);
        }
    }
}
