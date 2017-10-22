using System;
using System.Collections.Generic;
using System.Linq;

namespace StockCutter.Config
{
    public class OffspringMutation
    {
        public bool Adaptive;
        public double RatePerOffspring;
        public double RateCreepRandom;
        public double RateCreepStableRandom;
        public double RateSwapPosition;
        public double RateSwapInsertion;
    }
}