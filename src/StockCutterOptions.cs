﻿using System;
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
        public ParentSelection ParentSelection { get; set; }
        public SurvivalSelection SurvivalSelection { get; set; }
        public Termination Termination { get; set; }
        public OffspringMutation Mutations { get; set; }
        public bool ForceValid { get; set; }
        public double PenaltyWeight { get; set; }
        public bool AdaptivePenalty { get; set; }
        public bool AdaptivePenaltyRepair { get; set; }
        public double RepairRate { get; set; }

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

            options.ParentSelection = new ParentSelection();
            options.ParentSelection.SelectionWeight = (SelectionWeight)Enum.Parse(typeof(SelectionWeight), args[8]);
            options.ParentSelection.SelectPool = Convert.ToInt32(args[9]);
            options.ParentSelection.NumMates = Convert.ToInt32(args[10]);
            options.ParentSelection.Replacement = Boolean.Parse(args[11]);
            options.ParentSelection.RateP = Convert.ToDouble(args[12]);

            options.SurvivalSelection = new SurvivalSelection();
            options.SurvivalSelection.SelectionWeight = (SelectionWeight)Enum.Parse(typeof(SelectionWeight), args[13]);
            options.SurvivalSelection.SelectPool = Convert.ToInt32(args[14]);
            options.SurvivalSelection.DropParents = Boolean.Parse(args[15]);
            options.SurvivalSelection.Replacement = Boolean.Parse(args[16]);
            options.SurvivalSelection.RateP = Convert.ToDouble(args[17]);

            options.Termination = new Termination();
            options.Termination.EvalLimit = Convert.ToInt32(args[18]);
            options.Termination.GenerationLimit = Convert.ToInt32(args[19]);
            options.Termination.UnchangedAvgGenerationLimit = Convert.ToInt32(args[20]);
            options.Termination.UnchangedBestGenerationLimit = Convert.ToInt32(args[21]);

            options.Mutations = new OffspringMutation();
            options.Mutations.Adaptive = Boolean.Parse(args[22]);
            options.Mutations.RatePerOffspring = Convert.ToDouble(args[23]);
            options.Mutations.RateCreepRandom = Convert.ToDouble(args[24]);
            options.Mutations.RateCreepStableRandom = Convert.ToDouble(args[25]);
            options.Mutations.RateSwapPosition = Convert.ToDouble(args[26]);
            options.Mutations.RateSwapInsertion = Convert.ToDouble(args[27]);

            options.ForceValid = Boolean.Parse(args[28]);
            options.PenaltyWeight = Convert.ToDouble(args[29]);
            options.AdaptivePenalty = Boolean.Parse(args[30]);
            options.AdaptivePenaltyRepair = Boolean.Parse(args[31]);
            options.RepairRate = Convert.ToDouble(args[32]);

            return options;
        }
    }
}