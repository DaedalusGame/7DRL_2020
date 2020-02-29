using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeEngine
{
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
            IEnumerable<T> rValue = Enumerable.Empty<T>();
            Content.TryGetValue(typeof(T), out rValue);
            return rValue.OfType<TGet>();
        }
    }

    static class Util
    {
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
            List<T> shuffled = new List<T>();
            Random random = new Random();
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
    }
}
