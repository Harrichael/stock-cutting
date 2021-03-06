using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Utility;

namespace StockCutter.Extensions
{
    public static class ExtendList
    {
        public static void Swap<T>(this IList<T> source, int index1, int index2)
        {
            T temp = source[index1];
            source[index1] = source[index2];
            source[index2] = temp;
        }

        public static IEnumerable<T> Choose<T>(this IList<T> source, int numChoices, Func<T, int> weight)
        {
            var totalWeight = source.Sum(i => weight(i));
            var choices = new List<T>();
            for (int i = 0; i < numChoices; i++)
            {
                var pick = CmnRandom.Random.Next(0, totalWeight - 1);
                foreach (var item in source)
                {
                    pick -= weight(item);
                    if (pick < 0)
                    {
                        choices.Add(item);
                        break;
                    }
                }
            }
            return choices;
        }

        public static T ChooseSingle<T>(this IList<T> source, Func<T, int> weight)
        {
            return source.Choose(1, weight).First();
        }

        public static IEnumerable<T> Choose<T>(this IList<T> source, int numChoices)
        {
            return source.Choose(numChoices, ((i) => 1));
        }

        public static T ChooseSingle<T>(this IList<T> source)
        {
            return source.Choose(1).First();
        }

        public static IEnumerable<T> ChooseUnique<T>(this IList<T> source, int numChoices)
        {
            var choices = new List<T>();
            var indexChoices = new HashSet<int>();
            while (indexChoices.Count < Math.Min(source.Count, numChoices))
            {
                indexChoices.Add(CmnRandom.Random.Next(0, source.Count));
            }
            foreach(var index in indexChoices)
            {
                choices.Add(source[index]);
            }
            return choices;
        }
    }
}
