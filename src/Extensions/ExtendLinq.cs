using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Utility;

namespace StockCutter.Extensions
{
    public static class ExtendLinq
    {
        public static IEnumerable<T> Mode<T>(this IEnumerable<T> source)
        {
            return source.GroupBy(el => el)
                .OrderByDescending(g => g.Count())
                .Select(g => g.First());
        }

        public static IEnumerable<TResult> SelectTwo<TSource, TResult>(this IEnumerable<TSource> source,
                                                                       Func<TSource, TSource, TResult> selector)
        {
            return Enumerable.Zip(source, source.Skip(1), selector);
        }

        public static T MinByValue<T, K>(this IEnumerable<T> source, Func<T, K> selector)
        {
            var comparer = Comparer<K>.Default;

            var enumerator = source.GetEnumerator();
            enumerator.MoveNext();

            var min = enumerator.Current;
            var minV = selector(min);

            while (enumerator.MoveNext())
            {
                var s = enumerator.Current;
                var v = selector(s);
                if (comparer.Compare(v, minV) < 0)
                {
                    min = s;
                    minV = v;
                }
            }
            enumerator.Dispose();
            return min;
        }

        public static T MaxByValue<T, K>(this IEnumerable<T> source, Func<T, K> selector)
        {
            var comparer = Comparer<K>.Default;

            var enumerator = source.GetEnumerator();
            enumerator.MoveNext();

            var max = enumerator.Current;
            var maxV = selector(max);

            while (enumerator.MoveNext())
            {
                var s = enumerator.Current;
                var v = selector(s);
                if (comparer.Compare(v, maxV) > 0)
                {
                    max = s;
                    maxV = v;
                }
            }
            enumerator.Dispose();
            return max;
        }
    }
}
