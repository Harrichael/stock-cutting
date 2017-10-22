
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StockCutter.Config;
using StockCutter.StockCutRepr;
using StockCutter.Utility;

namespace StockCutter
{
    public class StockCutter
    {
        public static void Main(string[] args)
        {
            var config = EAConfig.FromArguments(args);
            CmnRandom.InitRandom(config.Seed);
            var problem = ProblemFile.Parse(config.ProblemFile);
            new StockCutterRunner().RunAll(config, problem.Item1, problem.Item2);
        }
    }
}