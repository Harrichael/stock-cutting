using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StockCutter;
using StockCutter.Config;

namespace StockCutter
{
    public class EAConfig
    {
        public string ConfigFile { get; set; }
        public string ProblemFile { get; set; }
        public string SolutionFile { get; set; }
        public string LogFile { get; set; }
        public int Seed { get; set; }
        public int NumRuns { get; set; }
        public int NumParents { get; set; }
        public int NumOffspring { get; set; }
        public string SolutionInit { get; set; }
        public bool Sharing { get; set; }
        public ParentSelection ParentSelection { get; set; }
        public SurvivalSelection SurvivalSelection { get; set; }
        public Termination Termination { get; set; }
        public OffspringMutation Mutations { get; set; }
        public FitnessEval Fitness { get; set; }

        public static EAConfig FromArguments(string[] args)
        {
            var options = new EAConfig();

            options.ConfigFile = args[0];
            options.ProblemFile = args[1];
            options.SolutionFile = args[2];
            options.LogFile = args[3];
            options.Seed = Convert.ToInt32(args[4]);
            options.NumRuns = Convert.ToInt32(args[5]);
            options.NumParents = Convert.ToInt32(args[6]);
            options.NumOffspring = Convert.ToInt32(args[7]);
            options.SolutionInit = args[8];
            options.Sharing = Boolean.Parse(args[9]);

            options.ParentSelection = new ParentSelection();
            options.ParentSelection.SelectionWeight = (SelectionWeight)Enum.Parse(typeof(SelectionWeight), args[10]);
            options.ParentSelection.SelectPool = Convert.ToInt32(args[11]);
            options.ParentSelection.Replacement = Boolean.Parse(args[12]);
            options.ParentSelection.RateP = Convert.ToDouble(args[13]);
            options.ParentSelection.AdaptiveCrossover = Boolean.Parse(args[14]);
            options.ParentSelection.RateAdjacencyCrossover = Convert.ToDouble(args[15]);

            options.SurvivalSelection = new SurvivalSelection();
            options.SurvivalSelection.SelectionWeight = (SelectionWeight)Enum.Parse(typeof(SelectionWeight), args[16]);
            options.SurvivalSelection.SelectPool = Convert.ToInt32(args[17]);
            options.SurvivalSelection.DropParents = Boolean.Parse(args[18]);
            options.SurvivalSelection.Replacement = Boolean.Parse(args[19]);
            options.SurvivalSelection.RateP = Convert.ToDouble(args[20]);

            options.Termination = new Termination();
            options.Termination.EvalLimit = Convert.ToInt32(args[21]);
            options.Termination.GenerationLimit = Convert.ToInt32(args[22]);
            options.Termination.UnchangedBestLimit = Convert.ToInt32(args[23]);

            options.Mutations = new OffspringMutation();
            options.Mutations.Adaptive = Boolean.Parse(args[24]);
            options.Mutations.RateCreepRandom = Convert.ToDouble(args[25]);
            options.Mutations.RateRotateRandom = Convert.ToDouble(args[26]);
            options.Mutations.RateSlideRandom = Convert.ToDouble(args[27]);

            options.Fitness = new FitnessEval();
            options.Fitness.Length = Boolean.Parse(args[28]);
            options.Fitness.Width = Boolean.Parse(args[29]);
            options.Fitness.Cut = Boolean.Parse(args[30]);
            options.Fitness.Adjacents = Boolean.Parse(args[31]);

            return options;
        }
    }
}
