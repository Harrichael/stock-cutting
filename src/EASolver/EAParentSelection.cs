using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Extensions;

namespace StockCutter.EASolver
{
    public static class EAParentSelection<T>
    {
        public static Func<IEnumerable<T>, IEnumerable<T>> CreateTournamentSelector(
            Func<T, T, T> breeder,
            Func<IEnumerable<EvalNode<T>>, EvalNode<T>> tourneyWinner,
            Func<IEnumerable<T>, IEnumerable<EvalNode<T>>> evaluate,
            int kCandidates,
            bool replacement,
            int numOffspring)
        {
            return (population) =>
            {
                var selectPool = new List<EvalNode<T>>(evaluate(population));
                var parentPairs = new List<Tuple<T, T>>();
                for (var i = 0; i < numOffspring; i++)
                {
                    if (replacement)
                    {
                        parentPairs.Add(Tuple.Create(
                                tourneyWinner(selectPool.Choose(kCandidates)).Individual,
                                tourneyWinner(selectPool.Choose(kCandidates)).Individual
                        ));
                    }
                    else
                    {
                        parentPairs.Add(Tuple.Create(
                                tourneyWinner(selectPool.ChooseUnique(kCandidates)).Individual,
                                tourneyWinner(selectPool.ChooseUnique(kCandidates)).Individual
                        ));
                    }
                }
                var offspring = new List<T>();
                foreach (var parentPair in parentPairs)
                {
                    offspring.Add(breeder(parentPair.Item1, parentPair.Item2));
                }
                return offspring;
            };
        }
    }
}
