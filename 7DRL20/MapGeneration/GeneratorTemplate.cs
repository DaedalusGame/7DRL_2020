using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    abstract class GeneratorTemplate
    {
        protected SceneGame World;
        protected Random Random;
        public Map Map;
        public List<IGrouping<RoomGroup, Tile>> Rooms;
        public LevelFeelingSet Feelings = new LevelFeelingSet();

        public abstract void Build(SceneGame world);

        public void SetFeelings(LevelFeelingSet feelings)
        {
            Feelings = feelings.Copy();
        }

        public Tile BuildStairRoom()
        {
            var stairRoom = Rooms.PickAndRemoveBest(x => -x.Key.Connections.Count, Random);
            var stairRoomFloors = stairRoom.Where(tile => !tile.Solid).Shuffle(Random);

            var stairTile = stairRoomFloors.First();
            return stairTile;
        }

        public Tile BuildStairRoom(Type type)
        {
            var stairRoom = Rooms.PickAndRemoveBest(x => (x.Key.Origin.Group.GetType() == type ? 100 : 0) - x.Key.Connections.Count, Random);
            var stairRoomFloors = stairRoom.Where(tile => !tile.Solid).Shuffle(Random);

            var stairTile = stairRoomFloors.First();
            return stairTile;
        }

        public Tile GetStartRoom()
        {
            var stairRoom = Rooms.PickAndRemoveBest(x => x.Key.Connections.Count, Random);
            var stairRoomFloors = stairRoom.Where(tile => !tile.Solid).Shuffle(Random);

            var stairTile = stairRoomFloors.First();
            return stairTile;
        }
    }

    class TemplateRandomLevel : GeneratorTemplate
    {
        public GroupSet GroupGenerator;
        int Seed;

        public TemplateRandomLevel(GroupSet groupGenerator, int seed)
        {
            GroupGenerator = groupGenerator;
            Seed = seed;
        }

        public override void Build(SceneGame world)
        {
            World = world;
            Random = new Random(Seed);

            Map = world.CreateMap(100, 100);

            MapGenerator generator = new MapGenerator(Map.Width, Map.Height, Seed, GroupGenerator, Feelings);
            generator.SetupDefaultOres();
            generator.Generate();
            generator.Print(Map);

            Rooms = generator.GetRooms(Map).ToList();

            int followups = 1;
            if (Random.NextDouble() < 0.4)
                followups = Random.Next(2, 4);
            for(int i = 0; i < followups; i++)
            {
                var nextStair = BuildStairRoom();
                var group = nextStair.Group;
                StairDown stairTile = new StairDown()
                {
                    Type = StairType.Random,
                    Seed = Random.Next(),
                };
                stairTile.InitBonuses();
                nextStair.Replace(stairTile);
            }
        }
    }

    class TemplateHome : GeneratorTemplate
    {
        int Seed;

        public override void Build(SceneGame world)
        {
            World = world;
            Random = new Random(Seed);

            Map = world.CreateMap(100, 100);

            MapGenerator generatorHome = new MapGenerator(Map.Width, Map.Height, Seed, new GroupSet(GroupGenerator.Home), new LevelFeelingSet())
            {
                PointCount = 7,
                PointDeviation = 35,
            };
            generatorHome.Generate();
            generatorHome.Print(Map);

            Rooms = generatorHome.GetRooms(Map).ToList();

            BuildSmeltery(Rooms);
            BuildStoreRoom(Rooms);
        }

        private void BuildStoreRoom(List<IGrouping<RoomGroup, Tile>> rooms)
        {
            var storeRoom = rooms.PickAndRemoveBest(x => x.Any(tile => tile is FloorPlank || tile is FloorCarpet) ? -x.Key.Connections.Count : -9999, Random);
            var storeRoomFloors = storeRoom.Where(tile => !tile.Solid).Shuffle(Random);

            Material[] possibleFuels = new[] { Material.Coal };
            Material[] possibleMaterials = new[] { Material.Karmesine, Material.Ovium, Material.Jauxum, Material.Basalt };
            for (int i = 0; i < 30; i++)
            {
                if (storeRoomFloors.Count() <= i)
                    break;
                Material pick;
                if (i < 5)
                    pick = possibleFuels.Pick(Random);
                else
                    pick = possibleMaterials.Pick(Random);
                var pickFloor = storeRoomFloors.ElementAt(i);

                new Ore(World, pick, 100).MoveTo(pickFloor);
            }
        }

        private void BuildSmeltery(List<IGrouping<RoomGroup, Tile>> rooms)
        {
            var smeltery = rooms.PickAndRemoveBest(x => !x.Any(tile => tile is FloorPlank || tile is FloorCarpet) ? -x.Key.Connections.Count : -9999, Random);
            var smelteryFloors = smeltery.Where(tile => !tile.Solid).Shuffle(Random);

            var anvilTile = smelteryFloors.ElementAt(0);
            var smelterTile = smelteryFloors.ElementAt(1);

            anvilTile.PlaceOn(new Anvil());
            smelterTile.PlaceOn(new Smelter(World));
        }
    }
}
