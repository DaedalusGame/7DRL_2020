using FibonacciHeap;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    static class Util
    {
        #region Dijkstra
        public static DijkstraTile[,] Dijkstra(Point start, int width, int height, Rectangle activeArea, double maxDist, Func<Point, Point, double> length, Func<Point, IEnumerable<Point>> neighbors)
        {
            return Dijkstra(new[] { start }, width, height, activeArea, maxDist, length, neighbors);
        }

        public static DijkstraTile[,] Dijkstra(IEnumerable<Point> start, int width, int height, Rectangle activeArea, double maxDist, Func<Point, Point, double> length, Func<Point, IEnumerable<Point>> neighbors)
        {
            var dijkstraMap = new DijkstraTile[width, height];
            var nodeMap = new FibonacciHeapNode<DijkstraTile, double>[width, height];
            var heap = new FibonacciHeap<DijkstraTile, double>(0);
            if (activeArea.X < 0)
                activeArea.X = 0;
            if (activeArea.Y < 0)
                activeArea.Y = 0;
            if (activeArea.Width > width-1)
                activeArea.Width = width-1;
            if (activeArea.Height > height-1)
                activeArea.Height = height-1;

            for (int x = activeArea.X; x < activeArea.Width; x++)
            {
                for (int y = activeArea.Y; y < activeArea.Height; y++)
                {
                    Point tile = new Point(x, y);
                    bool isStart = start.Contains(tile);
                    DijkstraTile dTile = new DijkstraTile(tile, isStart ? 0 : double.PositiveInfinity, isStart ? 0 : double.PositiveInfinity);
                    var node = new FibonacciHeapNode<DijkstraTile, double>(dTile, dTile.Distance);
                    dijkstraMap[x, y] = dTile;
                    nodeMap[x, y] = node;
                    heap.Insert(node);
                }
            }

            while (!heap.IsEmpty())
            {
                var node = heap.RemoveMin();
                var dTile = node.Data;

                if (dTile.Distance >= maxDist)
                    break;

                foreach (var neighbor in neighbors(dTile.Tile))
                {
                    if (!activeArea.Contains(neighbor.X,neighbor.Y)/*neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height*/)
                        continue;
                    var nodeNeighbor = nodeMap[neighbor.X, neighbor.Y];
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

        public static IEnumerable<Point> FindEnds(this DijkstraTile[,] dijkstra, Func<Point, bool> predicate)
        {
            for (int x = 0; x < dijkstra.GetLength(0); x++)
            {
                for (int y = 0; y < dijkstra.GetLength(1); y++)
                {
                    var dTile = dijkstra[x, y];
                    if (!double.IsInfinity(dTile.Distance) && predicate(dTile.Tile))
                        yield return dTile.Tile;
                }
            }
        }

        private static IEnumerable<Point> FindPathInternal(DijkstraTile[,] dijkstra, Point end)
        {
            DijkstraTile dTile = dijkstra[end.X, end.Y];

            while (dTile.Previous != null)
            {
                yield return dTile.Tile;
                dTile = dTile.Previous;
            }
        }

        public static Point FindStart(this DijkstraTile[,] dijkstra, Point end)
        {
            DijkstraTile dTile = dijkstra[end.X, end.Y];

            while (dTile.Previous != null)
            {
                dTile = dTile.Previous;
            }

            return dTile.Tile;
        }

        public static IEnumerable<Point> FindPath(this DijkstraTile[,] dijkstra, Point end)
        {
            return FindPathInternal(dijkstra, end).Reverse();
        }

        public static double GetMove(this DijkstraTile[,] dijkstra, Tile end)
        {
            return dijkstra[end.X, end.Y].MoveDistance;
        }

        public static double GetCost(this DijkstraTile[,] dijkstra, Tile end)
        {
            return dijkstra[end.X, end.Y].Distance;
        }

        public static bool Reachable(this DijkstraTile[,] dijkstra, Tile end)
        {
            DijkstraTile dTile = dijkstra[end.X, end.Y];
            return dTile.Previous != null;
        }
        #endregion

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

        public static void IncrementTurn(this ITurnTaker turnTaker)
        {
            turnTaker.TurnBuildup += turnTaker.TurnSpeed;
        }

        public static void ReduceTurn(this ITurnTaker turnTaker, double turns)
        {
            turnTaker.TurnBuildup -= turns;
        }

        public static void ResetTurn(this ITurnTaker turnTaker)
        {
            turnTaker.TurnBuildup %= 1;
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

        public static IEnumerable<T> GetAndClean<T>(this List<T> enumerable, Predicate<T> shouldClean)
        {
            enumerable.RemoveAll(shouldClean);
            return enumerable;
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

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }

        public static void DrawPass(this ILookup<DrawPass, IGameObject> drawPasses, SceneGame scene, DrawPass pass)
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
            PartType partType = parts[index];
            return SpriteLoader.Instance.AddSprite(partType.SpritePrefix+material.Parts[partType].Sprite);
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

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> toShuffle)
        {
            return Shuffle(toShuffle, new Random());
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
    }
}
