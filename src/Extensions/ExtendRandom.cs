using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Utility;

namespace StockCutter.Extensions
{
    public static class ExtendRandom
    {
        public static int NextBiased(this Random random, int lowBound, int upBound, int bias, int biasBound)
        {
            //Console.WriteLine("debug {0}, {1}, {2}", lowBound, upBound, bias);
            bias = Math.Min(upBound, bias);
            bias = Math.Max(lowBound, bias);
            var value = random.NextBiased(lowBound, upBound, bias);
            return Math.Max(Math.Min(value, bias + biasBound), bias - biasBound);
        }

        public static int NextBiased(this Random random, int lowBound, int upBound, int bias)
        {
            bias = Math.Min(upBound, bias);
            bias = Math.Max(lowBound, bias);
            int val;
            if ((random.NextDouble() < 0.5 || bias == upBound) && bias != lowBound)
            {
                val = random.Next(lowBound, bias - 1);
            }
            else
            {
                val = random.Next(bias + 1, upBound);
            }
            var correction = (int)Math.Floor(
                Math.Abs(random.NextDouble() - random.NextDouble()) *
                Math.Abs(random.NextDouble() - random.NextDouble()) *
                (val - bias));
            return bias + correction;
        }

        public static T NextFrom<T>(this Random random, IEnumerable<T> options)
        {
            return random.NextFrom(options.ToList());
        }

        public static T NextFrom<T>(this Random random, IList<T> options)
        {
            return options[random.Next(options.Count())];
        }
    }
}
