using Microsoft.Xna.Framework;
using RoguelikeEngine.Enemies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class GeneratorTile
    {
        public static GeneratorTile Empty = new GeneratorTile(' ') { Print = (generator, tile, group) => tile.Replace(new WallCave()) };
        public static GeneratorTile Floor = new GeneratorTile('.') { Print = (generator, tile, group) => tile.Replace(new FloorCave()) };
        public static GeneratorTile FloorBrick = new GeneratorTile('.') { Print = (generator, tile, group) => tile.Replace(new FloorTiles()) };
        public static GeneratorTile Wall = new GeneratorTile('X') { Print = (generator, tile, group) => tile.Replace(new WallCave()) };
        public static GeneratorTile WallBrick = new GeneratorTile('#') { Print = (generator, tile, group) => tile.Replace(new WallBrick()) };
        public static GeneratorTile OreDilithium = new GeneratorTile('G') { Print = (generator, tile, group) => PrintOre(generator, tile, group, new WallOre(Material.Dilithium)) };
        public static GeneratorTile OreTiberium = new GeneratorTile('T') { Print = (generator, tile, group) => PrintOre(generator, tile, group, new WallOre(Material.Tiberium)) };
        public static GeneratorTile OreBasalt = new GeneratorTile('B') { Print = (generator, tile, group) => tile.Replace(new WallBasalt()) };
        public static GeneratorTile OreMeteorite = new GeneratorTile('M') { Print = (generator, tile, group) => tile.Replace(new WallMeteorite()) };
        public static GeneratorTile OreObsidiorite = new GeneratorTile('D') { Print = (generator, tile, group) => tile.Replace(new WallObsidiorite()) };
        public static GeneratorTile OreKarmesine = new GeneratorTile('K') { Print = (generator, tile, group) => PrintOre(generator, tile, group, new WallOre(Material.Karmesine)) };
        public static GeneratorTile OreOvium = new GeneratorTile('O') { Print = (generator, tile, group) => PrintOre(generator, tile, group, new WallOre(Material.Ovium)) };
        public static GeneratorTile OreJauxum = new GeneratorTile('J') { Print = (generator, tile, group) => PrintOre(generator, tile, group, new WallOre(Material.Jauxum)) };

        public char Character;
        public Action<MapGenerator, Tile, GeneratorGroup> Print;

        public GeneratorTile(char character)
        {
            Character = character;
        }

        static void PrintCaveWall(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            tile.Replace(new WallCave());
        }

        static void PrintOre(MapGenerator generator, Tile tile, GeneratorGroup group, Tile ore)
        {
            PrintCaveWall(generator, tile, group);
            tile.PlaceOn(ore);
        }

        static void NoPrint(MapGenerator generator, Tile tile, GeneratorGroup group)
        {
            //NOOP
        }
    }

    abstract class GeneratorGroup
    {
        HashSet<GeneratorGroup> ConnectedGroups = new HashSet<GeneratorGroup>();
        HashSet<GeneratorCell> Cells = new HashSet<GeneratorCell>();
        public TileColor CaveColor;
        public TileColor BrickColor;
        public Func<int,Color> GlowColor = (time) => Color.Black;
        public EnemySpawnDelegate Spawn = (world,tile) => Enumerable.Empty<Enemy>();

        public void Connect(GeneratorGroup other)
        {
            this.ConnectedGroups.Add(other);
            other.ConnectedGroups.Add(this);
        }

        public bool IsConnected(GeneratorGroup other)
        {
            return ConnectedGroups.Contains(other);
        }

        public abstract void PlaceConnection(MapGenerator generator, GeneratorCell cell);

        public abstract void PlaceRoom(MapGenerator generator, GeneratorCell cell);

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
            return new Rectangle(Math.Min(a.X,b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        public class Smelter : GeneratorGroup
        {
            public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
            {
                return Enumerable.Empty<Point>();
            }

            public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
            {
                //NOOP
            }

            public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
            {
                for (int i = 0; i < 4; i++)
                {
                    int offX = generator.Random.Next(-5, 6);
                    int offY = generator.Random.Next(-5, 6);
                    GeneratorCell center = cell.GetNeighbor(offX, offY);
                    center.Tile = GeneratorTile.Floor;
                    center.Group = this;
                    generator.SpreadCastle(center, 3 + generator.Random.Next(4), GeneratorTile.Floor);
                }
            }
        }

        public class Castle : GeneratorGroup
        {
            public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
            {
                var dijkstraMap = Util.Dijkstra(a, generator.Width, generator.Height, new Rectangle(0,0,generator.Width,generator.Height), 50, generator.GetWeightStraight, generator.GetAllNeighbors);
                return dijkstraMap.FindPath(b);
            }

            public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCastle(cell, 1, GeneratorTile.Floor);
            }

            public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCastle(cell, generator.Random.Next(3,7), GeneratorTile.Floor);
            }
        }

        public class Tower : GeneratorGroup
        {
            public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
            {
                var dijkstraMap = Util.Dijkstra(a, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, generator.GetWeightStraight, generator.GetAllNeighbors);
                return dijkstraMap.FindPath(b);
            }

            public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCastle(cell, 1, GeneratorTile.Floor);
            }

            public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCastle(cell, generator.Random.Next(3, 7), GeneratorTile.FloorBrick);
            }
        }

        public class Cave : GeneratorGroup
        {
            public override IEnumerable<Point> GetPath(MapGenerator generator, Point a, Point b)
            {
                var dijkstraMap = Util.Dijkstra(a, generator.Width, generator.Height, new Rectangle(0, 0, generator.Width, generator.Height), 50, generator.GetWeightStraight, generator.GetNeighbors);
                return dijkstraMap.FindPath(b);
            }

            public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCave(cell, 2);
            }

            public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCave(cell, generator.Random.Next(3,10));
            }
        }
    }

    class GeneratorCell
    {
        class SpreadData
        {
            public Func<Point, IEnumerable<Point>> GetNeighbors;
            public Action<GeneratorCell> SpreadAction;

            public SpreadData(Func<Point, IEnumerable<Point>> getNeighbors, Action<GeneratorCell> spreadAction)
            {
                GetNeighbors = getNeighbors;
                SpreadAction = spreadAction;
            }
        }

        MapGenerator Generator;
        Random Random => Generator.Random;
        GeneratorGroup CurrentGroup;

        public bool HasSpread => SpreadTiles.Any();
        public bool HasCollapse => CollapseTiles.Any();

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
        List<SpreadData> SpreadTiles = new List<SpreadData>();
        List<Action<GeneratorCell>> CollapseTiles = new List<Action<GeneratorCell>>();

        int _ExpansionDistance;
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
        }

        public GeneratorCell(MapGenerator generator, int x, int y, GeneratorTile tile)
        {
            Generator = generator;
            Tile = tile;
            X = x;
            Y = y;
        }

        public void AddSpread(Func<Point, IEnumerable<Point>> getNeighbors, Action<GeneratorCell> spreadAction)
        {
            Generator.AddSpread(this);
            SpreadTiles.Add(new SpreadData(getNeighbors, spreadAction));
        }

        public void AddCollapse(Action<GeneratorCell> collapseAction)
        {
            Generator.AddCollapse(this);
            CollapseTiles.Add(collapseAction);
        }

        public GeneratorCell GetNeighbor(int dx, int dy)
        {
            return Generator.Cells[X + dx, Y + dy];
        }

        public void Spread()
        {
            foreach(var spread in SpreadTiles)
            {
                var neighbors = spread.GetNeighbors(new Point(X, Y)).Where(Generator.InBounds).Select(Generator.GetCell);
                foreach (var neighbor in neighbors)
                    spread.SpreadAction(neighbor);
            }
            ExpansionGeneration++;
            SpreadTiles.Clear();
        }

        public void Collapse()
        {
            foreach(var collapse in CollapseTiles)
            {
                collapse(this);
            }
            CollapseTiles.Clear();
        }
    }

    class MapGenerator
    {
        public Random Random;
        public List<Point> Points = new List<Point>();
        public Point StartRoom;
        public GeneratorGroup StartRoomGroup;
        List<GeneratorGroup> Groups = new List<GeneratorGroup>();
        Queue<GeneratorCell> ToSpread = new Queue<GeneratorCell>();
        Queue<GeneratorCell> ToCollapse = new Queue<GeneratorCell>();
        public GeneratorCell[,] Cells;
        public int ExpansionGeneration;

        public int Width => Cells.GetLength(0);
        public int Height => Cells.GetLength(1);

        public MapGenerator(int width, int height, int seed)
        {
            Random = new Random(seed);
            Cells = new GeneratorCell[width, height];
            for(int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cells[x, y] = new GeneratorCell(this, x, y, GeneratorTile.Empty);
                    Cells[x, y].Weight = Random.NextDouble();
                }
            }
        }

        public void AddCollapse(GeneratorCell cell)
        {
            ToCollapse.Enqueue(cell);
        }

        public void AddSpread(GeneratorCell cell)
        {
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

        public void Print(Map map)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = Cells[x, y];
                    Tile mapTile = map.GetTile(x, y);
                    cell.Tile.Print(this, mapTile, cell.Group);
                    mapTile.Group = cell.Group;
                }
            }
        }

        public void Generate()
        {
            SetupPoints(250, 10);
            ConnectPoints();
            ConnectGroups();
            Expand();
            GenerateStartRoom();
            Expand();
            GenerateOres(250, 0, GeneratorTile.OreDilithium);
            GenerateOres(30, 3, GeneratorTile.OreBasalt);
            GenerateOres(50, 3, GeneratorTile.OreKarmesine);
            GenerateOres(50, 3, GeneratorTile.OreOvium);
            GenerateOres(50, 3, GeneratorTile.OreJauxum);
            GenerateOres(10, 6, GeneratorTile.OreMeteorite);
            GenerateOres(30, 6, GeneratorTile.OreObsidiorite);
            GenerateOres(20, 6, GeneratorTile.OreTiberium);
            Expand();
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

            foreach (var tile in groupTiles)
            {
                toVisit.Enqueue(new FloodPoint(tile,GetCell(tile).Group));
                visited.Add(tile);
            }

            while (toVisit.Any())
            {
                FloodPoint visit = toVisit.Dequeue();

                if (!InMap(visit.Position))
                    continue;

                GetCell(visit.Position).Group = visit.Group;

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

        private void Expand()
        {
            while (ToSpread.Count > 0)
            {
                Spread();
                Collapse();
            }
            ExpansionGeneration++;
        }

        public void Spread()
        {
            while(ToSpread.Count > 0)
            {
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

        Func<SceneGame, Enemy> SpawnSkeleton = (world) => new Skeleton(world);
        Func<SceneGame, Enemy> SpawnDeathKnight = (world) => new DeathKnight(world);
        
        Func<SceneGame, Enemy> SpawnBlastCannon = (world) => new BlastCannon(world);

        Func<SceneGame, Enemy> SpawnGoreVala = (world) => new GoreVala(world);
        Func<SceneGame, Enemy> SpawnVorrax = (world) => new Vorrax(world);
        Func<SceneGame, Enemy> SpawnCtholoid = (world) => new Ctholoid(world);

        Func<SceneGame, Enemy> SpawnBlueDragon = (world) => new BlueDragon(world);
        Func<SceneGame, Enemy> SpawnYellowDragon = (world) => new YellowDragon(world);

        Func<SceneGame, Enemy> SpawnPoisonBlob = (world) => new PoisonBlob(world);
        Func<SceneGame, Enemy> SpawnAcidBlob = (world) => new AcidBlob(world);

        private IEnumerable<Enemy> SpawnEnemySet(SceneGame world, Tile tile, IList<Func<SceneGame, Enemy>> spawns)
        {
            var enemy = spawns.Pick(Random)(world);
            enemy.MoveTo(tile,0);
            return new[] { enemy };
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
            StartRoomGroup = new GeneratorGroup.Smelter()
            {
                CaveColor = new TileColor(new Color(64, 64, 64), new Color(160, 160, 160)),
                BrickColor = new TileColor(new Color(64, 64, 64), new Color(160, 160, 160))
            };
            /*Groups.Add(new GeneratorGroup.Cave() //Fire Cave
            {
                CaveColor = new TileColor(new Color(128, 96, 16), new Color(255, 64, 16)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnSkeleton })
            });
            Groups.Add(new GeneratorGroup.Cave() //Adamant Cave
            {
                CaveColor = new TileColor(new Color(128, 160, 160), new Color(32, 64, 32)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnSkeleton, SpawnPoisonBlob })
            });
            Groups.Add(new GeneratorGroup.Cave() //Acid Cave
            {
                CaveColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                GlowColor = (time) => Color.Lerp(Color.Black, Color.GreenYellow, 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnAcidBlob, SpawnAcidBlob, SpawnAcidBlob, SpawnCtholoid, SpawnYellowDragon })
            });
            Groups.Add(new GeneratorGroup.Cave() //Sea of Dirac
            {
                CaveColor = new TileColor(new Color(88, 156, 175), new Color(111, 244, 194)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnPoisonBlob, SpawnPoisonBlob, SpawnGoreVala, SpawnGoreVala, SpawnGoreVala, SpawnBlueDragon, SpawnCtholoid })
            });
            Groups.Add(new GeneratorGroup.Cave() //Magma Mine
            {
                CaveColor = new TileColor(new Color(247, 211, 70), new Color(160, 35, 35)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnBlastCannon, SpawnBlastCannon, SpawnBlastCannon, SpawnAcidBlob, SpawnAcidBlob, SpawnSkeleton })
            });*/
            Groups.Add(new GeneratorGroup.Castle() //Dungeon
            {
                CaveColor = new TileColor(new Color(128, 128, 128), new Color(160, 160, 160)),
                BrickColor = new TileColor(new Color(32, 64, 32), new Color(128, 160, 160)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnSkeleton, SpawnSkeleton, SpawnSkeleton, SpawnVorrax, SpawnVorrax, SpawnDeathKnight })
            });
            Groups.Add(new GeneratorGroup.Tower() //Ivory Tower
            {
                CaveColor = new TileColor(new Color(108, 106, 79), new Color(188, 173, 139)),
                BrickColor = new TileColor(new Color(197, 182, 137), new Color(243, 241, 233)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] { SpawnSkeleton, SpawnSkeleton, SpawnDeathKnight, SpawnBlueDragon, SpawnBlueDragon })
            });
            Groups.Add(new GeneratorGroup.Castle() //Dark Castle
            {
                CaveColor = new TileColor(new Color(29, 50, 56), new Color(131, 138, 167)),
                BrickColor = new TileColor(new Color(29, 50, 56), new Color(53, 124, 151)),
                GlowColor = (time) => Color.Lerp(new Color(62, 79, 2), new Color(227, 253, 138), 0.5f + 0.5f * (float)Math.Sin(time / 60f)),
                Spawn = (world, tile) => SpawnEnemySet(world, tile, new[] {  SpawnDeathKnight, SpawnBlastCannon, SpawnCtholoid })
            });
            var i = 0;
            IEnumerable<Point> shuffled = Points.Shuffle(Random);
            var toAssign = shuffled.Take(Groups.Count);
            foreach(var cell in toAssign.Select(GetCell))
            {
                cell.Group = Groups[i];
                i++;
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
            }
        }

        public void ConnectGroups()
        {
            var groupIds = new Dictionary<GeneratorGroup, int>();
            var id = 0;
            foreach(var group in Groups)
            {
                groupIds.Add(group, id++);
            }
            
            var pairs = Points.SelectMany(x => Points.Where(y => IsUngrouped(x,y)).Select(y => Tuple.Create(x, y)));
            var unconnectedPairs = pairs.Where(pair => groupIds[GetCell(pair.Item1).Group] != groupIds[GetCell(pair.Item2).Group]);

            var test = pairs.All(pair => IsUngrouped(pair.Item1, pair.Item2));

            while (unconnectedPairs.Any())
            {
                var pick = unconnectedPairs.WithMin(pair => GetDistance(pair.Item1,pair.Item2));
                var cellA = GetCell(pick.Item1);
                var cellB = GetCell(pick.Item2);
                foreach(var key in groupIds.Keys.ToList())
                {
                    var groupId = groupIds[key];
                    if (groupId == groupIds[cellA.Group] || groupId == groupIds[cellB.Group])
                    {
                        groupIds[key] = groupIds[cellB.Group];
                    }
                }
                groupIds[cellA.Group] = groupIds[cellB.Group];
                Connect(pick.Item1, pick.Item2);
            }
        }

        public void GenerateOres(int times, int size, GeneratorTile ore)
        {
            var validTiles = AllCells().Select(GetCell).Where(cell => cell.Tile == GeneratorTile.Empty).ToList();
            
            for(int i = 0; i < times; i++)
            {
                var cell = validTiles.Pick(Random);
                if (cell.Tile != GeneratorTile.Empty)
                    continue;
                cell.Tile = ore;
                SpreadOre(cell, size, ore);
            }
        }

        public void GenerateStartRoom()
        {
            var validTiles = AllCells().Select(GetCell).Where(cell => cell.Tile == GeneratorTile.Empty).Where(cell => cell.X >= 10 && cell.Y >= 10 && cell.X < Width-10 && cell.Y < Height-10).ToList();

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
            var group = baseTile.Group;
            var path = group.GetPath(this, a, b);

            foreach(var cell in path.Select(GetCell))
            {
                cell.Tile = GeneratorTile.Floor;
                cell.Group = group;
                cell.Group.PlaceConnection(this, cell);
            }
        }

        public GeneratorCell GetCell(Point point)
        {
            return Cells[point.X,point.Y];
        }

        public bool InBounds(Point point)
        {
            return point.X > 0 && point.Y > 0 && point.X < Width - 1 && point.Y < Height - 1;
        }

        private bool InMap(Point point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X <= Width - 1 && point.Y <= Height - 1;
        }

        public void SpreadCastle(GeneratorCell cell, int n, GeneratorTile floor)
        {
            if (n < 0)
                return;
            cell.AddSpread(GetAllNeighbors, x => SpreadCastleInternal(x, n, cell.Group, floor));
            /*if (cell.Tile == GeneratorTile.Empty)
            {
                if (cell.HasCollapse)
                    cell.AddCollapse(x => x.Tile = GeneratorTile.Wall);
                else
                {
                    cell.AddCollapse(x => x.Tile = GeneratorTile.Floor);
                    if (n > 1)
                    {
                        cell.AddCollapse(x => x.AddSpread(GetAllNeighbors, y => SpreadCastle(y, n - 1)));
                    }
                }
            }*/
        }

        private void SpreadCastleInternal(GeneratorCell cell, int n, GeneratorGroup group, GeneratorTile floor)
        {
            if (cell.Tile != GeneratorTile.Empty)
                return;
            cell.AddCollapse(x => {
                if (n == 0)
                {
                    x.Tile = GeneratorTile.WallBrick;
                    x.Group = group;
                }
                else if (x.Group == group || x.Group == null)
                {
                    x.Tile = floor;
                    x.Group = group;
                    if(!x.HasSpread)
                        SpreadCastle(x, n - 1, floor);
                }
            });
            
        }

        public void SpreadCave(GeneratorCell cell, int n)
        {
            if (n <= 0)
                return;
            cell.AddSpread(x => GetRandomNeighbors(x, 0.6), x => SpreadCaveInternal(x, n, cell.Group));
        }

        private void SpreadCaveInternal(GeneratorCell cell, int n, GeneratorGroup group)
        {
            if (cell.Tile != GeneratorTile.Empty)
                return;
            cell.AddCollapse(x => {
                if (n == 0)
                {
                    x.Tile = GeneratorTile.Wall;
                    x.Group = group;
                }
                else if (x.Group == group || x.Group == null)
                {
                    x.Tile = GeneratorTile.Floor;
                    x.Group = group;
                    if (!x.HasSpread)
                        SpreadCave(x, n - 1);
                }
            });
        }

        public void SpreadOre(GeneratorCell cell, int n, GeneratorTile ore)
        {
            if (n <= 0)
                return;
            cell.AddSpread(x => GetRandomNeighbors(x, 0.6), x => SpreadOreInternal(x, n, ore, cell.Group));
        }

        private void SpreadOreInternal(GeneratorCell cell, int n, GeneratorTile ore, GeneratorGroup group)
        {
            if (cell.Tile != GeneratorTile.Empty)
                return;
            cell.AddCollapse(x => {
                if (x.Group == group || x.Group == null)
                {
                    x.Tile = ore;
                    x.Group = group;
                    if (!x.HasSpread)
                        SpreadOre(x, n - 1, ore);
                }
            });
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
                if(Random.NextDouble() < chance)
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
