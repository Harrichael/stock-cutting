
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
            EAConfig config;
            try
            {
                config = EAConfig.FromArguments(args);
            } catch (System.FormatException)
            {
                Console.WriteLine(String.Join(" ", args));
                return;
            }
            CmnRandom.InitRandom(config.Seed);
            var problem = ProblemFile.Parse(config.ProblemFile);
            new StockCutterRunner().RunAll(config, problem.Item1, problem.Item2);
        }
    }
}
