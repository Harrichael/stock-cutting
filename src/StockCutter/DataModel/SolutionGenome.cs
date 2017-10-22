using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Config;
using StockCutter.Utility;
using StockCutter.Extensions;

namespace StockCutter.StockCutRepr
{

    public class SolutionGenome
    {
        public List<Gene> Genes;
        public bool AdaptiveMutation;
        public double RateCreepRandom;

        public SolutionGenome(
            IEnumerable<Gene> genes,
            bool adaptiveMutation,
            double rateCreepRandom
        )
        {
            Genes = genes.ToList();
            AdaptiveMutation = adaptiveMutation;
            RateCreepRandom = rateCreepRandom;
        }

        public static SolutionGenome ConstructRandom(IEnumerable<ShapeTemplate> shapeTemplates, int stockLength, EAConfig config)
        {
            var genes = shapeTemplates.Select(s => Gene.ConstructRandom(s, stockLength));
            return new SolutionGenome(
                genes,
                config.Mutations.Adaptive,
                config.Mutations.RateCreepRandom
            );
        }

        public int SolutionLength
        {
            get
            {
                return Phenotype().Max(sc => sc.Origin.X + sc.ShapeTemplateRotation.MaxX);
            }
        }

        public IEnumerable<ShapeCut> Phenotype()
        {
            return Genes.Select(g => g.Phenotype());
        }


        public void Repair(Stock stock)
        {
            var sheet = stock.GetSheet<Gene>();
            var unPlacedGenes = Genes.ToList();
            while (unPlacedGenes.Count() > 0)
            {
                var gene = unPlacedGenes.ChooseSingle();
                var shape = gene.Phenotype();
                if (!shape.IsInBounds(stock))
                {
                    gene.CreepRandomize(stock.Length);
                    shape = gene.Phenotype();
                }
                var conflict = shape.Place(sheet, gene);
                if (conflict == null)
                {
                    unPlacedGenes.Remove(gene);
                }
                else
                {
                    conflict.Phenotype().UnPlace(sheet);
                    if (conflict.Repair(sheet, stock, shape))
                    {
                        conflict.Phenotype().Place(sheet, conflict);
                    }
                    else
                    {
                        conflict.CreepRandomize(stock.Length);
                        unPlacedGenes.Add(conflict);
                    }
                }
            }
        }

        public int NumOverlaps()
        {
            int totalShapePoints = 0;
            var placePoints = new HashSet<Point>();
            foreach(var gene in Genes)
            {
                var shape = gene.Phenotype();
                totalShapePoints += shape.Points.Count();
                placePoints.UnionWith(shape.Points);
            }
            return totalShapePoints - placePoints.Count();
        }

        public static Func<SolutionGenome, SolutionGenome, SolutionGenome>
            GetParentBreeder(Stock stock)
        {
            return (parent1, parent2) =>
            {
                var childGenes = new List<Gene>();
                foreach (var genePair in parent1.Genes.Zip(parent2.Genes, (first, second) => Tuple.Create(first, second)))
                {
                    var gene = (new List<Gene>{genePair.Item1, genePair.Item2}).ChooseSingle(g => stock.Length - g.Origin.X);
                    childGenes.Add(new Gene(gene.Template, gene.Origin, gene.Rotation));
                }

                Func<IEnumerable<SolutionGenome>, Func<SolutionGenome, double>, double> MutateValue = (ps, getValue) => {
                    var value = getValue(ps.ToList().ChooseSingle());
                    if (!parent1.AdaptiveMutation)
                    {
                        return value;
                    }
                    var randValue = value + 0.35 * (CmnRandom.Random.NextDouble() - CmnRandom.Random.NextDouble());
                    return Math.Min(Math.Max(randValue, 0), 1);
                };
                var child = new SolutionGenome(
                    childGenes,
                    parent1.AdaptiveMutation,
                    MutateValue(new SolutionGenome[] {parent1, parent2}, (parent) => parent.RateCreepRandom)
                );
                child.Repair(stock);
                return child;
            };
        }
    }
}
