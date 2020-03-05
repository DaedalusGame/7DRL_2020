using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class GeneratorTile
    {
        public static GeneratorTile Empty = new GeneratorTile(' ');
        public static GeneratorTile Floor = new GeneratorTile('.');
        public static GeneratorTile Wall = new GeneratorTile('X');

        public char Character;

        public GeneratorTile(char character)
        {
            Character = character;
        }
    }

    abstract class GeneratorGroup
    {
        HashSet<GeneratorCell> Cells = new HashSet<GeneratorCell>();

        public abstract void PlaceConnection(MapGenerator generator, GeneratorCell cell);

        public abstract void PlaceRoom(MapGenerator generator, GeneratorCell cell);

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
        
        public class Castle : GeneratorGroup
        {
            public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCastle(cell, 1);
            }

            public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCastle(cell, 3+generator.Random.Next(4));
            }
        }

        public class Cave : GeneratorGroup
        {
            public override void PlaceConnection(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCave(cell, 2);
            }

            public override void PlaceRoom(MapGenerator generator, GeneratorCell cell)
            {
                generator.SpreadCave(cell, 3 + generator.Random.Next(6));
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

        public GeneratorCell GetNeighbor(int x, int y)
        {
            return Generator.Cells[x, y];
        }

        public void Spread()
        {
            foreach(var spread in SpreadTiles)
            {
                var neighbors = spread.GetNeighbors(new Point(X, Y)).Where(Generator.InBounds).Select(p => GetNeighbor(p.X, p.Y));
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
        List<Point> Points = new List<Point>();
        Queue<GeneratorCell> ToSpread = new Queue<GeneratorCell>();
        Queue<GeneratorCell> ToCollapse = new Queue<GeneratorCell>();
        public GeneratorCell[,] Cells;
        public int ExpansionGeneration;

        int Width => Cells.GetLength(0);
        int Height => Cells.GetLength(1);

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

        public void Generate()
        {
            SetupPoints(250, 5);
            ConnectPoints();
            Expand();
            string map = "";
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    map += Cells[x, y].Tile.Character;
                }
                map += '\n';
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

        public void SetupPoints(int count, int deviation)
        {
            while(Points.Count < count)
            {
                int x = Random.Next(deviation, Width - deviation);
                int y = Random.Next(deviation, Height - deviation);
                if(!Points.Contains(new Point(x,y)))
                    Points.Add(new Point(x,y));
            }
            GeneratorGroup[] biomes = new GeneratorGroup[]
            {
                new GeneratorGroup.Cave(),
                new GeneratorGroup.Cave(),
                new GeneratorGroup.Cave(),
                new GeneratorGroup.Cave(),
                new GeneratorGroup.Cave(),
                new GeneratorGroup.Castle(),
                new GeneratorGroup.Castle(),
                new GeneratorGroup.Castle(),
            };
            var i = 0;
            var toAssign = Points.Shuffle(Random).Take(biomes.Length);
            foreach(var cell in toAssign.Select(GetCell))
            {
                cell.Group = biomes[i];
                i++;
            }
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
            var groups = Points.GroupBy(point => Cells[point.X, point.Y].Group);


        }

        public void Connect(Point a, Point b)
        {
            var baseTile = Cells[a.X, a.Y];
            var group = baseTile.Group;
            var dijkstraMap = Util.Dijkstra(a, Width, Height, new Rectangle(0,0,Width,Height), 50, GetWeightStraight, GetAllNeighbors);
            var path = dijkstraMap.FindPath(b);

            foreach(var cell in path.Select(GetCell))
            {
                cell.Tile = GeneratorTile.Floor;
                cell.Group = group;
                cell.Group.PlaceConnection(this, cell);
            }
        }

        private GeneratorCell GetCell(Point point)
        {
            return Cells[point.X,point.Y];
        }

        public bool InBounds(Point point)
        {
            return point.X > 0 && point.Y > 0 && point.X < Width - 1 && point.Y < Height - 1;
        }

        public void SpreadCastle(GeneratorCell cell, int n)
        {
            if (n < 0)
                return;
            cell.AddSpread(GetAllNeighbors, x => SpreadCastleInternal(x, n, cell.Group));
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

        private void SpreadCastleInternal(GeneratorCell cell, int n, GeneratorGroup group)
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
                    if(!x.HasSpread)
                        SpreadCastle(x, n - 1);
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

        private double GetWeightStraight(Point start, Point end)
        {
            return 1;
        }

        private double GetWeightWavy(Point start, Point end)
        {
            return Cells[end.X, end.Y].Weight;
        }

        private IEnumerable<Point> GetNeighbors(Point point)
        {
            yield return new Point(point.X, point.Y - 1);
            yield return new Point(point.X, point.Y + 1);
            yield return new Point(point.X - 1, point.Y);
            yield return new Point(point.X + 1, point.Y);
        }

        private IEnumerable<Point> GetRandomNeighbors(Point point, double chance)
        {
            var neighbors = GetNeighbors(point).Shuffle();
            yield return neighbors.First();
            foreach (var neighbor in neighbors.Skip(1))
                if(Random.NextDouble() < chance)
                    yield return neighbor;
        }

        private IEnumerable<Point> GetAllNeighbors(Point point)
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
