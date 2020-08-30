using FibonacciHeap;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
    class DijkstraTile
    {
        public Point Tile;
        public double Distance;
        public double MoveDistance;
        public DijkstraTile Previous;

        public DijkstraTile(Point tile, double dist, double moveDist)
        {
            Tile = tile;
            Distance = dist;
            MoveDistance = moveDist;
        }

        public override string ToString()
        {
            return $"{Tile.ToString()} ({Distance})";
        }
    }

    interface IDijkstraMap
    {
        int Width
        {
            get;
        }
        int Height
        {
            get;
        }

        DijkstraTile this[int x, int y]
        {
            get;
        }

        FibonacciHeapNode<DijkstraTile, double> GetNode(int x, int y);
    }

    class DijkstraMap : IDijkstraMap
    {
        FibonacciHeap<DijkstraTile, double> Heap;
        Dictionary<Point, DijkstraTile> Tiles = new Dictionary<Point, DijkstraTile>();
        Dictionary<Point, FibonacciHeapNode<DijkstraTile, double>> NodeMap = new Dictionary<Point, FibonacciHeapNode<DijkstraTile, double>>();

        public int Width
        {
            get;
            set;
        }
        public int Height
        {
            get;
            set;
        }

        public DijkstraMap(int width, int height, FibonacciHeap<DijkstraTile, double> heap, IEnumerable<Point> starts)
        {
            Heap = heap;
            Width = width;
            Height = height;
            foreach (var start in starts)
            {
                GenerateNode(start, 0, 0);
            }
        }

        public DijkstraTile this[int x, int y] {
            get
            {
                DijkstraTile tile;
                bool hasValue = Tiles.TryGetValue(new Point(x,y), out tile);
                if(hasValue)
                    return tile;
                else
                    return GenerateTile(new Point(x, y), double.PositiveInfinity, double.PositiveInfinity);
            }
        }

        public FibonacciHeapNode<DijkstraTile, double> GetNode(int x, int y)
        {
            FibonacciHeapNode<DijkstraTile, double> node;
            bool hasValue = NodeMap.TryGetValue(new Point(x, y), out node);
            if (hasValue)
                return node;
            else
                return GenerateNode(new Point(x, y), double.PositiveInfinity, double.PositiveInfinity);
        }

        private void GenerateNode(Point tile, double distance, double moveDist, out DijkstraTile outTile, out FibonacciHeapNode<DijkstraTile, double> outNode)
        {
            outTile = new DijkstraTile(tile, distance, moveDist);
            outNode = new FibonacciHeapNode<DijkstraTile, double>(outTile, outTile.Distance);
            Tiles.Add(tile, outTile);
            NodeMap.Add(tile, outNode);
            Heap.Insert(outNode);
        }

        private DijkstraTile GenerateTile(Point tile, double distance, double moveDist)
        {
            DijkstraTile outTile;
            FibonacciHeapNode<DijkstraTile, double> outNode;
            GenerateNode(tile, distance, moveDist, out outTile, out outNode);
            return outTile;
        }

        private FibonacciHeapNode<DijkstraTile, double> GenerateNode(Point tile, double distance, double moveDist)
        {
            DijkstraTile outTile;
            FibonacciHeapNode<DijkstraTile, double> outNode;
            GenerateNode(tile, distance, moveDist, out outTile, out outNode);
            return outNode;
        }
    }

    class DijkstraArray : IDijkstraMap
    {
        FibonacciHeap<DijkstraTile, double> Heap;
        DijkstraTile[,] Array;
        FibonacciHeapNode<DijkstraTile, double>[,] NodeMap;

        public DijkstraTile this[int x, int y] => Array[x,y];

        public int Width => Array.GetLength(0);
        public int Height => Array.GetLength(1);

        public DijkstraArray(int width, int height, FibonacciHeap<DijkstraTile, double> heap, IEnumerable<Point> starts, Rectangle activeArea)
        {
            Heap = heap;
            Array = new DijkstraTile[width, height];
            NodeMap = new FibonacciHeapNode<DijkstraTile, double>[width, height];

            for (int x = activeArea.X; x < activeArea.Width; x++)
            {
                for (int y = activeArea.Y; y < activeArea.Height; y++)
                {
                    Point tile = new Point(x, y);
                    bool isStart = starts.Contains(tile);
                    DijkstraTile dTile = new DijkstraTile(tile, isStart ? 0 : double.PositiveInfinity, isStart ? 0 : double.PositiveInfinity);
                    var node = new FibonacciHeapNode<DijkstraTile, double>(dTile, dTile.Distance);
                    Array[x, y] = dTile;
                    NodeMap[x, y] = node;
                    Heap.Insert(node);
                }
            }
        }

        public FibonacciHeapNode<DijkstraTile, double> GetNode(int x, int y)
        {
            return NodeMap[x, y];
        }
    }

    class TypeLookup<T>
    {
        Dictionary<Type, IEnumerable<T>> Content = new Dictionary<Type, IEnumerable<T>>();

        private TypeLookup()
        {

        }

        public static TypeLookup<T> Create(IEnumerable<T> enumerable)
        {
            var lookup = new TypeLookup<T>();
            var groups = enumerable.MultiGroupBy(x => Util.GetBaseTypes(x));
            foreach (var group in groups)
                lookup.Content.Add(group.Key, group);
            return lookup;
        }

        public IEnumerable<TGet> Get<TGet>() where TGet : T
        {
            IEnumerable<T> rValue;
            bool success = Content.TryGetValue(typeof(T), out rValue);
            return success ? rValue.OfType<TGet>() : Enumerable.Empty<TGet>();
        }
    }

    class MultiDict<TKey, TValue>
    {
        private Dictionary<TKey, List<TValue>> Data = new Dictionary<TKey, List<TValue>>();

        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                return Data[key];
            }
        }

        public void Add(TKey key, TValue value)
        {
            List<TValue> list;
            if (Data.TryGetValue(key, out list))
                list.Add(value);
            else
                Data.Add(key, new List<TValue>() { value });
        }

        public IEnumerable<TValue> GetOrEmpty(TKey key)
        {
            List<TValue> list;
            if (Data.TryGetValue(key, out list))
                return list;
            else
                return Enumerable.Empty<TValue>();
        }
    }

    static class Util
    {
        #region Dijkstra
        public static IDijkstraMap Dijkstra(Point start, Point end, int width, int height, Rectangle activeArea, double maxDist, Func<Point, Point, double> length, Func<Point, IEnumerable<Point>> neighbors)
        {
            return Dijkstra(new[] { start }, new[] { end }, width, height, activeArea, maxDist, length, neighbors);
        }

        public static IDijkstraMap Dijkstra(IEnumerable<Point> start, IEnumerable<Point> end, int width, int height, Rectangle activeArea, double maxDist, Func<Point, Point, double> length, Func<Point, IEnumerable<Point>> neighbors)
        {
            HashSet<Point> ends = new HashSet<Point>(end);
            FibonacciHeap<DijkstraTile, double> heap = new FibonacciHeap<DijkstraTile, double>(0);

            if (activeArea.X < 0)
                activeArea.X = 0;
            if (activeArea.Y < 0)
                activeArea.Y = 0;
            if (activeArea.Width > width-1)
                activeArea.Width = width-1;
            if (activeArea.Height > height-1)
                activeArea.Height = height-1;

            IDijkstraMap dijkstraMap = new DijkstraMap(width, height, heap, start);

            while (!heap.IsEmpty() && end.Any() && ends.Count > 0)
            {
                var node = heap.RemoveMin();
                var dTile = node.Data;

                if (dTile.Distance >= maxDist)
                    break;

                if (ends.Contains(dTile.Tile))
                    ends.Remove(dTile.Tile);

                foreach (var neighbor in neighbors(dTile.Tile))
                {
                    if (!activeArea.Contains(neighbor.X,neighbor.Y)/*neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height*/)
                        continue;
                    var nodeNeighbor = dijkstraMap.GetNode(neighbor.X, neighbor.Y);
                    var dNeighbor = nodeNeighbor.Data;
                    double newDist = dTile.Distance + length(dTile.Tile, dNeighbor.Tile);

                    if (newDist < dNeighbor.Distance)
                    {
                        dNeighbor.Distance = newDist;
                        dNeighbor.Previous = dTile;
                        dNeighbor.MoveDistance = dTile.MoveDistance + 1;
                        heap.DecreaseKey(nodeNeighbor, dNeighbor.Distance);
                    }
                }
            }

            return dijkstraMap;
        }

        public static IEnumerable<Point> FindEnds(this IDijkstraMap dijkstra, Func<Point, bool> predicate)
        {
            for (int x = 0; x < dijkstra.Width; x++)
            {
                for (int y = 0; y < dijkstra.Height; y++)
                {
                    var dTile = dijkstra[x, y];
                    if (!double.IsInfinity(dTile.Distance) && predicate(dTile.Tile))
                        yield return dTile.Tile;
                }
            }
        }

        private static IEnumerable<Point> FindPathInternal(IDijkstraMap dijkstra, Point end)
        {
            DijkstraTile dTile = dijkstra[end.X, end.Y];

            while (dTile.Previous != null)
            {
                yield return dTile.Tile;
                dTile = dTile.Previous;
            }
        }

        public static Point FindStart(this IDijkstraMap dijkstra, Point end)
        {
            DijkstraTile dTile = dijkstra[end.X, end.Y];

            while (dTile.Previous != null)
            {
                dTile = dTile.Previous;
            }

            return dTile.Tile;
        }

        public static IEnumerable<Point> FindPath(this IDijkstraMap dijkstra, Point end)
        {
            return FindPathInternal(dijkstra, end).Reverse();
        }

        public static double GetMove(this IDijkstraMap dijkstra, Tile end)
        {
            return dijkstra[end.X, end.Y].MoveDistance;
        }

        public static double GetCost(this IDijkstraMap dijkstra, Tile end)
        {
            return dijkstra[end.X, end.Y].Distance;
        }

        public static bool Reachable(this IDijkstraMap dijkstra, Tile end)
        {
            DijkstraTile dTile = dijkstra[end.X, end.Y];
            return dTile.Previous != null;
        }
        #endregion

        public static IEnumerable<Point> DrunkardWalk(Point a, Point b, Func<Point, IEnumerable<Point>> neighbors, Random random)
        {
            Point current = a;
            Point last = a;
            //Rectangle acceptableArea = new Rectangle(a.X, a.Y, b.X - a.X, b.Y - a.Y);
            //acceptableArea.Inflate(3, 3);

            while(current != b)
            {
                var validSquares = neighbors(current).Where(x => IsStepTowards(b, current, x)).OrderBy(x => random.Next());
                last = current;
                current = validSquares.First();
                yield return current;
            }
        }

        public static bool IsStepTowards(Point target, Point last, Point current)
        {
            return GetDistanceSquared(current, target) <= GetDistanceSquared(last, target);
        }

        public static int GetDistanceSquared(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;

            return dx * dx + dy * dy;
        }

        public static int PositiveMod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        public static int FloorDiv(int x, int m)
        {
            if (((x < 0) ^ (m < 0)) && (x % m != 0))
            {
                return (x / m - 1);
            }
            else
            {
                return (x / m);
            }
        }

        public static T WithMin<T, V>(this IEnumerable<T> enumerable, Func<T, V> selector) where V : IComparable
        {
            return enumerable.Aggregate((i1, i2) => selector(i1).CompareTo(selector(i2)) < 0 ? i1 : i2);
        }

        public static T WithMax<T, V>(this IEnumerable<T> enumerable, Func<T, V> selector) where V : IComparable
        {
            return enumerable.Aggregate((i1, i2) => selector(i1).CompareTo(selector(i2)) > 0 ? i1 : i2);
        }

        public static bool Empty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }

        public static TValue GetOrDefault<TKey,TValue>(this IDictionary<TKey,TValue> dict, TKey key, TValue defaultValue)
        {
            TValue value;
            if (dict.TryGetValue(key, out value))
                return value;
            else
                return defaultValue;
        }

        public static IEnumerable<T> GetAndClean<T>(this List<T> enumerable, Predicate<T> shouldClean)
        {
            enumerable.RemoveAll(shouldClean);
            return enumerable;
        }

        public static void AddRange<T>(this HashSet<T> set,IEnumerable<T> enumerable)
        {
            foreach(var item in enumerable)
                set.Add(item);
        }

        public static void RemoveRange<T>(this HashSet<T> set, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
                set.Remove(item);
        }

        private static T PickInternal<T>(IList<T> enumerable, Random random, bool remove)
        {
            int select = random.Next(enumerable.Count());
            T pick = enumerable[select];
            if (remove)
                enumerable.RemoveAt(select);
            return pick;
        }

        public static T Pick<T>(this IList<T> enumerable, Random random)
        {
            return PickInternal(enumerable, random, false);
        }

        public static T PickAndRemove<T>(this IList<T> enumerable, Random random)
        {
            return PickInternal(enumerable, random, true);
        }

        public static T PickAndRemoveBest<T>(this IList<T> enumerable, Func<T,IComparable> keySelector, Random random)
        {
            var pick = enumerable.Shuffle(random).OrderByDescending(keySelector).First();
            enumerable.Remove(pick);
            return pick;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        public static void DrawPass(this ILookup<DrawPass, IDrawable> drawPasses, SceneGame scene, DrawPass pass)
        {
            foreach(var obj in drawPasses[pass].OrderBy(x => x.DrawOrder))
            {
                obj.Draw(scene, pass);
            }
        }

        public static void Destroy(this IGameObject obj)
        {
            obj.Destroyed = true;
            obj.OnDestroy();
        }

        public static IList<int> ProportionalSplit(this IEnumerable<int> input, int n)
        {    
            int sum = input.Sum();
            if (sum <= 0)
                return input.Select(x => 0).ToList();
            var deltas = input.Select(x => (float)x * n / sum);
            int i = 0;
            int rest = n;
            List<int> results = new List<int>();
            foreach(var delta in deltas)
            {
                int toSubtract = (int)Math.Round(delta);
                if (rest >= toSubtract)
                {
                    rest -= toSubtract;
                    results.Add(toSubtract);
                }
                else
                {
                    results.Add(rest);
                }
                i++;
            }
            if (rest > 0)
                results[results.Count - 1] += rest;
            if (results.Sum() != n)
                throw new Exception();
            return results;
        }

        public static IList<double> ProportionalSplit(this IEnumerable<double> input, double n)
        {
            double sum = input.Sum();
            if (sum <= 0)
                return input.Select(x => 0.0).ToList();
            var deltas = input.Select(x => x * n / sum);
            return deltas.ToList();
        }

        public static void Operate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue val, Func<TValue, TValue, TValue> op)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = op(dictionary[key],val);
            else
                dictionary.Add(key, val);
        }

        public static string GetName(this PartType[] parts, int index)
        {
            return parts[index].Name;
        }

        public static SpriteReference GetSprite(this PartType[] parts, int index, Material material)
        {
            return parts.GetSprite(index, material, "content/");
        }

        public static SpriteReference GetSprite(this PartType[] parts, int index, Material material, string prefix)
        {
            PartType partType = parts[index];
            return SpriteLoader.Instance.AddSprite(prefix + partType.SpritePrefix + material.Parts[partType].Sprite);
        }

        public static Color RotateHue(this Color color, double amount)
        {
            double U = Math.Cos(amount * Math.PI * 2);
            double W = Math.Sin(amount * Math.PI * 2);

            double r = (.299 + .701 * U + .168 * W) * color.R
                        + (.587 - .587 * U + .330 * W) * color.G
                        + (.114 - .114 * U - .497 * W) * color.B;
            double g = (.299 - .299 * U - .328 * W) * color.R
                        + (.587 + .413 * U + .035 * W) * color.G
                        + (.114 - .114 * U + .292 * W) * color.B;
            double b = (.299 - .3 * U + 1.25 * W) * color.R
                        + (.587 - .588 * U - 1.05 * W) * color.G
                        + (.114 + .886 * U - .203 * W) * color.B;

            return new Color((int)r, (int)g, (int)b, color.A);
        }

        public static string EnglishJoin(string seperator, string finalSeperator, IEnumerable<string> values)
        {
            values = values.ToList();
            var first = values.Take(values.Count() - 1);
            var last = values.Last();
            if (!first.Any())
                return last;
            else
                return $"{String.Join(seperator, first)}{finalSeperator}{last}";
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> toShuffle, Random random)
        {
            List<T> shuffled = new List<T>();
            foreach (T value in toShuffle)
            {
                shuffled.Insert(random.Next(shuffled.Count + 1), value);
            }
            return shuffled;
        }

        public static TypeLookup<TSource> ToTypeLookup<TSource>(this IEnumerable<TSource> enumerable)
        {
            return TypeLookup<TSource>.Create(enumerable);
        }

        public static ILookup<TKey, TElement> ToMultiLookup<TSource, TKey, TElement>(this IEnumerable<TSource> enumerable, Func<TSource, IEnumerable<TKey>> keySelector, Func<TSource, TElement> valueSelector)
        {
            return enumerable.SelectMany(obj => keySelector(obj).Select(pass => Tuple.Create(valueSelector(obj), pass))).ToLookup(obj => obj.Item2, obj => obj.Item1);
        }

        public static ILookup<TKey, TElement> ToMultiLookup<TKey, TElement>(this IEnumerable<TElement> enumerable, Func<TElement, IEnumerable<TKey>> keySelector)
        {
            return enumerable.SelectMany(obj => keySelector(obj).Select(pass => Tuple.Create(obj, pass))).ToLookup(obj => obj.Item2, obj => obj.Item1);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> MultiGroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> enumerable, Func<TSource, IEnumerable<TKey>> keySelector, Func<TSource, TElement> valueSelector)
        {
            return enumerable.SelectMany(obj => keySelector(obj).Select(pass => Tuple.Create(valueSelector(obj), pass))).GroupBy(obj => obj.Item2, obj => obj.Item1);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> MultiGroupBy<TKey, TElement>(this IEnumerable<TElement> enumerable, Func<TElement, IEnumerable<TKey>> keySelector)
        {
            return enumerable.SelectMany(obj => keySelector(obj).Select(pass => Tuple.Create(obj, pass))).GroupBy(obj => obj.Item2, obj => obj.Item1);
        }

        public static IEnumerable<Type> GetBaseTypes<T>(T o)
        {
            Type type = o.GetType();
            yield return type;
            while (type != typeof(T))
            {
                type = type.BaseType;
                yield return type;
            }
        }

        public static IEnumerable<Type> GetBaseTypes<T>(Type type)
        {
            if (typeof(T).IsAssignableFrom(type))
            {
                yield return type;
                while (type != typeof(T))
                {
                    type = type.BaseType;
                    yield return type;
                }
            }
        }

        public static Vector2 ClampLength(this Vector2 vector, float minLength, float maxLength)
        {
            float length = vector.Length();
            Vector2 normalized = vector / length;
            length = MathHelper.Clamp(length, minLength, maxLength);
            return normalized * length;
        }

        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        public static Vector2 Mirror(this Vector2 vector, SpriteEffects mirror)
        {
            if (mirror.HasFlag(SpriteEffects.FlipHorizontally))
                vector.X *= -1;
            if (mirror.HasFlag(SpriteEffects.FlipVertically))
                vector.Y *= -1;

            return vector;
        }

        public static Point ToOffset(this Facing facing)
        {
            switch (facing)
            {
                case (Facing.North):
                    return new Point(0, -1);
                case (Facing.East):
                    return new Point(1, 0);
                case (Facing.South):
                    return new Point(0, 1);
                case (Facing.West):
                    return new Point(-1, 0);
                default:
                    return Point.Zero;
            }
        }

        internal static Color AddColor(Color a, Color b)
        {
            return new Color(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);
        }

        public static Facing TurnLeft(this Facing facing)
        {
            switch (facing)
            {
                case (Facing.North):
                    return Facing.West;
                case (Facing.East):
                    return Facing.North;
                case (Facing.South):
                    return Facing.East;
                case (Facing.West):
                    return Facing.South;
                default:
                    throw new Exception();
            }
        }

        public static Facing TurnRight(this Facing facing)
        {
            return facing.TurnLeft().Mirror();
        }

        public static Facing Mirror(this Facing facing)
        {
            switch (facing)
            {
                case (Facing.North):
                    return Facing.South;
                case (Facing.East):
                    return Facing.West;
                case (Facing.South):
                    return Facing.North;
                case (Facing.West):
                    return Facing.East;
                default:
                    throw new Exception();
            }
        }

        public static float GetAngleDistance(float a0, float a1)
        {
            var max = Math.PI * 2;
            var da = (a1 - a0) % max;
            return (float)(2 * da % max - da);
        }

        public static float AngleLerp(float a0, float a1, float t)
        {
            return a0 + GetAngleDistance(a0, a1) * t;
        }

        public static Vector2 AngleToVector(float angle)
        {
            return new Vector2((float)Math.Sin(angle), (float)-Math.Cos(angle));
        }

        public static float VectorToAngle(Vector2 vector)
        {
            return (float)Math.Atan2(vector.X, -vector.Y);
        }

        public static int GetSectionDelta(int aMin, int aMax, int bMin, int bMax)
        {
            if (aMax < bMin)
                return Math.Abs(aMax - bMin);
            if (bMax < aMin)
                return -Math.Abs(bMax - aMin);
            return 0;
        }

        public static int GetDeltaX(Rectangle a, Rectangle b)
        {
            return GetSectionDelta(a.Left, a.Right-1, b.Left, b.Right-1);
        }

        public static int GetDeltaY(Rectangle a, Rectangle b)
        {
            return GetSectionDelta(a.Top, a.Bottom-1, b.Top, b.Bottom-1);
        }

        public static Facing? GetFacing(int dx, int dy)
        {
            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? Facing.East : Facing.West;
            }
            else if (Math.Abs(dy) > Math.Abs(dx))
            {
                return dy > 0 ? Facing.South : Facing.North;
            }
            return null;
        }

        public static bool HasKey(this JToken json, string key)
        {
            return json[key] != null;
        }

        static Dictionary<int, int> BlobTileMap = new Dictionary<int, int>() //Mapper for the minimal tileset, index in memory -> index in image
        {
            {0, 0},
            {4, 1},
            {92, 2},
            {124, 3},
            {116, 4},
            {80, 5},
            //{0, 6},
            {16, 7},
            {20, 8},
            {87, 9},
            {223, 10},
            {241, 11},
            {21, 12},
            {64, 13},
            {29, 14},
            {117, 15},
            {85, 16},
            {71, 17},
            {221, 18},
            {125, 19},
            {112, 20},
            {31, 21},
            {253, 22},
            {113, 23},
            {28, 24},
            {127, 25},
            {247, 26},
            {209, 27},
            {23, 28},
            {199, 29},
            {213, 30},
            {95, 31},
            {255, 32},
            {245, 33},
            {81, 34},
            {5, 35},
            {84, 36},
            {93, 37},
            {119, 38},
            {215, 39},
            {193, 40},
            {17, 41},
            //{0, 42},
            {1, 43},
            {7, 44},
            {197, 45},
            {69, 46},
            {68, 47},
            {65, 48},
        };

        public static Connectivity Rotate(this Connectivity connectivity, int halfTurns)
        {
            return (Connectivity)(ShiftWrap((int)connectivity, PositiveMod(halfTurns, 8), 8) & 255);
        }

        private static int Shift(int a, int b)
        {
            if (b > 0)
                return a >> b;
            else
                return a << -b;
        }

        private static int ShiftWrap(int a, int b, int size)
        {
            int left = Shift(a, b);
            int right = Shift(a, -(size - b));
            return left | right;
        }

        public static Connectivity CullDiagonals(this Connectivity connectivity)
        {
            if (!connectivity.HasFlag(Connectivity.North))
                connectivity &= Connectivity.KillNorth;
            if (!connectivity.HasFlag(Connectivity.East))
                connectivity &= Connectivity.KillEast;
            if (!connectivity.HasFlag(Connectivity.South))
                connectivity &= Connectivity.KillSouth;
            if (!connectivity.HasFlag(Connectivity.West))
                connectivity &= Connectivity.KillWest;
            return connectivity;
        }

        public static int GetBlobTile(this Connectivity connectivity)
        {
            connectivity = connectivity.CullDiagonals();
            return BlobTileMap.ContainsKey((int)connectivity) ? BlobTileMap[(int)connectivity] : BlobTileMap[0];
        }
    }
}
