using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using RoguelikeEngine.MapGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    enum TileTag
    {
        Floor,
        Wall,
        Liquid,
        Ore,
        Artificial
    }

    delegate void PrintDelegate(MapGenerator generator, Tile tile, GeneratorGroup group);

    class GeneratorTile
    {
        public static GeneratorTile Empty = new GeneratorTile(' ', Color.Black, PrintWallCave, TileTag.Wall);
        public static GeneratorTile Floor = new GeneratorTile('.', Color.Gray, PrintFloorCave, TileTag.Floor);
        public static GeneratorTile FloorBrick = new GeneratorTile('.', Color.Gray, PrintFloorBrick, TileTag.Floor, TileTag.Artificial);
        public static GeneratorTile Wall = new GeneratorTile('X', Color.White, PrintWallCave, TileTag.Wall);
        public static GeneratorTile WallBrick = new GeneratorTile('#', Color.White, PrintWallBrick, TileTag.Wall, TileTag.Artificial);
        public static GeneratorTile OreDilithium = new GeneratorTile('G', Color.LightCyan, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreTiberium = new GeneratorTile('T', Color.Lime, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreBasalt = new GeneratorTile('B', Color.White, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreMeteorite = new GeneratorTile('M', Color.DarkGray, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreObsidiorite = new GeneratorTile('D', Color.Purple, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreKarmesine = new GeneratorTile('K', Color.IndianRed, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreOvium = new GeneratorTile('O', Color.LightSteelBlue, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile OreJauxum = new GeneratorTile('J', Color.LimeGreen, PrintWallCave, TileTag.Wall, TileTag.Ore);
        public static GeneratorTile AcidPool = new GeneratorTile('.', Color.GreenYellow, PrintAcid, TileTag.Liquid);
        public static GeneratorTile Coral = new GeneratorTile('.', Color.Pink, PrintCoral, TileTag.Floor);
        public static GeneratorTile AcidCoral = new GeneratorTile('.', Color.LightGoldenrodYellow, PrintAcidCoral, TileTag.Floor);
        public static GeneratorTile Bridge = new GeneratorTile('.', Color.Brown, PrintBridge, TileTag.Floor, TileTag.Artificial);
        public static GeneratorTile Statue = new GeneratorTile('.', Color.LightGray, PrintFloorBrick, TileTag.Floor, TileTag.Artificial);
        public static GeneratorTile Water = new GeneratorTile('.', Color.Blue, PrintWater, TileTag.Liquid);
        public static GeneratorTile WaterShallow = new GeneratorTile('.', Color.Blue, PrintWaterShallow, TileTag.Liquid);
        public static GeneratorTile Lava = new GeneratorTile('.', Color.Red, PrintLava, TileTag.Liquid);
        public static GeneratorTile SuperLava = new GeneratorTile('.', Color.Orange, PrintSuperLava, TileTag.Liquid);
        public static GeneratorTile HyperLava = new GeneratorTile('.', Color.Yellow, PrintHyperLava, TileTag.Liquid);

        public char Character;
        public Color Color;
        public PrintDelegate Print;
        public HashSet<TileTag> Tags;

        public GeneratorTile(char character, Color color, PrintDelegate print, params TileTag[] tags)
        {
            Character = character;
            Color = color;
            Print = print;
            Tags = new HashSet<TileTag>(tags);
        }

        public bool HasTag(TileTag tag)
        {
            return Tags.Contains(tag);
        }

        private static void PrintWallCave(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new WallCave());
        }

        private static void PrintWallBrick(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new WallBrick());
        }

        private static void PrintFloorCave(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new FloorCave());
        }

        private static void PrintFloorBrick(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new FloorTiles());
        }

        private static void PrintBridge(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new FloorBridge());
        }


        private static void PrintAcid(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new AcidPool());
        }

        private static void PrintAcidCoral(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new FloorCave());
            tile.PlaceOn(new AcidCoral());
        }

        private static void PrintCoral(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new FloorCave());
            tile.PlaceOn(new Coral());
        }

        private static void PrintWater(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new Water());
        }

        private static void PrintWaterShallow(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new WaterShallow());
        }

        private static void PrintLava(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new Lava());
        }

        private static void PrintSuperLava(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new SuperLava());
        }

        private static void PrintHyperLava(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new HyperLava());
        }
    }

    abstract class CollapseTile
    {
        public MapGenerator Generator;
        public GeneratorCell Cell;
        public GeneratorCell Origin;

        protected CollapseTile(GeneratorCell origin)
        {
            Origin = origin;
        }

        public abstract void Collapse();
    }

    class RoomGroup
    {
        public GeneratorCell Origin;
        public Color Color;
        public List<GeneratorCell> Connections = new List<GeneratorCell>();

        public RoomGroup(GeneratorCell origin)
        {
            Origin = origin;
        }

        public void AddConnection(GeneratorCell cell)
        {
            Connections.Add(cell);
        }
    }

    class GeneratorCell
    {
        MapGenerator Generator;
        Random Random => Generator.Random;
        GeneratorGroup CurrentGroup;

        public GeneratorTile Tile;
        public int X, Y;
        public double Weight;
        public GeneratorGroup Group
        {
            get
            {
                return CurrentGroup;
            }
            set
            {
                CurrentGroup?.Remove(this);
                CurrentGroup = value;
                CurrentGroup?.Add(this);
            }
        }
        public RoomGroup Room;
        public bool Glowing;

        /*int _ExpansionDistance;
        int ExpansionGeneration;

        public int ExpansionDistance
        {
            get
            {
                if (ExpansionGeneration < Generator.ExpansionGeneration)
                    return 0;
                return _ExpansionDistance;
            }
            set
            {
                _ExpansionDistance = value;
                ExpansionGeneration = Generator.ExpansionGeneration;
            }
        }*/

        public GeneratorCell(MapGenerator generator, int x, int y, GeneratorTile tile)
        {
            Generator = generator;
            Tile = tile;
            X = x;
            Y = y;
        }

        public GeneratorCell GetNeighbor(int dx, int dy)
        {
            if (Generator.InBounds(new Point(X + dx, Y + dy)))
                return Generator.Cells[X + dx, Y + dy];
            else
                return null;
        }

        public IEnumerable<GeneratorCell> GetNeighbors()
        {
            yield return GetNeighbor(0, -1);
            yield return GetNeighbor(0, +1);
            yield return GetNeighbor(-1, 0);
            yield return GetNeighbor(+1, 0);
        }

        public IEnumerable<GeneratorCell> GetAllNeighbors()
        {
            yield return GetNeighbor(0, -1);
            yield return GetNeighbor(0, +1);
            yield return GetNeighbor(-1, 0);
            yield return GetNeighbor(+1, 0);
            yield return GetNeighbor(-1, -1);
            yield return GetNeighbor(+1, +1);
            yield return GetNeighbor(-1, +1);
            yield return GetNeighbor(+1, -1);
        }

        public IEnumerable<GeneratorCell> GetStatueNeighbors(int gridHorizontal, int gridVertical)
        {
            yield return GetNeighbor(0, -gridVertical);
            yield return GetNeighbor(0, +gridVertical);
            yield return GetNeighbor(-gridHorizontal, 0);
            yield return GetNeighbor(+gridHorizontal, 0);
            yield return GetNeighbor(-gridHorizontal, -gridVertical);
            yield return GetNeighbor(+gridHorizontal, +gridVertical);
            yield return GetNeighbor(-gridHorizontal, +gridVertical);
            yield return GetNeighbor(+gridHorizontal, -gridVertical);
        }

        public IEnumerable<GeneratorCell> GetPlantNeighbors()
        {
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    if (Math.Abs(x) == Math.Abs(y) && Math.Abs(x) == 2)
                        continue;
                    if (Math.Abs(x) <= 1 && Math.Abs(y) <= 1)
                        continue;
                    yield return GetNeighbor(x, y);
                }
            }
        }

        public void AddSpread(SpreadTile spread)
        {
            spread.Cell = this;
            if (spread.Origin == null)
                spread.Origin = this;
            Generator.AddSpread(spread);
        }

        public void AddCollapse(CollapseTile collapse)
        {
            collapse.Cell = this;
            if (collapse.Origin == null)
                collapse.Origin = this;
            Generator.AddCollapse(collapse);
        }

        internal void NewRoom()
        {
            Room = new RoomGroup(this)
            {
                Color = Generator.GetRandomColor(),
            };
        }
    }

    enum WaitState
    {
        Wait,
        SoftWait,
    }

    class MapGenerator
    {
        public WaitState WaitState;

        public Random Random;
        public List<Point> Points = new List<Point>();
        public Point StartRoom;
        public GeneratorGroup StartRoomGroup;
        public List<GeneratorGroup> Groups = new List<GeneratorGroup>();
        Queue<SpreadTile> ToSpread = new Queue<SpreadTile>();
        Queue<CollapseTile> ToCollapse = new Queue<CollapseTile>();
        public GeneratorCell[,] Cells;
        public int ExpansionGeneration;

        public int Width => Cells.GetLength(0);
        public int Height => Cells.GetLength(1);

        public bool Done = true;

        public MapGenerator(int width, int height, int seed)
        {
            Random = new Random(seed);
            Cells = new GeneratorCell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cells[x, y] = new GeneratorCell(this, x, y, GeneratorTile.Empty);
                    Cells[x, y].Weight = Random.NextDouble();
                }
            }
        }

        public void AddCollapse(CollapseTile cell)
        {
            cell.Generator = this;
            ToCollapse.Enqueue(cell);
        }

        public void AddSpread(SpreadTile cell)
        {
            cell.Generator = this;
            ToSpread.Enqueue(cell);
        }

        private IEnumerable<Point> AllCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    yield return new Point(x, y);
                }
            }
        }

        public void Wait()
        {
        }

        public void WaitSoft()
        {
        }

        public void Print(Map map)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = Cells[x, y];
                    Tile mapTile = map.GetTile(x, y);
                    cell.Tile.Print(this, mapTile, cell.Group);
                    mapTile.NewTile.Glowing = cell.Glowing;
                    mapTile.Group = cell.Group;
                }
            }
        }

        public void Generate()
        {
            Done = false;
            SetupPoints(50, 10);
            Wait();
            ConnectPoints();
            Wait();
            ConnectGroups();
            Expand();
            RunGroupTechniques();
            Wait();
            GenerateOres(250 / 5, 0, GeneratorTile.OreDilithium);
            GenerateOres(30 / 5, 3, GeneratorTile.OreBasalt);
            GenerateOres(50 / 5, 3, GeneratorTile.OreKarmesine);
            GenerateOres(50 / 5, 3, GeneratorTile.OreOvium);
            GenerateOres(50 / 5, 3, GeneratorTile.OreJauxum);
            GenerateOres(10 / 5, 6, GeneratorTile.OreMeteorite);
            GenerateOres(30 / 5, 6, GeneratorTile.OreObsidiorite);
            GenerateOres(20 / 5, 6, GeneratorTile.OreTiberium);
            Expand();
            Wait();
            FloodFillGroup();
            /*string map = "";
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = Cells[x, y];
                    if (cell.Tile == GeneratorTile.Empty)
                        map += (char)('0' + ((cell.Group?.GetHashCode() ?? 0) & 63));
                    else
                        map += cell.Tile.Character;
                }
                map += '\n';
            }*/
            Done = true;
            Wait();
        }

        private void RunGroupTechniques()
        {
            foreach (GeneratorGroup group in Groups)
            {
                group.StartTechniques();
            }
            while (Groups.Any(group => group.Techniques != null))
            {
                foreach (GeneratorGroup group in Groups)
                {
                    group.RunTechnique();
                }
            }
        }

        struct FloodPoint
        {
            public Point Position;
            public GeneratorGroup Group;

            public FloodPoint(Point position, GeneratorGroup group)
            {
                Position = position;
                Group = group;
            }
        }

        public void FloodFillGroup()
        {
            HashSet<Point> visited = new HashSet<Point>();
            Queue<FloodPoint> toVisit = new Queue<FloodPoint>();

            var groupTiles = AllCells().Where(p => GetCell(p).Group != null);

            int countTillWait = 0;

            foreach (var tile in groupTiles)
            {
                toVisit.Enqueue(new FloodPoint(tile, GetCell(tile).Group));
                visited.Add(tile);
            }

            while (toVisit.Any())
            {
                FloodPoint visit = toVisit.Dequeue();

                if (!InMap(visit.Position))
                    continue;

                GetCell(visit.Position).Group = visit.Group;
                countTillWait++;
                if (countTillWait > 500)
                {
                    countTillWait = 0;
                    WaitSoft();
                }

                foreach (Point neighbor in GetNeighbors(visit.Position))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        toVisit.Enqueue(new FloodPoint(neighbor, visit.Group));
                    }
                }
            }
        }

        public void Expand()
        {
            while (ToSpread.Count > 0)
            {
                Spread();
                Collapse();
                Wait();
            }
            ExpansionGeneration++;
        }

        public void Spread()
        {
            int countTillWait = 0;
            while (ToSpread.Count > 0)
            {
                countTillWait++;
                if (countTillWait > 50)
                {
                    countTillWait = 0;
                    WaitSoft();
                }
                var tile = ToSpread.Dequeue();
                tile.Spread();
            }
        }

        public void Collapse()
        {
            while (ToCollapse.Count > 0)
            {
                var tile = ToCollapse.Dequeue();
                tile.Collapse();
            }
        }

        public Color GetRandomColor()
        {
            return Color.Red.RotateHue(Random.NextDouble());
        }

        public void SetupPoints(int count, int deviation)
        {
            while (Points.Count < count)
            {
                int x = Random.Next(deviation, Width - deviation);
                int y = Random.Next(deviation, Height - deviation);
                if (!Points.Contains(new Point(x, y)))
                    Points.Add(new Point(x, y));
            }
            StartRoomGroup = new Home(this)
            {
                CaveColor = new TileColor(new Color(64, 64, 64), new Color(160, 160, 160)),
                BrickColor = new TileColor(new Color(64, 64, 64), new Color(160, 160, 160))
            };
            /*Groups.Add(new CaveLava(this) //Fire Cave
            {
                CaveColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                Spawns = { EnemySpawn.Skeleton },
            });
            Groups.Add(new Cave(this) //Adamant Cave
            {
                CaveColor = new TileColor(new Color(128, 160, 160), new Color(32, 64, 32)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.PoisonBlob },
            });*/
            /*Groups.Add(new CaveAcid(this) //Acid Cave
            {
                CaveColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                GlowColor = (time) => Color.Lerp(Color.Black, Color.GreenYellow, 0.75f + 0.25f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.AcidBlob, EnemySpawn.Ctholoid, EnemySpawn.YellowDragon },
            });*/
            Groups.Add(new CaveWater(this) //Sea of Dirac
            {
                CaveColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                BrickColor = new TileColor(new Color(80, 80, 150), new Color(253, 234, 248)),
                GlowColor = (time) => Color.Lerp(Color.Black, new Color(34, 255, 255), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.PoisonBlob, EnemySpawn.GoreVala, EnemySpawn.BlueDragon, EnemySpawn.Ctholoid },
            });
            /*Groups.Add(new CaveLava(this) //Magma Mine
            {
                CaveColor = new TileColor(new Color(247, 211, 70), new Color(160, 35, 35)),
                Spawns = { EnemySpawn.BlastCannon, EnemySpawn.AcidBlob, EnemySpawn.Skeleton },
            });
            Groups.Add(new Castle(this) //Dungeon
            {
                CaveColor = new TileColor(new Color(128, 128, 128), new Color(160, 160, 160)),
                BrickColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.Vorrax, EnemySpawn.DeathKnight },
            });
            Groups.Add(new Tower(this) //Ivory Tower
            {
                CaveColor = new TileColor(new Color(108, 106, 79), new Color(188, 173, 139)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                Spawns = { EnemySpawn.Skeleton, EnemySpawn.DeathKnight, EnemySpawn.BlueDragon },
            });
            Groups.Add(new Castle(this) //Dark Castle
            {
                CaveColor = new TileColor(new Color(29, 50, 56), new Color(131, 138, 167)),
                BrickColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                GlowColor = (time) => Color.Lerp(new Color(62, 79, 2), new Color(227, 253, 138), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawns = { EnemySpawn.DeathKnight, EnemySpawn.BlastCannon, EnemySpawn.Ctholoid },
            });*/
            var i = 0;
            IEnumerable<Point> shuffled = Points.Shuffle(Random);
            var toAssign = shuffled.Take(Groups.Count);
            foreach (var cell in toAssign.Select(GetCell))
            {
                cell.Group = Groups[i];
                i++;
            }
            foreach (var cell in Points.Select(GetCell))
            {
                cell.NewRoom();
            }
            /*StartRoom = shuffled.Last();
            Points.Remove(StartRoom);
            var startCell = GetCell(StartRoom);
            startCell.Group = StartRoomGroup;
            StartRoomGroup.PlaceRoom(this, startCell);*/
        }

        public void ConnectPoints()
        {
            var uncolored = Points.Where(point => Cells[point.X, point.Y].Group == null);
            var colored = Points.Where(point => Cells[point.X, point.Y].Group != null);

            var connections = new List<Tuple<Point, Point>>();

            while (uncolored.Any())
            {
                var pairs = colored.SelectMany(x => uncolored.Select(y => Tuple.Create(x, y)));
                var closest = pairs.WithMin(pair => GetDistance(pair.Item1, pair.Item2));

                connections.Add(closest);
                GetCell(closest.Item2).Group = GetCell(closest.Item1).Group;
            }

            foreach (var point in colored.Select(GetCell))
            {
                point.Group.PlaceRoom(this, point);
            }

            foreach (var pair in connections)
            {
                Connect(pair.Item1, pair.Item2);
                WaitSoft();
            }
        }

        public void ConnectGroups()
        {
            var groupIds = new Dictionary<GeneratorGroup, int>();
            var id = 0;
            foreach (var group in Groups)
            {
                groupIds.Add(group, id++);
            }

            var pairs = Points.SelectMany(x => Points.Where(y => IsUngrouped(x, y)).Select(y => Tuple.Create(x, y)));
            var unconnectedPairs = pairs.Where(pair => groupIds[GetCell(pair.Item1).Group] != groupIds[GetCell(pair.Item2).Group]);

            var test = pairs.All(pair => IsUngrouped(pair.Item1, pair.Item2));

            while (unconnectedPairs.Any())
            {
                var pick = unconnectedPairs.WithMin(pair => GetDistance(pair.Item1, pair.Item2));
                var cellA = GetCell(pick.Item1);
                var cellB = GetCell(pick.Item2);
                foreach (var key in groupIds.Keys.ToList())
                {
                    var groupId = groupIds[key];
                    if (groupId == groupIds[cellA.Group] || groupId == groupIds[cellB.Group])
                    {
                        groupIds[key] = groupIds[cellB.Group];
                    }
                }
                groupIds[cellA.Group] = groupIds[cellB.Group];
                Connect(pick.Item1, pick.Item2);
                WaitSoft();
            }
        }

        public void GenerateOres(int times, int size, GeneratorTile ore)
        {
            var validTiles = AllCells().Select(GetCell).Where(cell => cell.Tile == GeneratorTile.Empty).ToList();

            for (int i = 0; i < times; i++)
            {
                var cell = validTiles.Pick(Random);
                if (cell.Tile != GeneratorTile.Empty)
                    continue;
                cell.Tile = ore;
                cell.AddSpread(new SpreadOre(null, size, 0.6f, ore));
            }
        }

        public void GenerateStartRoom()
        {
            var validTiles = AllCells().Select(GetCell).Where(cell => cell.Tile == GeneratorTile.Empty).Where(cell => cell.X >= 10 && cell.Y >= 10 && cell.X < Width - 10 && cell.Y < Height - 10).ToList();

            var startCell = validTiles.Pick(Random);
            StartRoomGroup.PlaceRoom(this, startCell);
            StartRoom = new Point(startCell.X, startCell.Y);
        }

        private bool IsUngrouped(Point a, Point b)
        {
            var cellA = GetCell(a);
            var cellB = GetCell(b);
            return cellA.Group != cellB.Group;
        }

        private bool IsConnected(Point a, Point b)
        {
            var cellA = GetCell(a);
            var cellB = GetCell(b);
            return cellA.Group.IsConnected(cellB.Group);
        }

        public void Connect(Point a, Point b)
        {
            var baseTile = Cells[a.X, a.Y];
            var otherTile = Cells[b.X, b.Y];
            var group = baseTile.Group;
            var path = group.GetPath(this, a, b);

            foreach (var cell in path.Select(GetCell))
            {
                cell.Tile = GeneratorTile.Floor;
                cell.Group = group;
                cell.Group.PlaceConnection(this, cell);
            }

            baseTile.Room.AddConnection(otherTile);
            otherTile.Room.AddConnection(baseTile);
        }

        public GeneratorCell GetCell(Point point)
        {
            return Cells[point.X, point.Y];
        }

        public bool InBounds(Point point)
        {
            return point.X > 0 && point.Y > 0 && point.X < Width - 1 && point.Y < Height - 1;
        }

        private bool InMap(Point point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X <= Width - 1 && point.Y <= Height - 1;
        }

        public double GetWeightStraight(Point start, Point end)
        {
            return 1;
        }

        public double GetWeightWavy(Point start, Point end)
        {
            return Cells[end.X, end.Y].Weight;
        }

        public IEnumerable<Point> GetNeighbors(Point point)
        {
            yield return new Point(point.X, point.Y - 1);
            yield return new Point(point.X, point.Y + 1);
            yield return new Point(point.X - 1, point.Y);
            yield return new Point(point.X + 1, point.Y);
        }

        public IEnumerable<Point> GetRandomNeighbors(Point point, double chance)
        {
            var neighbors = GetNeighbors(point).Shuffle();
            yield return neighbors.First();
            foreach (var neighbor in neighbors.Skip(1))
                if (Random.NextDouble() < chance)
                    yield return neighbor;
        }

        public IEnumerable<Point> GetAllNeighbors(Point point)
        {
            yield return new Point(point.X, point.Y - 1);
            yield return new Point(point.X, point.Y + 1);
            yield return new Point(point.X - 1, point.Y);
            yield return new Point(point.X + 1, point.Y);
            yield return new Point(point.X - 1, point.Y - 1);
            yield return new Point(point.X + 1, point.Y + 1);
            yield return new Point(point.X - 1, point.Y + 1);
            yield return new Point(point.X + 1, point.Y - 1);
        }

        private double GetDistance(Point a, Point b)
        {
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public void Update()
        {

        }
    }
}
