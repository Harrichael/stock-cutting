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
        public List<SolutionGenome> BestPopulation;
        public string LogFileText;
        private List<List<Tuple<int, float, int>>> logFileData;


        public void RunAll(EAConfig config, Stock stock, IEnumerable<ShapeTemplate> shapes)
        {
            logFileData = new List<List<Tuple<int, float, int>>>();
            LogFileText = "";

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
            var solutions = new List<Dictionary<ShapeTemplate, ShapeCut>>();
            foreach (var bestSolution in BestPopulation)
            {
                var solutionDict = new Dictionary<ShapeTemplate, ShapeCut>();
                solutions.Add(solutionDict);
                foreach (var shape in bestSolution.Phenotype())
                {
                    solutionDict[shape.Template] = shape;
                }
            }
            using(var solutionFile = new StreamWriter(config.SolutionFile, false))
            {
                foreach (var solutionDict in solutions)
                {
                    solutionFile.WriteLine("\n[Solution]\n");
                    foreach (var shapeTempl in shapes)
                    {
                        var shape = solutionDict[shapeTempl];
                        solutionFile.WriteLine("{0},{1},{2}", shape.Origin.X, shape.Origin.Y, (int)shape.Rotation);
                    }
                }
            }
        }

        public void Run(EAConfig config, Stock stock, IEnumerable<ShapeTemplate> shapes)
        {
            var initialSolutions = Enumerable
                .Range(0, config.NumParents)
                .Select(i => GenerateRandomSolution(shapes, stock, config));

            int evalCounter = 0;
            Func<IEnumerable<SolutionGenome>, List<EvalNode<SolutionGenome>>> evaluate = (population) =>
            {
                evalCounter += population.Count();

                var popFits = new Dictionary<SolutionGenome, List<int>>();

                foreach(var individual in population)
                {
                    popFits[individual] = new List<int>();
                    if (config.Fitness.Length)
                    {
                        popFits[individual].Add(stock.Length - individual.SolutionLength);
                    }

                    if (config.Fitness.Width)
                    {
                        popFits[individual].Add(stock.Width - individual.SolutionWidth);
                    }

                    if (config.Fitness.Cut)
                    {
                        var placements = new Dictionary<Point, Gene>();
                        foreach(var gene in individual.Genes)
                        {
                            foreach(var point in gene.Phenotype().Points)
                            {
                                placements[point] = gene;
                            }
                        }
                        // Maximizing adjacent points is minimizing cuts
                        popFits[individual].Add(
                            individual.Genes.Sum( g => {
                                return g.Phenotype().AdjacentPoints
                                    .Where(p => placements.ContainsKey(p))
                                    .Select(p => placements[p])
                                    .Count();
                            })
                        );
                    }

                    if (config.Fitness.Adjacents)
                    {
                        var placements = new Dictionary<Point, Gene>();
                        foreach(var gene in individual.Genes)
                        {
                            foreach(var point in gene.Phenotype().Points)
                            {
                                placements[point] = gene;
                            }
                        }
                        // Minimize number of shapes that are adjacent to each other
                        popFits[individual].Add(
                            -individual.Genes.Sum( g => {
                                return new HashSet<Gene>(
                                    g.Phenotype().AdjacentPoints
                                    .Where(p => placements.ContainsKey(p))
                                    .Select(p => placements[p])
                                ).Count();
                            })
                        );
                    }
                }

                var fittedPopulation = new List<EvalNode<SolutionGenome>>();
                var levels = new Dictionary<SolutionGenome, int>();
                var dominatedBy = new Dictionary<SolutionGenome, List<SolutionGenome>>();
                if (popFits[population.First()].Count() > 1)
                {
                    foreach(var individual in population)
                    {
                        var fitnessTypes = popFits[individual];
                        dominatedBy[individual] = new List<SolutionGenome>();
                        foreach(var dominator in population)
                        {
                            var fitPairs = fitnessTypes
                                .Zip(popFits[dominator], (i, d) => Tuple.Create(i, d));

                            var ltFits = fitPairs.Where(el => el.Item1 > el.Item2);
                            var rtFits = fitPairs.Where(el => el.Item1 < el.Item2);
                            bool isDominated = ltFits.Count() == 0 && rtFits.Any();
                            if (isDominated)
                            {
                                dominatedBy[individual].Add(dominator);
                            }
                        }
                    }
                    var unFitted = new List<SolutionGenome>(population);
                    int levelCounter = 1;
                    while (unFitted.Count() > 0)
                    {
                        foreach(var individual in unFitted.ToList())
                        {
                            bool fit = true;
                            foreach(var dominator in dominatedBy[individual])
                            {
                                if (!levels.ContainsKey(dominator))
                                {
                                    fit = false;
                                    break;
                                }
                            }
                            if (fit)
                            {
                                levels[individual] = -levelCounter;
                                unFitted.Remove(individual);
                            }
                        }
                        levelCounter += 1;
                    }
                }
                foreach(var individual in population)
                {
                    var fitnessTypes = popFits[individual];
                    int fitness = 0;
                    if (fitnessTypes.Count() == 1)
                    {
                        fitness = fitnessTypes.First();
                    }
                    else if (fitnessTypes.Count() > 1)
                    {
                        fitness = levels[individual];
                    }
                    var newFit = new EvalNode<SolutionGenome>(individual, fitness);
                    fittedPopulation.Add(newFit);
                }
                return fittedPopulation;
            };

            var bestFitPopulation = new List<SolutionGenome>();
            int generationCounter = 0;
            Func<IEnumerable<SolutionGenome>, bool> terminate = (_population) =>
            {
                generationCounter += 1;
                var population = evaluate((new []{_population, bestFitPopulation}).SelectMany(p => p));
                int maxFitness = population.Max(i => i.Fitness);
                bestFitPopulation = population.Where(e => e.Fitness == maxFitness).Select(e => e.Individual).ToList();
                Console.WriteLine("Evals {0}\tLevels: {1}\tMutations: {2:0.000} {3:0.000} {4:0.000}\tCrossover: {5:0.000}",
                    evalCounter,
                    (new HashSet<int>(population.Select(e => e.Fitness))).Count(),
                    population.Sum(p => p.Individual.RateCreepRandom)/population.Count(),
                    population.Sum(p => p.Individual.RateRotateRandom)/population.Count(),
                    population.Sum(p => p.Individual.RateSlideRandom)/population.Count(),
                    population.Sum(p => p.Individual.RateAdjacencyCrossover)/population.Count()
                );
                bool evalLimitReached = config.Termination.EvalLimit != 0 && config.Termination.EvalLimit <= evalCounter;
                bool generationLimitReached =
                    config.Termination.GenerationLimit != 0 && config.Termination.GenerationLimit <= generationCounter;
                return evalLimitReached || generationLimitReached;
            };

            Func<IEnumerable<SolutionGenome>,
                 IEnumerable<SolutionGenome>,
                 IEnumerable<SolutionGenome>> survive;
            switch (config.SurvivalSelection.SelectionWeight)
            {
                case SelectionWeight.Truncate:
                    survive = (parents, offspring) =>
                    {
                        return evaluate(
                                    new[] {parents.SkipWhile(s => config.SurvivalSelection.DropParents), offspring}
                                    .SelectMany(p => p)
                                )
                            .OrderByDescending(o => o.Fitness)
                            .Select(o => o.Individual)
                            .Take(parents.Count());
                    };
                    break;
                case SelectionWeight.Random:
                    survive = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) => kChoices.ToList().ChooseSingle(),
                        evaluate,
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                case SelectionWeight.Best:
                    survive = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) => kChoices.MaxByValue((k) => k.Fitness),
                        evaluate,
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                case SelectionWeight.Fitness:
                    survive = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) =>
                        {
                            var totalFitness = kChoices.Sum(k => k.Fitness);
                            var fitPick = CmnRandom.Random.Next(0, totalFitness - 1);
                            return kChoices.First( k =>
                            {
                                fitPick -= k.Fitness;
                                return fitPick <= 0;
                            });
                        },
                        evaluate,
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                case SelectionWeight.Rank:
                    survive = EASurvivalSelection<SolutionGenome>.CreateTournamentSelector(
                        (kChoices) => kChoices
                            .OrderBy((k) => -k.Fitness)
                            .Select((k, index) => Tuple.Create(k, config.SurvivalSelection.RateP * Math.Pow(1 - config.SurvivalSelection.RateP, index)))
                            .Select(ki => Tuple.Create(ki.Item1, CmnRandom.Random.NextDouble() < ki.Item2))
                            .OrderBy(kp => !kp.Item2)
                            .First().Item1,
                        evaluate,
                        config.SurvivalSelection.SelectPool,
                        config.SurvivalSelection.Replacement,
                        config.SurvivalSelection.DropParents,
                        config.NumParents
                    );
                    break;
                default:
                    throw new NotImplementedException("Selection weight for survival not found");
            }

            Func<IEnumerable<SolutionGenome>,
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
                        evaluate,
                        config.ParentSelection.SelectPool,
                        config.ParentSelection.Replacement,
                        config.NumOffspring
                    );
                    break;
                case SelectionWeight.Best:
                    breed = EAParentSelection<SolutionGenome>.CreateTournamentSelector(
                        SolutionGenome.GetParentBreeder(stock),
                        (kChoices) => kChoices.MaxByValue((k) => k.Fitness),
                        evaluate,
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
                            var totalFitness = kChoices.Sum(k => k.Fitness);
                            var fitPick = CmnRandom.Random.Next(0, totalFitness - 1);
                            return kChoices.First( k =>
                            {
                                fitPick -= k.Fitness;
                                return fitPick <= 0;
                            });
                        },
                        evaluate,
                        config.ParentSelection.SelectPool,
                        config.ParentSelection.Replacement,
                        config.NumOffspring
                    );
                    break;
                case SelectionWeight.Rank:
                    breed = EAParentSelection<SolutionGenome>.CreateTournamentSelector(
                        SolutionGenome.GetParentBreeder(stock),
                        (kChoices) => kChoices
                            .OrderBy((k) => -k.Fitness)
                            .Select((k, index) => Tuple.Create(k, config.ParentSelection.RateP * Math.Pow(1 - config.ParentSelection.RateP, index)))
                            .Select(ki => Tuple.Create(ki.Item1, CmnRandom.Random.NextDouble() < ki.Item2))
                            .OrderBy(kp => !kp.Item2)
                            .First().Item1,
                        evaluate,
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
                        if (CmnRandom.Random.NextDouble() < individual.RateRotateRandom)
                        {
                            gene.RotateRandomize();
                        }
                        if (CmnRandom.Random.NextDouble() < individual.RateSlideRandom)
                        {
                            gene.SlideRandomize(stock.Length);
                        }
                    }
                    individual.Repair(stock);
                }
            };

            var ea = new EvolveSolution<SolutionGenome>(
                initialSolutions,
                breed,
                mutator,
                survive,
                terminate
            );
            var newLogData = new List<Tuple<int, float, int>>();
            BestPopulation = new List<SolutionGenome>();
            foreach (var _population in ea.Solve())
            {
                var population = evaluate((new []{_population, BestPopulation}).SelectMany(p => p));
                int maxFitness = population.Max(i => i.Fitness);
                BestPopulation = population.Where(s => s.Fitness == maxFitness).Select(s => s.Individual).ToList();
                //newLogData.Add(new Tuple<int, float, int>(evalCounter, 0.0, 0));
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
