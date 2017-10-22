using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.StockCutRepr;

namespace StockCutter.EASolver
{
    public class EvalNode<T>
    {
        public T Individual;
        public Lazy<int> Fitness;

        public EvalNode(T individual, Func<T, int> evaluate)
        {
            Individual = individual;
            Fitness = new Lazy<int>(() => evaluate(individual));
        }
    }

    public class EvolveSolution<T>
    {
        public IEnumerable<EvalNode<T>> InitPopulation;
        public Func<IEnumerable<EvalNode<T>>, IEnumerable<T>> Breed;
        public Action<IEnumerable<T>> Mutator;
        public Func<IEnumerable<EvalNode<T>>, IEnumerable<EvalNode<T>>, IEnumerable<EvalNode<T>>> SelectSurvivors;
        public Func<T, int> Evaluate;
        public Func<IEnumerable<EvalNode<T>>, bool> Terminate;

        public EvolveSolution(
            IEnumerable<T> initPopulation,
            Func<IEnumerable<EvalNode<T>>, IEnumerable<T>> breed,
            Action<IEnumerable<T>> mutator,
            Func<IEnumerable<EvalNode<T>>, IEnumerable<EvalNode<T>>, IEnumerable<EvalNode<T>>> selectSurvivors,
            Func<T, int> evaluate,
            Func<IEnumerable<EvalNode<T>>, bool> terminate
        )
        {
            InitPopulation = initPopulation.Select(i => new EvalNode<T>(i, evaluate));
            Breed = breed;
            Mutator = mutator;
            SelectSurvivors = selectSurvivors;
            Evaluate = evaluate;
            Terminate = terminate;
        }

        public IEnumerable<IEnumerable<EvalNode<T>>> Solve()
        {
            var population = InitPopulation.ToList();
            yield return population;
            while (!Terminate(population))
            {
                var offspring = Breed(population).Select(o => new EvalNode<T>(o, Evaluate));
                Mutator(offspring.Select(o => o.Individual));
                population = SelectSurvivors(population, offspring).ToList();
                yield return population;
            }
        }
    }
}