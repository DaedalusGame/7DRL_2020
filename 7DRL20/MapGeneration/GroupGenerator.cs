using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    struct FeelingRequirement {
        public LevelFeeling Feeling;
        public double Min;
        public double Max;

        public FeelingRequirement(LevelFeeling feeling, double min, double max)
        {
            Feeling = feeling;
            Min = min;
            Max = max;
        }

        public bool Fits(double value)
        {
            return value >= Min && value <= Max;
        }

        public override string ToString()
        {
            return $"Requires {Feeling} ({Min}-{Max})";
        }
    }

    class GroupGenerator
    {
        public delegate GeneratorGroup GroupDelegate(MapGenerator generator);

        public static List<GroupGenerator> Generators = new List<GroupGenerator>();

        public GroupDelegate Generate;
        public int Weight = 1;
        public List<FeelingRequirement> Requirements = new List<FeelingRequirement>();

        public GroupGenerator(GroupDelegate generate)
        {
            Generate = generate;
            Generators.Add(this);
        }

        public bool FitsRequirements(LevelFeelingSet feelings)
        {
            foreach(var requirement in Requirements)
            {
                var value = feelings[requirement.Feeling];
                if (!requirement.Fits(value))
                    return false;
            }

            return true;
        }

        public static GroupGenerator Home = new GroupGenerator(MakeHome)
        {
            Weight = 0,
        };
        public static GroupGenerator DirtCave = new GroupGenerator(MakeDirtCave)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, double.NegativeInfinity, 20) },
        };
        public static GroupGenerator Bog = new GroupGenerator(MakeBog)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, double.NegativeInfinity, 60) },
        };
        public static GroupGenerator FireCave = new GroupGenerator(MakeFireCave)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, double.NegativeInfinity, 20) },
        };
        public static GroupGenerator AdamantCave = new GroupGenerator(MakeAdamantCave)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, double.NegativeInfinity, 20) },
        };
        public static GroupGenerator AcidCave = new GroupGenerator(MakeAcidCave)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 5, 25) },
        };
        public static GroupGenerator SeaOfDirac = new GroupGenerator(MakeSeaOfDirac)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 5, 15) },
        };
        public static GroupGenerator MagmaMine = new GroupGenerator(MakeMagmaMine)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 10, 40) },
        };
        public static GroupGenerator Dungeon = new GroupGenerator(MakeDungeon)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 10, 80) },
        };
        public static GroupGenerator IvoryTower = new GroupGenerator(MakeIvoryTower)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 40, 100) },
        };
        public static GroupGenerator EbonyTower = new GroupGenerator(MakeEbonyTower)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 40, 100) },
        };
        public static GroupGenerator DarkCastle = new GroupGenerator(MakeDarkCastle)
        {
            Requirements = { new FeelingRequirement(LevelFeeling.Difficulty, 60, 200) },
        };

        public static GeneratorGroup MakeHome(MapGenerator generator)
        {
            return new Home(generator) //Home
            {
                CaveColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                BrickColor = new TileColor(new Color(129, 64, 41), new Color(224, 175, 158)),
                WoodColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                Spawns = { EnemySpawn.Vorrax },
            };
        }
        public static GeneratorGroup MakeDirtCave(MapGenerator generator)
        {
            return new Cave(generator) //Dirt Cave
            {
                CaveColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                BrickColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                WoodColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                Spawns = { EnemySpawn.Skeleton },
            };
        }
        public static GeneratorGroup MakeBog(MapGenerator generator)
        {
            return new CaveBog(generator) //Bog
            {
                CaveColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                BrickColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                WoodColor = new TileColor(new Color(80, 56, 41), new Color(185, 138, 87)),
                Spawns = { EnemySpawn.PeatMummy, EnemySpawn.SwampHag },
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
            };
        }
        public static GeneratorGroup MakeAcidCave(MapGenerator generator)
        {
            Color lowColor = Color.Lerp(Color.Black, Color.GreenYellow, 0.5f);
            return new CaveAcid(generator) //Acid Cave
            {
                CaveColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                WoodColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                GlowColor = new AdvancedColor(new[] { Color.Black, Color.GreenYellow, Color.Black }, 60 * 3),
                Spawns = { EnemySpawn.AcidBlob, EnemySpawn.Ctholoid, EnemySpawn.YellowDragon },
            };
        }
        public static GeneratorGroup MakeSeaOfDirac(MapGenerator generator)
        {
            return new CaveWater(generator) //Sea of Dirac
            {
                CaveColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                BrickColor = new TileColor(new Color(80, 80, 150), new Color(253, 234, 248)),
                WoodColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                GlowColor = new AdvancedColor(new[] { Color.Black, new Color(34, 255, 255), Color.Black }, 60 * 3),
                Spawns = { EnemySpawn.PoisonBlob, EnemySpawn.GoreVala, EnemySpawn.BlueDragon, EnemySpawn.Ctholoid },
            };
        }
        public static GeneratorGroup MakeMagmaMine(MapGenerator generator)
        {
            return new CaveMagma(generator) //Magma Mine
            {
                CaveColor = new TileColor(new Color(247, 211, 70), new Color(160, 35, 35)),
                BrickColor = new TileColor(new Color(160, 35, 35), new Color(247, 211, 70)),
                WoodColor = new TileColor(new Color(160, 35, 35), new Color(247, 211, 70)),
                Spawns = { EnemySpawn.BlastCannon, EnemySpawn.AcidBlob, EnemySpawn.Skeleton, EnemySpawn.RedDragon },
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
            };
        }
        public static GeneratorGroup MakeEbonyTower(MapGenerator generator)
        {
            return new Tower(generator) //Ivory Tower
            {
                CaveColor = new TileColor(new Color(10, 7, 10), new Color(77, 77, 77)),
                BrickColor = new TileColor(new Color(50, 15, 50), new Color(90, 90, 90)),
                WoodColor = new TileColor(new Color(50, 15, 50), new Color(90, 90, 90)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.DeathKnight, EnemySpawn.BlueDragon },
            };
        }
        public static GeneratorGroup MakeDarkCastle(MapGenerator generator)
        {
            
            return new CastleDark(generator) //Dark Castle
            {
                CaveColor = new TileColor(new Color(54, 72, 101), new Color(109, 197, 112)),
                BrickColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                WoodColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                GlowColor = new AdvancedColor(new[] { new Color(62, 79, 2), new Color(227, 253, 138), new Color(62, 79, 2) }, 60 * 3),
                Spawns = { EnemySpawn.DeathKnight, EnemySpawn.BlastCannon, EnemySpawn.Ctholoid },
            };
        }
    }

    class GroupSet
    {
        public List<GroupGenerator> Groups = new List<GroupGenerator>();

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
        double ChanceRequired = 0.1;
        double ChanceMultiple = 0.7;
        double ChanceStripe = 0.3;

        int GroupMin = 2;
        int GroupMax = 5;

        int RepeatMin = 1;
        int RepeatMax = 4;

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
            var selection = GroupGenerator.Generators.Where(gen => gen.Weight > 0 && gen.FitsRequirements(generator.Feelings)).ToList();

            bool canStripe = true;

            if (Groups.Any() && x < ChanceRequired) //Just the required one
            {
                canStripe = false;
            }
            else if (x < ChanceRequired + ChanceMultiple) //Multiple
            {
                int groups = random.Next(GroupMin, GroupMax + 1);
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

            if(canStripe && random.NextDouble() < ChanceStripe)
            {
                int n = Groups.Count;
                int repeats = random.Next(RepeatMin, RepeatMax + 1);
                for(int e = 0; e < repeats; e++)
                    for (int i = 0; i < n; i++)
                        Groups.Add(Groups[i]);
            }

            return base.Generate(generator);
        }
    }
}
