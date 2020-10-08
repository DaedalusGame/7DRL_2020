using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    class AppliedBonus
    {
        public StairBonus Bonus;
        public int LevelsActive;

        public AppliedBonus(StairBonus bonus, int levelsActive)
        {
            Bonus = bonus;
            LevelsActive = levelsActive;
        }
    }

    class StairBonus
    {
        public static List<StairBonus> AllStairBonuses = new List<StairBonus>();

        public int Index;
        public string ID;
        public string Name;
        //Action<GeneratorTemplate> ModifyTemplate = (template) => { };
        Action<GroupSet> ModifySet = (set) => { };
        Action<MapGenerator> ModifyGenerator = (generator) => { }; 
        Action<GeneratorGroup> ModifyGroupPre = (group) => { };
        Action<GeneratorGroup> ModifyGroupPost = (group) => { };
        Func<Random, int> LevelDuration = (random) => 0;

        public StairBonus(string id, string name)
        {
            Index = AllStairBonuses.Count;
            ID = id;
            Name = name;
            AllStairBonuses.Add(this);
        }

        /*public void Apply(GeneratorTemplate template)
        {
            ModifyTemplate(template);
        }*/

        public void Apply(GroupSet set)
        {
            ModifySet(set);
        }

        public void Apply(MapGenerator generator)
        {
            ModifyGenerator(generator);
        }

        public void ApplyPre(GeneratorGroup group)
        {
            ModifyGroupPre(group);
        }

        public void ApplyPost(GeneratorGroup group)
        {
            ModifyGroupPost(group);
        }

        public int GetDuration(Random random)
        {
            return LevelDuration(random);
        }

        public static StairBonus NoBonus = new StairBonus("no_bonus", "No Bonus");

        public static StairBonus Difficult = new StairBonus("difficult", "Difficult Level")
        {
            ModifyGenerator = generator => { generator.Feelings.Add(LevelFeeling.Difficulty, +50); }
        };
        public static StairBonus Easy = new StairBonus("easy", "Easy Level")
        {
            ModifyGenerator = generator => { generator.Feelings.Add(LevelFeeling.Difficulty, -50); }
        };
        public static StairBonus Extend = new StairBonus("extend", "Extend Feelings")
        {
            ModifyGenerator = generator => {
                foreach(var feeling in generator.Bonuses)
                {
                    feeling.LevelsActive += 3;
                }
            }
        };
        public static StairBonus Shorten = new StairBonus("shorten", "Shorten Feelings")
        {
            ModifyGenerator = generator => {
                foreach (var feeling in generator.Bonuses)
                {
                    feeling.LevelsActive -= 3;
                }
            }
        };

        public static StairBonus Hell = new StairBonus("hell", "Hellish Environment")
        {
            ModifyGenerator = generator =>
            {
                generator.Feelings.Add(LevelFeeling.Fire, +30);
                generator.Feelings.Add(LevelFeeling.Hell, +50);
                generator.Feelings.Add(LevelFeeling.Difficulty, +10);
            }
        };

        public static StairBonus Dungeon = new StairBonus("dungeon", "Dungeon")
        {
            ModifySet = set =>
            {
                set.Groups.Add(GroupGenerator.Dungeon);
            }
        };
        public static StairBonus SeaOfDirac = new StairBonus("sea_of_dirac", "Sea of Dirac")
        {
            ModifySet = set =>
            {
                set.Groups.Add(GroupGenerator.SeaOfDirac);
            }
        };

        public static StairBonus RedZone = new StairBonus("red_zone", "Red Zone")
        {
            ModifyGroupPre = group =>
            {
                group.CaveColor = new TileColor(new Color(64, 32, 8), new Color(255, 64, 64));
            }
        };
        public static StairBonus WideArea = new StairBonus("wide_area", "Wide Area")
        {
            ModifyGroupPre = group =>
            {
                group.ConnectionMin = (int)(group.ConnectionMin * 1.5);
                group.ConnectionMax = (int)(group.ConnectionMax * 2.0);
                foreach (var room in group.RoomTypes)
                {
                    room.DistanceMin = (int)(room.DistanceMin * 1.5);
                    room.DistanceMax = (int)(room.DistanceMax * 2.0);
                }
            }
        };
        public static StairBonus NarrowArea = new StairBonus("narrow_area", "Narrow Area")
        {
            ModifyGroupPre = group =>
            {
                group.ConnectionMin = 1;
                group.ConnectionMax = 2;
                foreach (var room in group.RoomTypes)
                {
                    room.DistanceMin = (int)(room.DistanceMin * 0.5);
                    room.DistanceMax = (int)(room.DistanceMax * 0.75);
                }
            }
        };
        public static StairBonus Fortified = new StairBonus("fortified", "Fortified")
        {
            ModifyGroupPre = group =>
            {
                group.RoomTypes.Add(new RoomType.Castle(3, 7, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, 30));
            }
        };
        public static StairBonus NecroRuin = new StairBonus("necro_ruin", "Necro Ruin")
        {
            ModifyGroupPre = group =>
            {
                group.RoomTypes.Add(new RoomType.Castle(3, 7, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, 30));
                group.BrickColor = new TileColor(new Color(88, 60, 91), new Color(175, 184, 199));
                group.CaveColor = Desaturate(group.CaveColor, 0.5f);
                group.Spawns.Add(EnemySpawn.Skeleton);
            }
        };

        private static TileColor Desaturate(TileColor color, float slide)
        {
            ColorMatrix colorMatrix = ColorMatrix.Saturate(slide);
            return new TileColor(colorMatrix.Transform(color.Background), colorMatrix.Transform(color.Foreground));
        }

        public static StairBonus GetStairBonus(string id)
        {
            return AllStairBonuses.Find(x => x.ID == id);
        }
    }
}
