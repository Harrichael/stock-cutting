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
                .OrderByDescending(o => o.Fitness.Value)
                .Take(parents.Count());
        }

        public static Func<IEnumerable<EvalNode<T>>, IEnumerable<EvalNode<T>>, IEnumerable<EvalNode<T>>>
            CreateTournamentSelector(
                Func<IEnumerable<EvalNode<T>>, EvalNode<T>> tourneyWinner,
                int kCandidates,
                bool replacement,
                bool dropParents,
                int sizeNextGen
            )
        {
            return (parents, offspring) =>
                {
                    var selectPool = new [] {parents.SkipWhile(p => dropParents), offspring}.SelectMany(s => s).ToList();
                    var nextGen = new List<EvalNode<T>>();
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
                        nextGen.Add(choice);
                        selectPool.Remove(choice);
                    }
                    return nextGen;
                };
        }
    }
}
