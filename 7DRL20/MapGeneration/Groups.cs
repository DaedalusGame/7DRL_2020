using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine.MapGeneration
{
    abstract class GeneratorTechnique
    {
        public GeneratorTechnique()
        {
        }

        public abstract void Apply(GeneratorGroup group);

        public class Expand : GeneratorTechnique
        {
            public override void Apply(GeneratorGroup group)
            {
                group.Generator.Expand();
            }
        }

        public class GetCells : GeneratorTechnique
        {
            public List<GeneratorCell> Result = new List<GeneratorCell>();
            public Func<IEnumerable<GeneratorCell>> Generator;

            public GetCells(Func<IEnumerable<GeneratorCell>> generator)
            {
                Generator = generator;
            }

            public override void Apply(GeneratorGroup group)
            {
                Result.AddRange(Generator());
            }
        }

        public class MakeLakes : GeneratorTechnique
        {
            public int Distance = 10;
            public float SpreadChance = 0.8f;
            public GeneratorTile Liquid;
            public IEnumerable<GeneratorCell> Cells;

            public MakeLakes(GeneratorTile liquid, IEnumerable<GeneratorCell> cells)
            {
                Liquid = liquid;
                Cells = cells;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var cell in Cells)
                {
                    cell.AddSpread(new SpreadLake(null, Distance, SpreadChance, Liquid));
                }
            }
        }

        public class MakeDeepLakes : GeneratorTechnique
        {
            public int Distance = 10;
            public float SpreadChance = 0.8f;
            public GeneratorTile Liquid;
            public IEnumerable<GeneratorCell> Cells;

            public MakeDeepLakes(GeneratorTile liquid, IEnumerable<GeneratorCell> cells)
            {
                Liquid = liquid;
                Cells = cells;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var cell in Cells)
                {
                    cell.AddSpread(new SpreadDeepLake(null, Distance, SpreadChance, Liquid));
                }
            }
        }

        public class MakeCarpets : GeneratorTechnique
        {
            public int Distance = 10;
            public GeneratorTile Carpet;
            public IEnumerable<GeneratorCell> Cells;

            public MakeCarpets(GeneratorTile carpet, IEnumerable<GeneratorCell> cells)
            {
                Carpet = carpet;
                Cells = cells;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var cell in Cells)
                {
                    cell.AddSpread(new SpreadCarpet(null, Distance, Carpet));
                }
            }
        }

        public class ConnectCarpets : GeneratorTechnique
        {
            public Func<GeneratorCell, bool> Filter = (cell) => cell.Tile == GeneratorTile.Carpet;

            public ConnectCarpets()
            {
            }

            public override void Apply(GeneratorGroup group)
            {
                bool isCarpet(GeneratorCell cell) => cell != null && Filter(cell);

                var carpets = group.GetCells().Where(cell => Filter(cell));
                foreach (var carpet in carpets)
                {
                    carpet.Connectivity = Connectivity.None;
                    if (isCarpet(carpet.GetNeighbor(0, 1)))
                        carpet.Connectivity |= Connectivity.South;
                    if (isCarpet(carpet.GetNeighbor(1, 0)))
                        carpet.Connectivity |= Connectivity.East;
                    if (isCarpet(carpet.GetNeighbor(0, -1)))
                        carpet.Connectivity |= Connectivity.North;
                    if (isCarpet(carpet.GetNeighbor(-1, 0)))
                        carpet.Connectivity |= Connectivity.West;
                    if (isCarpet(carpet.GetNeighbor(1, 1)))
                        carpet.Connectivity |= Connectivity.SouthEast;
                    if (isCarpet(carpet.GetNeighbor(1, -1)))
                        carpet.Connectivity |= Connectivity.NorthEast;
                    if (isCarpet(carpet.GetNeighbor(-1, -1)))
                        carpet.Connectivity |= Connectivity.NorthWest;
                    if (isCarpet(carpet.GetNeighbor(-1, 1)))
                        carpet.Connectivity |= Connectivity.SouthWest;
                }
            }
        }

        public class MakePlants : GeneratorTechnique
        {
            public int Distance = 5;
            public float Chance = 0.0f;
            public GeneratorTile Plant;
            public IEnumerable<GeneratorCell> Cells;

            public MakePlants(GeneratorTile plant, IEnumerable<GeneratorCell> cells)
            {
                Plant = plant;
                Cells = cells;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var cell in Cells)
                {
                    cell.AddSpread(new SpreadPlant(null, Distance, Chance, Plant));
                }
            }
        }

        public class MakeBridges : GeneratorTechnique
        {
            public float ConnectChance = 1.0f;
            public float BreakChance = 0.0f;
            public GeneratorTile Bridge;
            public Func<GeneratorCell, bool> Filter = (cell) => cell.Tile.HasTag(TileTag.Liquid);

            public MakeBridges(GeneratorTile bridge)
            {
                Bridge = bridge;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var room in group.Rooms)
                {
                    foreach (var connection in room.Connections)
                    {
                        if (group.Random.NextDouble() >= ConnectChance)
                            continue;
                        bool broken = false;
                        var path = group.GetPath(group.Generator, new Point(room.Origin.X, room.Origin.Y), new Point(connection.X, connection.Y));
                        foreach (var cell in path.Select(group.Generator.GetCell))
                        {
                            if (group.Random.NextDouble() <= BreakChance)
                                broken = !broken;
                            if (!broken && Filter(cell))
                            {
                                cell.Tile = Bridge;
                            }
                        }
                    }
                }
            }
        }

        public class MakeBigBridges : GeneratorTechnique
        {
            public int Distance;
            public float ConnectChance = 1.0f;
            public float BreakChance = 0.0f;
            public GeneratorTile Bridge;
            public Func<GeneratorCell, bool> Filter = (cell) => cell.Tile.HasTag(TileTag.Liquid);

            public MakeBigBridges(GeneratorTile bridge, int distance)
            {
                Bridge = bridge;
                Distance = distance;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var room in group.Rooms)
                {
                    foreach (var connection in room.Connections)
                    {
                        bool broken = false;
                        var path = group.GetPath(group.Generator, new Point(room.Origin.X, room.Origin.Y), new Point(connection.X, connection.Y));
                        if (group.Generator.Random.NextDouble() < ConnectChance)
                            foreach (var cell in path.Select(group.Generator.GetCell))
                            {
                                if (group.Random.NextDouble() <= BreakChance)
                                    broken = !broken;
                                if (!broken && Filter(cell))
                                {
                                    cell.Tile = Bridge;
                                    cell.AddSpread(new SpreadPlatform(null, Distance, Bridge));
                                }
                            }
                    }
                }
            }
        }

        public class MakeGlow : GeneratorTechnique
        {
            public int Distance = 5;
            public float Chance = 0.6f;
            public bool Glow;
            public IEnumerable<GeneratorCell> Cells;

            public MakeGlow(bool glow, IEnumerable<GeneratorCell> cells)
            {
                Glow = glow;
                Cells = cells;
            }

            public override void Apply(GeneratorGroup group)
            {
                foreach (var cell in Cells)
                {
                    cell.AddSpread(new SpreadGlow(null, Distance, Chance, Glow));
                }
            }
        }

        public class MakeOutsideRooms : GeneratorTechnique
        {
            public int Count;
            public int Distance = 2;
            public GeneratorTile Floor;
            public GeneratorTile Wall;
            public Func<GeneratorCell, bool> WallFilter = (cell) => cell.Tile.HasTag(TileTag.Wall) && cell.GetNeighbors().Where(neighbor => neighbor != null).Any(neighbor => neighbor.Tile.HasTag(TileTag.Floor));

            public MakeOutsideRooms(GeneratorTile floor, GeneratorTile wall, int count, Func<GeneratorCell, bool> wallFilter)
            {
                Floor = floor;
                Wall = wall;
                Count = count;
                WallFilter = wallFilter;
            }

            public override void Apply(GeneratorGroup group)
            {
                    var borderWalls = group.GetCells().Where(WallFilter).ToHashSet();
                    var hidden = group.Generator.GetAllCells().Where(cell => cell.Tile == GeneratorTile.Empty);
                    var validArea = hidden.Where(cell => cell.GetNeighbors().Any(neighbor => borderWalls.Contains(neighbor)));
                    validArea = validArea.Shuffle(group.Random);
                    foreach (var cell in validArea.Take(Count))
                    {
                        var border = cell.GetNeighbors().Intersect(borderWalls).ToList().Pick(group.Random); //Find a wall
                        var room = border.GetNeighbors().Where(neighbor => neighbor.Room != null && neighbor.Group != null).FirstOrDefault();
                        if (room != null)
                        {
                            cell.Group = room.Group;
                            cell.AddSpread(new SpreadVault(room, Distance, Floor, Wall));
                            border.Tile = Floor; //Punch a hole
                        }
                    }
            }
        }

        public class MakeStatues : GeneratorTechnique
        {
            public int Distance = 3;
            public double StatueRoomChance = 0.2;
            public int GridHorizontalMin = 3;
            public int GridHorizontalMax = 5;
            public int GridVerticalMin = 3;
            public int GridVerticalMax = 5;
            public GeneratorTile Statue;
            public IEnumerable<GeneratorCell> Cells;

            public MakeStatues(GeneratorTile statue, IEnumerable<GeneratorCell> cells)
            {
                Statue = statue;
                Cells = cells;
            }

            public override void Apply(GeneratorGroup group)
            {
                var rooms = group.GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

                foreach (var room in rooms)
                {
                    if (group.Generator.Random.NextDouble() < StatueRoomChance)
                    {
                        int gridHorizontal = group.Generator.Random.Next(GridHorizontalMin, GridHorizontalMax + 1);
                        int gridVertical = group.Generator.Random.Next(GridVerticalMin, GridVerticalMax + 1);
                        room.Origin.AddSpread(new SpreadStatues(null, Distance, gridHorizontal, gridVertical, Statue));
                    }
                }
            }
        }
    }

    [SerializeInfo]
    abstract class GeneratorGroup
    {
        public MapGenerator Generator;
        public HashSet<GeneratorGroup> ConnectedGroups = new HashSet<GeneratorGroup>();
        public HashSet<GeneratorCell> Cells = new HashSet<GeneratorCell>();
        public IEnumerable<RoomGroup> Rooms => GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

        public Random Random => Generator.Random;

        public Color Color;
        public TileColor CaveColor;
        public TileColor BrickColor;
        public TileColor WoodColor;
        public AdvancedColor GlowColor = AdvancedColor.Empty;
        public ColorMatrix Atmosphere = ColorMatrix.Identity;
        public List<EnemySpawn> Spawns = new List<EnemySpawn>();

        public int ConnectionMin;
        public int ConnectionMax;
        public GeneratorTile ConnectionFloor;
        public GeneratorTile ConnectionWall;
        public List<RoomType> RoomTypes = new List<RoomType>();

        public List<AppliedBonus> Bonuses = new List<AppliedBonus>();
        public List<GeneratorTechnique> Techniques = new List<GeneratorTechnique>();
        public IEnumerator<GeneratorTechnique> RunningTechniques;

        protected GeneratorGroup(MapGenerator generator)
        {
            Generator = generator;
        }

        public void Connect(GeneratorGroup other)
        {
            this.ConnectedGroups.Add(other);
            other.ConnectedGroups.Add(this);
        }

        public bool IsConnected(GeneratorGroup other)
        {
            return ConnectedGroups.Contains(other);
        }

        public abstract void PlaceConnection(MapGenerator generator, GeneratorCell cell, int width);

        public virtual void PlaceRoom(MapGenerator generator, GeneratorCell cell)
        {
            var type = RoomTypes.PickWeighted(room => room.Weight, Random);
            type.Place(generator, cell);
        }

        public abstract IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b);

        public IEnumerable<GeneratorCell> GetCells()
        {
            return Cells.ToList();
        }

        public void Add(GeneratorCell generatorCell)
        {
            Cells.Add(generatorCell);
        }

        public void Remove(GeneratorCell generatorCell)
        {
            Cells.Remove(generatorCell);
        }

        private Rectangle Between(Point a, Point b)
        {
            return new Rectangle(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        protected virtual IEnumerable<GeneratorTechnique> GetTechniques()
        {
            return Enumerable.Empty<GeneratorTechnique>();
        }

        public void StartTechniques()
        {
            Techniques.AddRange(GetTechniques());
            RunningTechniques = Techniques.GetEnumerator();
        }

        public void RunTechnique()
        {
            if (RunningTechniques != null)
            {
                if (RunningTechniques.MoveNext())
                    RunningTechniques.Current.Apply(this);
                else
                    RunningTechniques = null;
            }
        }

        public GroupGenerator MakeTemplate()
        {
            return new GroupGenerator(Copy);
        }

        public abstract GeneratorGroup Copy(MapGenerator generator);

        public JToken WriteJson()
        {
            JObject json = new JObject();
            json["id"] = Serializer.GetID(this);
            json["colorCave"] = CaveColor.WriteJson();
            json["colorBrick"] = BrickColor.WriteJson();
            json["colorWood"] = WoodColor.WriteJson();
            json["colorGlow"] = GlowColor.WriteJson();
            JArray spawnArray = new JArray();
            foreach(var spawn in Spawns)
            {
                spawnArray.Add(spawn.ID);
            }
            json["spawns"] = spawnArray;
            return json;
        }

        public void ReadJson(JToken json)
        {
            CaveColor = new TileColor(json["colorCave"]);
            BrickColor = new TileColor(json["colorBrick"]);
            WoodColor = new TileColor(json["colorWood"]);
            GlowColor = new AdvancedColor(json["colorGlow"]);
            JArray spawnArray = json["spawns"] as JArray;
            foreach(var spawnJson in spawnArray)
            {
                Spawns.Add(EnemySpawn.GetSpawn(spawnJson.Value<string>()));
            }
        }

        #region Techniques
        protected void MakeLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(IsWall));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.Lava));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Lava));
            }
            Generator.Expand();
        }

        protected void MakeSuperLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Lava);
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadDeepLake(null, 10, 0.8f, GeneratorTile.SuperLava));
            }
            Generator.Expand();
        }

        protected void MakeHyperLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.SuperLava);
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadDeepLake(null, 5, 0.8f, GeneratorTile.HyperLava));
            }
            Generator.Expand();
        }

        protected void ShatterFloor()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(IsArtificialFloor).Where(cell => cell.GetNeighbors().Any(IsLiquid));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.Floor));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Floor));
            }
            Generator.Expand();
        }

        protected void MakeCarpets(int count, int size, GeneratorTile carpet, Func<GeneratorCell,bool> predicate)
        {
            var validArea = GetCells().Where(predicate);
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(count))
            {
                cell.AddSpread(new SpreadCarpet(null, size, carpet));
            }

            Generator.Expand();
        }

        protected void ConnectCarpets()
        {
            bool isCarpet(GeneratorCell cell) => cell != null && cell.Tile == GeneratorTile.Carpet;

            var carpets = GetCells().Where(cell => cell.Tile == GeneratorTile.Carpet);
            foreach (var carpet in carpets)
            {
                carpet.Connectivity = Connectivity.None;
                if (isCarpet(carpet.GetNeighbor(0, 1)))
                    carpet.Connectivity |= Connectivity.South;
                if (isCarpet(carpet.GetNeighbor(1, 0)))
                    carpet.Connectivity |= Connectivity.East;
                if (isCarpet(carpet.GetNeighbor(0, -1)))
                    carpet.Connectivity |= Connectivity.North;
                if (isCarpet(carpet.GetNeighbor(-1, 0)))
                    carpet.Connectivity |= Connectivity.West;
                if (isCarpet(carpet.GetNeighbor(1, 1)))
                    carpet.Connectivity |= Connectivity.SouthEast;
                if (isCarpet(carpet.GetNeighbor(1, -1)))
                    carpet.Connectivity |= Connectivity.NorthEast;
                if (isCarpet(carpet.GetNeighbor(-1, -1)))
                    carpet.Connectivity |= Connectivity.NorthWest;
                if (isCarpet(carpet.GetNeighbor(-1, 1)))
                    carpet.Connectivity |= Connectivity.SouthWest;
            }
        }

        protected void MakeDarkLava()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile.HasTag(TileTag.Floor)).Where(cell => cell.GetNeighbors().Any(IsWall));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.DarkLava));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.DarkLava));
            }
            Generator.Expand();
        }

        protected void MakeAcidLakes()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(IsWall));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.AcidPool));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.AcidPool));
            }
            Generator.Expand();
        }

        protected void MakeWaterLakes()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(IsWall));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.Water));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Water));
            }
            Generator.Expand();
        }

        protected void MakeWaterShallows()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(neighbor => neighbor.Tile == GeneratorTile.Water));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 10, 0.8f, GeneratorTile.WaterShallow));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.WaterShallow));
            }
            Generator.Expand();
        }

        protected void MakeCoral(GeneratorTile coral)
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(IsLiquid));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadPlant(null, 5, 0.0f, coral));
            }
            foreach (var cell in validArea.Skip(rooms / 3).Take(rooms))
            {
                cell.AddSpread(new SpreadPlant(null, 3, 0.0f, coral));
            }
            Generator.Expand();
        }

        protected void MakeBridges()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                foreach (var connection in room.Connections)
                {
                    var path = GetPath(Generator, new Point(room.Origin.X, room.Origin.Y), new Point(connection.X, connection.Y));
                    foreach (var cell in path.Select(Generator.GetCell))
                    {
                        if (cell.Tile.HasTag(TileTag.Liquid))
                        {
                            cell.Tile = GeneratorTile.Bridge;
                        }
                    }
                    Generator.WaitSoft();
                }
            }
        }

        protected void MakeLargeBogs()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                var cell = room.Origin;
                cell.AddSpread(new SpreadLake(null, 5, 0.8f, GeneratorTile.Bog));
            }

            Generator.Expand();
        }

        protected void MakeSmallBogs()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(IsWall));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms / 3))
            {
                cell.AddSpread(new SpreadLake(null, 3, 0.8f, GeneratorTile.Bog));
            }
            Generator.Expand();
        }

        protected void MakeStoneBridges()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                foreach (var connection in room.Connections)
                {
                    var path = GetPath(Generator, new Point(room.Origin.X, room.Origin.Y), new Point(connection.X, connection.Y));
                    if(Generator.Random.NextDouble() < 0.6)
                    foreach (var cell in path.Select(Generator.GetCell))
                    {
                        if (cell.Tile.HasTag(TileTag.Liquid))
                        {
                            cell.Tile = GeneratorTile.FloorBrick;
                            cell.AddSpread(new SpreadPlatform(null, 2, GeneratorTile.FloorBrick));
                        }
                    }
                    Generator.WaitSoft();
                }
            }

            Generator.Expand();
        }

        protected void MakeGlowingFloor()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Floor).Where(cell => cell.GetNeighbors().Any(IsLiquid));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms * 3))
            {
                cell.AddSpread(new SpreadGlow(null, 5, 0.6f, true));
            }
            Generator.Expand();
        }

        protected void MakeGlowingWall()
        {
            int rooms = Rooms.Count();
            var validArea = GetCells().Where(cell => cell.Tile == GeneratorTile.Wall).Where(cell => cell.GetNeighbors().Any(IsLiquid));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms))
            {
                cell.AddSpread(new SpreadGlow(null, 5, 0.6f, true));
            }
            Generator.Expand();
        }

        protected void MakeOutsideRooms()
        {
            int rooms = Rooms.Count();
            var borderWalls = GetCells().Where(IsWall).Where(cell => cell.GetNeighbors().Where(neighbor => neighbor != null).Any(IsFloor)).ToHashSet();
            var hidden = Generator.GetAllCells().Where(cell => cell.Tile == GeneratorTile.Empty);
            var validArea = hidden.Where(cell => cell.GetNeighbors().Any(neighbor => borderWalls.Contains(neighbor)));
            validArea = validArea.Shuffle(Random);
            foreach (var cell in validArea.Take(rooms))
            {
                var border = cell.GetNeighbors().Intersect(borderWalls).ToList().Pick(Generator.Random); //Find a wall
                var room = border.GetNeighbors().Where(neighbor => neighbor.Room != null && neighbor.Group != null).FirstOrDefault();
                if (room != null)
                {
                    cell.Group = room.Group;
                    cell.AddSpread(new SpreadVault(room, 2, GeneratorTile.FloorBrick, GeneratorTile.WallBrick));
                    border.Tile = GeneratorTile.FloorBrick; //Punch a hole
                }
            }
            Generator.Expand();
        }
        #endregion

        protected bool IsFloor(GeneratorCell cell)
        {
            return cell.Tile.HasTag(TileTag.Floor);
        }

        protected bool IsArtificialFloor(GeneratorCell cell)
        {
            return cell.Tile.HasTag(TileTag.Floor) && cell.Tile.HasTag(TileTag.Artificial);
        }

        protected bool IsWall(GeneratorCell cell)
        {
            return cell.Tile.HasTag(TileTag.Wall);
        }

        protected bool IsArtificialWall(GeneratorCell cell)
        {
            return cell.Tile.HasTag(TileTag.Wall) && cell.Tile.HasTag(TileTag.Artificial);
        }

        protected bool IsLiquid(GeneratorCell cell)
        {
            return cell.Tile.HasTag(TileTag.Liquid);
        }

        protected bool FloorNeighbor(GeneratorCell cell)
        {
            return HasNeighbor(cell, IsFloor);
        }

        protected bool WallNeighbor(GeneratorCell cell)
        {
            return HasNeighbor(cell, IsWall);
        }

        protected bool LiquidNeighbor(GeneratorCell cell)
        {
            return HasNeighbor(cell, IsLiquid);
        }

        protected bool HasNeighbor(GeneratorCell cell, Func<GeneratorCell,bool> neighborCheck)
        {
            return cell.GetNeighbors().Where(neighbor => neighbor != null).Any(neighborCheck);
        }
    }

    abstract class RoomType
    {
        public int DistanceMin;
        public int DistanceMax;
        public int Weight;

        public RoomType(int min, int max, int weight)
        {
            DistanceMin = min;
            DistanceMax = max;
            Weight = weight;
        }

        public abstract void Place(MapGenerator generator, GeneratorCell cell);

        public class Castle : RoomType
        {
            GeneratorTile Floor;
            GeneratorTile Wall;

            public Castle(int min, int max, GeneratorTile floor, GeneratorTile wall, int weight) : base(min, max, weight)
            {
                Floor = floor;
                Wall = wall;
            }

            public override void Place(MapGenerator generator, GeneratorCell cell)
            {
                cell.AddSpread(new SpreadCastle(null, generator.Random.Next(DistanceMin, DistanceMax + 1), Floor, Wall));
            }
        }

        public class Cave : RoomType
        {
            GeneratorTile Floor;
            GeneratorTile Wall;

            public Cave(int min, int max, GeneratorTile floor, GeneratorTile wall, int weight) : base(min, max, weight)
            {
                Floor = floor;
                Wall = wall;
            }

            public override void Place(MapGenerator generator, GeneratorCell cell)
            {
                cell.AddSpread(new SpreadCave(null, generator.Random.Next(DistanceMin, DistanceMax + 1), Floor, Wall));
            }
        }

        public class Tower : RoomType
        {
            int RadiusMin;
            int RadiusMax;
            GeneratorTile Floor;
            GeneratorTile Wall;

            public Tower(int minDist, int maxDist, int minRadius, int maxRadius, GeneratorTile floor, GeneratorTile wall, int weight) : base(minDist, maxDist, weight)
            {
                RadiusMin = minRadius;
                RadiusMax = maxRadius;
                Floor = floor;
                Wall = wall;
            }

            public override void Place(MapGenerator generator, GeneratorCell cell)
            {
                cell.AddSpread(new SpreadTower(null, generator.Random.Next(DistanceMin, DistanceMax + 1), generator.Random.Next(RadiusMin, RadiusMax + 1) + 0.5f, Floor, Wall));
            }
        }
    }

    class Castle : GeneratorGroup
    {
        public Castle(MapGenerator generator) : base(generator)
        {
            ConnectionMin = 2;
            ConnectionMax = 2;
            ConnectionFloor = GeneratorTile.FloorBrick;
            ConnectionWall = GeneratorTile.WallBrick;
            RoomTypes = new List<RoomType>() {
                new RoomType.Castle(3, 7, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, 80),
                new RoomType.Tower(6, 10, 3, 6, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, 20),
            };
        }

        [Construct("group_castle")]
        public static Castle Construct(Context context)
        {
            return new Castle(null);
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new Castle(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            var costMap = new CostMapFunction(generator.GetWeightStraight);
            var dijkstraMap = Util.Dijkstra(a, b, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, costMap, generator.GetNeighbors(Point.Zero));
            return dijkstraMap.FindPath(b);
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell, int width)
        {
            cell.AddSpread(new SpreadCastle(null, width, ConnectionFloor, ConnectionWall, false));
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            var statueRooms = new GeneratorTechnique.GetCells(() => Rooms.Select(room => room.Origin));
            yield return statueRooms;
            yield return new GeneratorTechnique.MakeStatues(GeneratorTile.Statue, statueRooms.Result.Take(rooms / 3)) { Distance = 3, StatueRoomChance = 0.2f };
            yield return new GeneratorTechnique.Expand();
        }

        private void MakeStatues()
        {
            var rooms = GetCells().Where(cell => cell.Room != null).Select(cell => cell.Room).Distinct();

            foreach (var room in rooms)
            {
                if (Generator.Random.NextDouble() < 0.2)
                    room.Origin.AddSpread(new SpreadStatues(null, 3, Generator.Random.Next(3, 5), Generator.Random.Next(3, 5), GeneratorTile.Statue));
            }

            Generator.Expand();
        }
    }

    class Tower : GeneratorGroup
    {
        public Tower(MapGenerator generator) : base(generator)
        {
            ConnectionMin = 2;
            ConnectionMax = 2;
            ConnectionFloor = GeneratorTile.FloorBrick;
            ConnectionWall = GeneratorTile.Empty;
            RoomTypes = new List<RoomType>() {
                new RoomType.Tower(6, 10, 3, 6, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, 100),
            };
        }

        [Construct("group_tower")]
        public static Tower Construct(Context context)
        {
            return new Tower(null);
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            var costMap = new CostMapFunction(generator.GetWeightStraight);
            var dijkstraMap = Util.Dijkstra(a, b, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, costMap, generator.GetAllNeighbors(Point.Zero));
            return dijkstraMap.FindPath(b);
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell, int width)
        {
            cell.AddSpread(new SpreadCastle(null, width, ConnectionFloor, ConnectionWall, false));
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new Tower(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class Cave : GeneratorGroup
    {
        public Cave(MapGenerator generator) : base(generator)
        {
            ConnectionMin = 2;
            ConnectionMax = 5;
            ConnectionFloor = GeneratorTile.Floor;
            ConnectionWall = GeneratorTile.Wall;
            RoomTypes = new List<RoomType>() {
                new RoomType.Cave(3, 10, GeneratorTile.Floor, GeneratorTile.Wall, 100),
            };
        }

        [Construct("group_cave")]
        public static Cave Construct(Context context)
        {
            return new Cave(null);
        }

        public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
        {
            var costMap = new CostMapFunction(generator.GetWeightWavy);
            var dijkstraMap = Util.Dijkstra(a, b, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, costMap, generator.GetNeighbors(Point.Zero));
            return dijkstraMap.FindPath(b);

            //return Util.DrunkardWalk(a, b, generator.GetNeighbors, generator.Random);
        }

        public override void PlaceConnection(MapGenerator generator, GeneratorCell cell, int width)
        {
            cell.AddSpread(new SpreadCave(null, width, ConnectionFloor, ConnectionWall));
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            yield return new GeneratorTechnique.MakeBridges(GeneratorTile.Bridge);
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new Cave(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class CaveAcid : Cave
    {
        public CaveAcid(MapGenerator generator) : base(generator)
        {
        }

        [Construct("group_cave_acid")]
        public static CaveAcid Construct(Context context)
        {
            return new CaveAcid(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            //Lakes
            var lakes = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(WallNeighbor).Shuffle(Random));
            yield return lakes;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.AcidPool, lakes.Result.Take(rooms / 3)) { Distance = 10 };
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.AcidPool, lakes.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Coral
            var corals = new GeneratorTechnique.GetCells(() => Cells.Where(cell => cell.Tile == GeneratorTile.Floor).Where(LiquidNeighbor).Shuffle(Random));
            yield return corals;
            yield return new GeneratorTechnique.MakePlants(GeneratorTile.AcidCoral, corals.Result.Take(rooms / 3)) { Distance = 5 };
            yield return new GeneratorTechnique.MakePlants(GeneratorTile.AcidCoral, corals.Result.Skip(rooms / 3).Take(rooms)) { Distance = 3 };
            yield return new GeneratorTechnique.Expand();
            //Glow
            var glowFloors = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(LiquidNeighbor).Shuffle(Random));
            var glowWalls = new GeneratorTechnique.GetCells(() => Cells.Where(IsWall).Where(LiquidNeighbor).Shuffle(Random));
            yield return glowFloors;
            yield return glowWalls;
            yield return new GeneratorTechnique.MakeGlow(true, glowFloors.Result.Take(rooms * 3)) { Distance = 5 };
            yield return new GeneratorTechnique.MakeGlow(true, glowWalls.Result.Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Bridge
            yield return new GeneratorTechnique.MakeBridges(GeneratorTile.Bridge);
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new CaveAcid(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class CaveLava : Cave
    {
        public CaveLava(MapGenerator generator) : base(generator)
        {
        }

        [Construct("group_cave_lava")]
        public static CaveLava Construct(Context context)
        {
            return new CaveLava(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            //Lava
            var lakes = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(WallNeighbor).Shuffle(Random));
            yield return lakes;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Lava, lakes.Result.Take(rooms / 3)) { Distance = 10 };
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Lava, lakes.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Bridge
            yield return new GeneratorTechnique.MakeBridges(GeneratorTile.Bridge);
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new CaveLava(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class CaveMagma : Cave
    {
        public CaveMagma(MapGenerator generator) : base(generator)
        {
            ConnectionMin = 4;
            ConnectionMax = 6;
            RoomTypes = new List<RoomType>() {
                new RoomType.Cave(5, 13, GeneratorTile.Floor, GeneratorTile.Wall, 100),
            };
        }

        [Construct("group_cave_magma")]
        public static CaveMagma Construct(Context context)
        {
            return new CaveMagma(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            //Lava
            for (int i = 0; i < 2; i++)
            {
                var lakes = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(WallNeighbor).Shuffle(Random));
                yield return lakes;
                yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Lava, lakes.Result.Take(rooms / 3)) { Distance = 10 };
                yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Lava, lakes.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
                yield return new GeneratorTechnique.Expand();
            }
            var lava = new GeneratorTechnique.GetCells(() => Cells.Where(cell => cell.Tile == GeneratorTile.Lava).Shuffle(Random));
            yield return lava;
            for (int i = 0; i < 2; i++)
            {
                yield return new GeneratorTechnique.MakeDeepLakes(GeneratorTile.SuperLava, lava.Result.Take(rooms / 3)) { Distance = 10 };
                yield return new GeneratorTechnique.Expand();
            }
            var superLava = new GeneratorTechnique.GetCells(() => Cells.Where(cell => cell.Tile == GeneratorTile.SuperLava).Shuffle(Random));
            yield return superLava;
            for (int i = 0; i < 2; i++)
            {
                yield return new GeneratorTechnique.MakeDeepLakes(GeneratorTile.HyperLava, lava.Result.Take(rooms / 3)) { Distance = 5 };
                yield return new GeneratorTechnique.Expand();
            }
            yield return new GeneratorTechnique.MakeBridges(GeneratorTile.Bridge);
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new CaveMagma(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class CaveWater : Cave
    {
        public CaveWater(MapGenerator generator) : base(generator)
        {
        }

        [Construct("group_cave_water")]
        public static CaveWater Construct(Context context)
        {
            return new CaveWater(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            //Lakes
            var lakes = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(WallNeighbor).Shuffle(Random));
            yield return lakes;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Water, lakes.Result.Take(rooms / 3)) { Distance = 10 };
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Water, lakes.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Shallows
            var coasts = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(cell => HasNeighbor(cell, neighbor => neighbor.Tile == GeneratorTile.Water)).Shuffle(Random));
            yield return coasts;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.WaterShallow, coasts.Result.Take(rooms / 3)) { Distance = 10 };
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.WaterShallow, coasts.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Corals
            var corals = new GeneratorTechnique.GetCells(() => Cells.Where(cell => cell.Tile == GeneratorTile.Floor).Where(LiquidNeighbor).Shuffle(Random));
            yield return corals;
            yield return new GeneratorTechnique.MakePlants(GeneratorTile.Coral, corals.Result.Take(rooms / 3)) { Distance = 5 };
            yield return new GeneratorTechnique.MakePlants(GeneratorTile.Coral, corals.Result.Skip(rooms / 3).Take(rooms)) { Distance = 3 };
            yield return new GeneratorTechnique.Expand();
            //Glows
            var glowSpots = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(LiquidNeighbor).Shuffle(Random));
            yield return glowSpots;
            yield return new GeneratorTechnique.MakeGlow(true, glowSpots.Result.Take(rooms / 3)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Bridges
            yield return new GeneratorTechnique.MakeBridges(GeneratorTile.Bridge);
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new CaveWater(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class CaveBog : Cave
    {
        public CaveBog(MapGenerator generator) : base(generator)
        {
        }

        [Construct("group_cave_bog")]
        public static CaveBog Construct(Context context)
        {
            return new CaveBog(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            var bigBogs = new GeneratorTechnique.GetCells(() => Rooms.Select(room => room.Origin));
            yield return bigBogs;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Bog, bigBogs.Result) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            var smallBogs = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(WallNeighbor).Shuffle(Random));
            yield return smallBogs;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Bog, smallBogs.Result.Take(rooms / 3)) { Distance = 3 };
            yield return new GeneratorTechnique.Expand();
            yield return new GeneratorTechnique.MakeBridges(GeneratorTile.Bridge) { BreakChance = 0.2f, ConnectChance = 0.9f };
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new CaveBog(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class CastleDark : Castle
    {
        public CastleDark(MapGenerator generator) : base(generator)
        {
        }

        [Construct("group_castle_dark")]
        public static CastleDark Construct(Context context)
        {
            return new CastleDark(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            Func<GeneratorCell, bool> artificialFloor = (cell) => cell.Tile.HasTag(TileTag.Floor) && cell.Tile.HasTag(TileTag.Artificial);
            int rooms = Rooms.Count();
            //Create Carpets
            var carpetSpots = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Shuffle(Random));
            yield return carpetSpots;
            yield return new GeneratorTechnique.MakeCarpets(GeneratorTile.Carpet, carpetSpots.Result.Take(rooms / 3))
            {
                Distance = 5,
            };
            yield return new GeneratorTechnique.Expand();
            yield return new GeneratorTechnique.ConnectCarpets();
            //Create Dark Lava
            var lakes = new GeneratorTechnique.GetCells(() => GetCells().Where(IsFloor).Where(WallNeighbor).Shuffle(Random));
            yield return lakes;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.DarkLava, lakes.Result.Take(rooms / 3)) { Distance = 10 };
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.DarkLava, lakes.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Break Tiles
            var toBreak = new GeneratorTechnique.GetCells(() => GetCells().Where(IsArtificialFloor).Where(LiquidNeighbor).Shuffle(Random));
            yield return toBreak;
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Floor, toBreak.Result.Take(rooms / 3)) { Distance = 10 };
            yield return new GeneratorTechnique.MakeLakes(GeneratorTile.Floor, toBreak.Result.Skip(rooms / 3).Take(rooms)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Create Glowing Floors
            var glowSpots = new GeneratorTechnique.GetCells(() => Cells.Where(IsFloor).Where(LiquidNeighbor).Shuffle(Random));
            yield return glowSpots;
            yield return new GeneratorTechnique.MakeGlow(true, glowSpots.Result.Take(rooms / 3)) { Distance = 5 };
            yield return new GeneratorTechnique.Expand();
            //Make Bridges
            yield return new GeneratorTechnique.MakeBigBridges(GeneratorTile.FloorBrick, 2);
            yield return new GeneratorTechnique.Expand();
            //Make Outside Rooms
            yield return new GeneratorTechnique.MakeOutsideRooms(GeneratorTile.FloorBrick, GeneratorTile.WallBrick, rooms, cell => IsWall(cell) && FloorNeighbor(cell));
            yield return new GeneratorTechnique.Expand();
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new CastleDark(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }

    class Home : Castle
    {
        public Home(MapGenerator generator) : base(generator)
        {
            RoomTypes = new List<RoomType>() {
                new RoomType.Castle(3, 6, GeneratorTile.FloorBrick, GeneratorTile.WallBrick, 50),
                new RoomType.Castle(3, 6, GeneratorTile.FloorPlank, GeneratorTile.WallPlank, 50),
            };
        }

        [Construct("group_home")]
        public static Home Construct(Context context)
        {
            return new Home(null);
        }

        protected override IEnumerable<GeneratorTechnique> GetTechniques()
        {
            int rooms = Rooms.Count();
            var carpetSpots = new GeneratorTechnique.GetCells(() => Cells.Where(cell => cell.Tile == GeneratorTile.FloorPlank).Shuffle(Random));
            yield return carpetSpots;
            yield return new GeneratorTechnique.MakeCarpets(GeneratorTile.Carpet, carpetSpots.Result.Take(rooms / 3))
            {
                Distance = 5,
            };
            yield return new GeneratorTechnique.Expand();
            yield return new GeneratorTechnique.ConnectCarpets();
            yield return new GeneratorTechnique.MakeOutsideRooms(GeneratorTile.FloorBrick, GeneratorTile.WallBrick, rooms, cell => IsWall(cell) && FloorNeighbor(cell));
            yield return new GeneratorTechnique.Expand();
        }

        public override GeneratorGroup Copy(MapGenerator generator)
        {
            return new Home(generator)
            {
                CaveColor = CaveColor,
                BrickColor = BrickColor,
                WoodColor = WoodColor,
                GlowColor = GlowColor,
                Spawns = Spawns,
                Atmosphere = Atmosphere,
            };
        }
    }
}
