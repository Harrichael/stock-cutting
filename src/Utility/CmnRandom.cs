using System;

namespace StockCutter.Utility
{
    public class CmnRandom
    {
        public static Random Random;

        public static void InitRandom(int seed)
        {
            Random = new Random(seed);
        }
    }
}