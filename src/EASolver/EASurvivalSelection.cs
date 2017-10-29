using System.Collections.Generic;
using System.Linq;
using System;
using StockCutter;
using StockCutter.Extensions;

namespace StockCutter.EASolver
{
    public static class EASurvivalSelection<T>
    {
        public static IEnumerable<EvalNode<T>> Truncate(IEnumerable<EvalNode<T>> parents,
            IEnumerable<EvalNode<T>> offspring)
        {
            return new[] {parents, offspring}
                .SelectMany(p => p)
                .OrderByDescending(o => o.Fitness)
                .Take(parents.Count());
        }

        public static Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<T>>
            CreateCrowdingSelector(
                Func<IEnumerable<EvalNode<T>>, EvalNode<T>> tourneyWinner,
                Func<IEnumerable<T>, IEnumerable<EvalNode<T>>> evaluate,
                Func<T, T, int> solutionDiff,
                int kCandidates,
                int sizeNextGen
            )
        {
            return (parents, offspring) =>
                {
                    var evalSolutions = evaluate((new [] {parents, offspring}).SelectMany(p => p));
                    var evalPairs = evalSolutions
                        .ToDictionary(e => e.Individual, e => e);
                    var selectPool = parents.ToList();
                    foreach(var child in offspring)
                    {
                        var parent = selectPool
                            .ChooseUnique(kCandidates)
                            .MinByValue(p => solutionDiff(p, child));
                        if (!parent.Equals(tourneyWinner(new [] {evalPairs[parent], evalPairs[child]}).Individual))
                        {
                            selectPool.Add(child);
                            selectPool.Remove(parent);
                        }
                    }
                    return selectPool;
                };
        }

        public static Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<T>>
            CreateTournamentSelector(
                Func<IEnumerable<EvalNode<T>>, EvalNode<T>> tourneyWinner,
                Func<IEnumerable<T>, IEnumerable<EvalNode<T>>> evaluate,
                int kCandidates,
                bool replacement,
                bool dropParents,
                int sizeNextGen
            )
        {
            return (parents, offspring) =>
                {
                    var selectPool = evaluate(new [] {parents.SkipWhile(p => dropParents), offspring}.SelectMany(s => s)).ToList();
                    var nextGen = new List<T>();
                    for (var i = 0; i < sizeNextGen; i++)
                    {
                        EvalNode<T> choice = null;
                        if (replacement)
                        {
                            choice = tourneyWinner(selectPool.Choose(kCandidates));
                        }
                        else
                        {
                            choice = tourneyWinner(selectPool.ChooseUnique(kCandidates));
                        }
                        nextGen.Add(choice.Individual);
                        selectPool.Remove(choice);
                    }
                    return nextGen;
                };
        }
    }
}
