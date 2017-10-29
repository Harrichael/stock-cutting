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
        private List<List<Tuple<int, List<double>>>> logFileData;


        public void RunAll(EAConfig config, Stock stock, IEnumerable<ShapeTemplate> shapes)
        {
            logFileData = new List<List<Tuple<int, List<double>>>>();
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
                logFile.WriteLine("[Config Log (seed = {0})]", config.Seed);
                logFile.WriteLine(File.ReadAllText(config.ConfigFile));

                logFile.WriteLine("\n\n[Results Log]");
                foreach (var runData in logFileData)
                {
                    logFile.WriteLine("\n[Run {0}]", runCounter);
                    foreach (var data in runData)
                    {
                        logFile.WriteLine("{0}\t{1}", data.Item1, String.Join("\t", data.Item2));
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
                solutionFile.WriteLine(solutions.Count());
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
            var initialSolutions = new List<SolutionGenome>();

            if (config.SolutionInit != "" && File.Exists(config.SolutionInit))
            {
                initialSolutions = ReadSolutions(config.SolutionInit, shapes.ToList(), stock, config).ToList();
            }

            if (initialSolutions.Count() < config.NumParents)
            {
                initialSolutions.AddRange(Enumerable
                    .Range(initialSolutions.Count(), config.NumParents)
                    .Select(i => GenerateRandomSolution(shapes, stock, config))
                    .ToList());
            }

            int evalCounter = 0;
            Func<SolutionGenome, List<int>> evaluateSolution = (individual) =>
            {
                var evals = new List<int>();
                if (config.Fitness.Length)
                {
                    evals.Add(stock.Length - individual.SolutionLength);
                }

                if (config.Fitness.Width)
                {
                    evals.Add(stock.Width - individual.SolutionWidth);
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
                    evals.Add(
                        individual.Genes.Sum( g => {
                            return g.Phenotype().AdjacentPoints
                                .Where(p => placements.ContainsKey(p))
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
                    // Minimize number of shapes that are adjacent to each other, also, keep it positive
                    evals.Add(
                        (individual.Genes.Count() * individual.Genes.Count())-individual.Genes.Sum( g => {
                            return new HashSet<Gene>(
                                g.Phenotype().AdjacentPoints
                                .Where(p => placements.ContainsKey(p))
                                .Select(p => placements[p])
                            ).Count();
                        })
                    );
                }
                return evals;
            };
            Func<IEnumerable<SolutionGenome>, List<EvalNode<SolutionGenome>>> evaluate = (population) =>
            {
                evalCounter += population.Count();

                var popFits = new Dictionary<SolutionGenome, List<int>>();

                foreach(var individual in population)
                {
                    popFits[individual] = evaluateSolution(individual);
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

            BestPopulation = new List<SolutionGenome>();
            int unchangedBest = 0;
            int generationCounter = 0;
            Func<IEnumerable<SolutionGenome>, bool> terminate = (population) =>
            {
                generationCounter += 1;

                var _evalPopulation = evaluate((new []{population, BestPopulation}).SelectMany(p => p))
                    .ToList();
                int maxFitness = _evalPopulation.Max(i => i.Fitness);
                // We need this unique population handling to prevent best pop bloat in moea
                var uniquePops = new Dictionary<string, EvalNode<SolutionGenome>>();
                foreach (var e in _evalPopulation.Where(s => s.Fitness == maxFitness))
                {
                    uniquePops[String.Join(",", evaluateSolution(e.Individual))] = e;
                }
                var evalPopulation = uniquePops.Values.ToList();

                bool isNewBest = true;
                var bestUniquePops = BestPopulation.ToDictionary(i => String.Join(",", evaluateSolution(i)), i => i);
                foreach (var newBest in evalPopulation)
                {
                    if (!bestUniquePops.ContainsKey(String.Join(",", evaluateSolution(newBest.Individual))))
                    {
                        isNewBest = false;
                        unchangedBest = 0;
                        break;
                    }
                }
                BestPopulation = evalPopulation.Select(e => e.Individual).ToList();
                if (isNewBest)
                {
                    unchangedBest += 1;
                }

                Console.WriteLine("Evals {0}\tLevels: {1}\tAvg Fitness: {2}\tBest Fitnesss {3}\tMutations: {4:0.000} {5:0.000} {6:0.000}\tCrossover: {7:0.000}",
                    evalCounter,
                    (new HashSet<int>(_evalPopulation.Select(e => e.Fitness))).Count(),
                    String.Join(",", BestPopulation
                        .Select(s => evaluateSolution(s))
                        .Aggregate(new List<int>{0, 0, 0, 0}, (lhs, rhs) => lhs.Zip(rhs, (l, r) => l + r).ToList())
                        .Select(total => String.Format("{0:0.000}", total / (double)BestPopulation.Count))
                    ),
                    String.Join(",", BestPopulation
                        .Select(s => evaluateSolution(s))
                        .Aggregate(new List<int>{0, 0, 0, 0}, (lhs, rhs) => lhs.Zip(rhs, (l, r) => Math.Max(l, r)).ToList())
                    ),
                    BestPopulation.Sum(p => p.RateCreepRandom)        / BestPopulation.Count(),
                    BestPopulation.Sum(p => p.RateRotateRandom)       / BestPopulation.Count(),
                    BestPopulation.Sum(p => p.RateSlideRandom)        / BestPopulation.Count(),
                    BestPopulation.Sum(p => p.RateAdjacencyCrossover) / BestPopulation.Count()
                );

                bool evalLimitReached = config.Termination.EvalLimit != 0 && config.Termination.EvalLimit <= evalCounter;
                bool generationLimitReached =
                    config.Termination.GenerationLimit != 0 && config.Termination.GenerationLimit < generationCounter;
                bool unchangedBestReached = config.Termination.UnchangedBestLimit != 0 && config.Termination.UnchangedBestLimit < unchangedBest;
                return evalLimitReached || generationLimitReached || unchangedBestReached;
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
            var newLogData = new List<Tuple<int, List<double>>>();
            foreach (var population in ea.Solve())
            {
                var avg = population
                    .Select(s => evaluateSolution(s))
                    .Aggregate(new List<int>{0, 0, 0, 0}, (lhs, rhs) => lhs.Zip(rhs, (l, r) => l + r).ToList())
                    .Select(total => total / (double)population.Count());
                var best = population
                    .Select(s => evaluateSolution(s))
                    .Aggregate(new List<int>{0, 0, 0, 0}, (lhs, rhs) => lhs.Zip(rhs, (l, r) => Math.Max(l, r)).ToList());
                var avgBest = avg
                    .Zip(best, (a, b) => new List<double>(new double[] {a, b}))
                    .SelectMany(i => i);
                newLogData.Add(Tuple.Create(evalCounter, avgBest.ToList()));
            }
            logFileData.Add(newLogData);
        }

        public static IEnumerable<SolutionGenome> ReadSolutions(string solutionFile, List<ShapeTemplate> shapes, Stock stock, EAConfig config)
        {
            using(var solFile = new StreamReader(solutionFile, false))
            {
                List<Gene> genes = null;
                int shapeCounter = 0;
                string line;
                while((line = solFile.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Contains("[Solution]"))
                    {
                        if (genes != null && genes.Any())
                        {
                            yield return SolutionGenome.ConstructConfig(genes, stock.Length, config);
                        }
                        genes = new List<Gene>();
                        shapeCounter = 0;
                    }
                    else if (line != "" && genes != null)
                    {
                        var lineData = line.Split(new [] {','}).Select(num => Convert.ToInt32(num)).ToList();
                        var point = new Point(lineData[0], lineData[1]);
                        var rotation = (ClockwiseRotation)(lineData[2]);
                        genes.Add(new Gene(shapes[shapeCounter], point, rotation));
                        shapeCounter += 1;
                    }
                }
                if (genes != null && genes.Any())
                {
                    yield return SolutionGenome.ConstructConfig(genes, stock.Length, config);
                }
            }
        }

        public static SolutionGenome GenerateRandomSolution(IEnumerable<ShapeTemplate> shapes, Stock stock, EAConfig config)
        {
            var solution = SolutionGenome.ConstructRandom(shapes, stock.Length, config);
            solution.Repair(stock);
            return solution;
        }
    }
}
