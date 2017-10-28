using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.StockCutRepr;

namespace StockCutter.EASolver
{
    public class EvalNode<T>
    {
        public T Individual;
        public int Fitness;

        public EvalNode(T individual, int fitness)
        {
            Individual = individual;
            Fitness = fitness;
        }
    }

    public class EvolveSolution<T>
    {
        public IEnumerable<T> InitPopulation;
        public Func<IEnumerable<T>, IEnumerable<T>> Breed;
        public Action<IEnumerable<T>> Mutator;
        public Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<T>> SelectSurvivors;
        public Func<IEnumerable<T>, bool> Terminate;

        public EvolveSolution(
            IEnumerable<T> initPopulation,
            Func<IEnumerable<T>, IEnumerable<T>> breed,
            Action<IEnumerable<T>> mutator,
            Func<IEnumerable<T>, IEnumerable<T>, IEnumerable<T>> selectSurvivors,
            Func<IEnumerable<T>, bool> terminate
        )
        {
            InitPopulation = initPopulation;
            Breed = breed;
            Mutator = mutator;
            SelectSurvivors = selectSurvivors;
            Terminate = terminate;
        }

        public IEnumerable<IEnumerable<T>> Solve()
        {
            var population = InitPopulation.ToList();
            yield return population;
            while (!Terminate(population))
            {
                var offspring = Breed(population);
                Mutator(offspring);
                population = SelectSurvivors(population, offspring).ToList();
                yield return population;
            }
        }
    }
}
