using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using StockCutter.Config;
using StockCutter.StockCutRepr;
using StockCutter.EASolver;
using StockCutter.Utility;
using StockCutter.Extensions;

namespace StockCutter
{
    public class StockCutterRunner
    {
        public EvalNode<SolutionGenome> BestSolution;
        public string LogFileText;
        private List<List<Tuple<int, float, int>>> logFileData;

        public void RunAll(EAConfig config, Stock stock, IEnumerable<ShapeTemplate> shapes)
        {
            logFileData = new List<List<Tuple<int, float, int>>>();
            LogFileText = "";
            BestSolution = null;

            for (int r = 0; r < config.NumRuns; r++)
            {
                Console.WriteLine("Starting Run {0}", r);
                Run(config, stock, shapes);
            }

            // Log File
            using(var logFile = new StreamWriter(config.LogFile, false))
            {
                int runCounter = 0;
                logFile.WriteLine("[Config Log]");
                logFile.WriteLine(File.ReadAllText(config.ConfigFile));

                logFile.WriteLine("\n\n[Results Log]");
                foreach (var runData in logFileData)
                {
                    logFile.WriteLine("\n[Run {0}]", runCounter);
                    foreach (var data in runData)
                    {
                        logFile.WriteLine("{0}\t{1}\t{2}", data.Item1, data.Item2, data.Item3);
                    }
                    runCounter += 1;
                }
            }

            // Solution File
            var solutionDict = new Dictionary<ShapeTemplate, ShapeCut>();
            Console.WriteLine("Best Fitness: {0}", BestSolution.Fitness.Value);
            foreach (var shape in BestSolution.Individual.Phenotype())
            {
                solutionDict[shape.Template] = shape;
            }
            using(var solutionFile = new StreamWriter(config.SolutionFile, false))
            {
                foreach (var shapeTempl in shapes)
                {
                    var shape = solutionDict[shapeTempl];
                    solutionFile.WriteLine("{0},{1},{2}", shape.Origin.X, shape.Origin.Y, (int)shape.Rotation);
                }
            }
        }

        public void Run(EAConfig config, Stock stock, IEnumerable<ShapeTemplate> shapes)
        {
            var initialSolutions = Enumerable
                .Range(0, config.NumParents)
                .Select(i => GenerateRandomSolution(shapes, stock, config));

            int evalCounter = 0;
            Func<SolutionGenome, int> evaluate = (solutionGenome) =>
            {
                evalCounter += 1;
                return stock.Length - solutionGenome.SolutionLength;
            };

            int lastBestFitness = -1;
            float lastAvgFitness = -1;
            int bestUnchangedGenCounter = 0;
            int avgUnchangedGenCounter = 0;
            int generationCounter = 0;
            Func<IEnumerable<EvalNode<SolutionGenome>>, bool> terminate = (population) =>
            {
                Lazy<int> bestFitness = new Lazy<int>(() => population.MaxByValue(i => i.Fitness.Value).Fitness.Value);
                Lazy<float> avgFitness = new Lazy<float>(() => population.Sum(i => i.Fitness.Value) / (float)population.Count());
                generationCounter += 1;
                Console.WriteLine("Fitness: {0}\tMutations: {1:0.000}\tCrossover: {2:0.000}",
                    bestFitness.Value,
                    population.Sum(p => p.Individual.RateCreepRandom)/population.Count(),
                    population.Sum(p => p.Individual.RateAdjacencyCrossover)/population.Count()
                );
                bool evalLimitReached = config.Termination.EvalLimit != 0 && config.Termination.EvalLimit <= evalCounter;
                bool generationLimitReached =
                    config.Termination.GenerationLimit != 0 && config.Termination.GenerationLimit <= generationCounter;
                bool bestGenLimitReached = config.Termination.UnchangedBestGenerationLimit != 0;
                if (bestGenLimitReached)
                {
                    bestGenLimitReached = false;
                    if (bestFitness.Value == lastBestFitness)
                    {
                        bestUnchangedGenCounter += 1;
                        if (config.Termination.UnchangedBestGenerationLimit <= bestUnchangedGenCounter)
                        {
                            bestGenLimitReached = true;
                        }
                    }
                    else
                    {
                        bestUnchangedGenCounter = 0;
                        lastBestFitness = bestFitness.Value;
                    }
                }
                bool avgGenLimitReached = config.Termination.UnchangedAvgGenerationLimit != 0;
                if (avgGenLimitReached)
                {
                    avgGenLimitReached = false;
                    if (Math.Abs(avgFitness.Value - lastAvgFitness) < 0.001)
                    {
                        avgUnchangedGenCounter += 1;
                        if (config.Termination.UnchangedAvgGenerationLimit <= avgUnchangedGenCounter)
                        {
                            avgGenLimitReached = true;
                        }
                    }
                    else
                    {
                        avgUnchangedGenCounter = 0;
                        lastAvgFitness = avgFitness.Value;
                    }
                }
                return evalLimitReached || generationLimitReached || bestGenLimitReached || avgGenLimitReached;
            };

            Func<IEnumerable<EvalNode<SolutionGenome>>,
                 IEnumerable<EvalNode<SolutionGenome>>,
                 IEnumerable<EvalNode<SolutionGenome>>> truncate;
            switch (config.SurvivalSelection.SelectionWeight)
            {
                case SelectionWeight.Truncate:
                    truncate = (parents, offspring) =>
                    {
                        return new[] {parents.SkipWhile(s => config.SurvivalSelection.DropParents), offspring}
                            .SelectMany(p => p)
                            .OrderByDescending(o => o.Fitness.Value)
                            .Take(parents.Count());
                    };
                    break;
                case SelectionWeight.Random:
                    truncate = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) => kChoices.ToList().ChooseSingle(),
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                case SelectionWeight.Best:
                    truncate = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) => kChoices.MaxByValue((k) => k.Fitness.Value),
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                case SelectionWeight.Fitness:
                    truncate = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) =>
                        {
                            var totalFitness = kChoices.Sum(k => k.Fitness.Value);
                            var fitPick = CmnRandom.Random.Next(0, totalFitness - 1);
                            return kChoices.First( k =>
                            {
                                fitPick -= k.Fitness.Value;
                                return fitPick <= 0;
                            });
                        },
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                case SelectionWeight.Rank:
                    truncate = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) => kChoices
                            .OrderBy((k) => -k.Fitness.Value)
                            .Select((k, index) => Tuple.Create(k, config.SurvivalSelection.RateP * Math.Pow(1 - config.SurvivalSelection.RateP, index)))
                            .Select(ki => Tuple.Create(ki.Item1, CmnRandom.Random.NextDouble() < ki.Item2))
                            .OrderBy(kp => !kp.Item2)
                            .First().Item1,
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                default:
                    throw new NotImplementedException("Selection weight for survival not found");
            }

            Func<IEnumerable<EvalNode<SolutionGenome>>,
                IEnumerable<SolutionGenome>> breed;
            switch (config.ParentSelection.SelectionWeight)
            {
                case SelectionWeight.None:
                    breed = (population) =>
                    {
                        return Enumerable
                            .Range(0, config.NumOffspring)
                            .Select(i => GenerateRandomSolution(shapes, stock, config));
                    };
                    break;
                case SelectionWeight.Random:
                    breed = EAParentSelection<SolutionGenome>.CreateTournamentSelector(
                        SolutionGenome.GetParentBreeder(stock),
                        (kChoices) => kChoices.ToList().ChooseSingle(),
                        config.ParentSelection.SelectPool,
                        config.ParentSelection.Replacement,
                        config.NumOffspring
                    );
                    break;
                case SelectionWeight.Best:
                    breed = EAParentSelection<SolutionGenome>.CreateTournamentSelector(
                        SolutionGenome.GetParentBreeder(stock),
                        (kChoices) => kChoices.MaxByValue((k) => k.Fitness.Value),
                        config.ParentSelection.SelectPool,
                        config.ParentSelection.Replacement,
                        config.NumOffspring
                    );
                    break;
                case SelectionWeight.Fitness:
                    breed = EAParentSelection<SolutionGenome>.CreateTournamentSelector(
                        SolutionGenome.GetParentBreeder(stock),
                        (kChoices) =>
                        {
                            var totalFitness = kChoices.Sum(k => k.Fitness.Value);
                            var fitPick = CmnRandom.Random.Next(0, totalFitness - 1);
                            return kChoices.First( k =>
                            {
                                fitPick -= k.Fitness.Value;
                                return fitPick <= 0;
                            });
                        },
                        config.ParentSelection.SelectPool,
                        config.ParentSelection.Replacement,
                        config.NumOffspring
                    );
                    break;
                case SelectionWeight.Rank:
                    breed = EAParentSelection<SolutionGenome>.CreateTournamentSelector(
                        SolutionGenome.GetParentBreeder(stock),
                        (kChoices) => kChoices
                            .OrderBy((k) => -k.Fitness.Value)
                            .Select((k, index) => Tuple.Create(k, config.ParentSelection.RateP * Math.Pow(1 - config.ParentSelection.RateP, index)))
                            .Select(ki => Tuple.Create(ki.Item1, CmnRandom.Random.NextDouble() < ki.Item2))
                            .OrderBy(kp => !kp.Item2)
                            .First().Item1,
                        config.ParentSelection.SelectPool,
                        config.ParentSelection.Replacement,
                        config.NumOffspring
                    );
                    break;
                default:
                    throw new NotImplementedException("Selection weight for parents not handled");
            }

            Action<IEnumerable<SolutionGenome>> mutator = (population) =>
            {
                foreach (var individual in population)
                {
                    foreach (var gene in individual.Genes)
                    {
                        if (CmnRandom.Random.NextDouble() < individual.RateCreepRandom)
                        {
                            gene.CreepRandomize(stock.Length);
                        }
                    }
                    individual.Repair(stock);
                }
            };

            var ea = new EvolveSolution<SolutionGenome>(
                initialSolutions,
                breed,
                mutator,
                truncate,
                evaluate,
                terminate
            );
            var newLogData = new List<Tuple<int, float, int>>();
            foreach (var population in ea.Solve())
            {
                var popList = new List<EvalNode<SolutionGenome>>(population);
                var bestGenSolution = popList.MaxByValue(i => i.Fitness.Value);
                if ( BestSolution == null || (bestGenSolution.Fitness.Value > BestSolution.Fitness.Value)
                        && bestGenSolution.Individual.NumOverlaps() <= BestSolution.Individual.NumOverlaps() )
                {
                    BestSolution = bestGenSolution;
                }
                newLogData.Add(Tuple.Create(
                    evalCounter,
                    popList.Sum(i => i.Fitness.Value)/(float)popList.Count(),
                    bestGenSolution.Fitness.Value
                ));
            }
            logFileData.Add(newLogData);
        }

        public static SolutionGenome GenerateRandomSolution(IEnumerable<ShapeTemplate> shapes, Stock stock, EAConfig config)
        {
            var solution = SolutionGenome.ConstructRandom(shapes, stock.Length, config);
            solution.Repair(stock);
            return solution;
        }
    }
}
