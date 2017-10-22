using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Extensions;

namespace StockCutter.EASolver
{
    public static class EAParentSelection<T>
    {
        public static Func<IEnumerable<EvalNode<T>>, IEnumerable<T>> CreateTournamentSelector(
            Func<IEnumerable<T>, T> breeder,
            Func<IEnumerable<EvalNode<T>>, EvalNode<T>> tourneyWinner,
            int kCandidates,
            bool replacement,
            int numMates,
            int numOffspring)
        {
            return (population) =>
            {
                var selectPool = new List<EvalNode<T>>(population);
                var parentPairs = new List<IEnumerable<T>>();
                for (var i = 0; i < numOffspring; i++)
                {
                    var parentPair = new List<T>();
                    parentPairs.Add(parentPair);
                    for (var k = 0; k < numMates; k++)
                    {
                        if (replacement)
                        {
                            parentPair.Add(tourneyWinner(selectPool.Choose(kCandidates)).Individual);
                        }
                        else
                        {
                            parentPair.Add(tourneyWinner(selectPool.ChooseUnique(kCandidates)).Individual);
                        }
                    }
                }
                var offspring = new List<T>();
                foreach (var parents in parentPairs)
                {
                    offspring.Add(breeder(parents));
                }
                return offspring;
            };
        }
    }
}