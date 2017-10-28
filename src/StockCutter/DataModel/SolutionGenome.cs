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
        public bool AdaptiveCrossover;
        public double RateAdjacencyCrossover;
        public bool AdaptiveMutation;
        public double RateCreepRandom;
        public double RateRotateRandom;
        public double RateSlideRandom;

        public SolutionGenome(
            IEnumerable<Gene> genes,
            bool adaptiveCrossover,
            double rateAdjacencyCrossover,
            bool adaptiveMutation,
            double rateCreepRandom,
            double rateRotateRandom,
            double rateSlideRandom
        )
        {
            Genes = genes.ToList();
            AdaptiveCrossover = adaptiveCrossover;
            RateAdjacencyCrossover = rateAdjacencyCrossover;
            AdaptiveMutation = adaptiveMutation;
            RateCreepRandom = rateCreepRandom;
            RateRotateRandom = rateRotateRandom;
            RateSlideRandom = rateSlideRandom;
        }

        public static SolutionGenome ConstructRandom(IEnumerable<ShapeTemplate> shapeTemplates, int stockLength, EAConfig config)
        {
            var genes = shapeTemplates.Select(s => Gene.ConstructRandom(s, stockLength));
            return new SolutionGenome(
                genes,
                config.ParentSelection.AdaptiveCrossover,
                config.ParentSelection.RateAdjacencyCrossover,
                config.Mutations.Adaptive,
                config.Mutations.RateCreepRandom,
                config.Mutations.RateRotateRandom,
                config.Mutations.RateSlideRandom
            );
        }

        public int SolutionLength
        {
            get
            {
                return Phenotype().Max(sc => sc.Origin.X + sc.ShapeTemplateRotation.MaxX);
            }
        }

        public int SolutionWidth
        {
            get
            {
                return Phenotype().Max(sc => sc.Origin.Y + sc.ShapeTemplateRotation.MaxY);
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

                var parent2TemplateToGenes = parent2.Genes
                    .ToDictionary(g => g.Template, g => g);
                var genePairs = parent1.Genes.Select(g => Tuple.Create(g, parent2TemplateToGenes[g.Template]));
                if (CmnRandom.Random.NextDouble() < parent1.RateAdjacencyCrossover)
                {
                    var refPoint = new Point(0, 0);
                    var parent1Shifts = parent1.Genes.Select(g => {
                                var shift = g.Origin - refPoint;
                                refPoint = g.Origin;
                                return Tuple.Create(g.Template, shift);
                            })
                            .ToDictionary(gs => gs.Item1, gs => gs.Item2);
                    refPoint = new Point(0, 0);
                    var parent2Shifts = parent2.Genes.Select(g => {
                                var shift = g.Origin - refPoint;
                                refPoint = g.Origin;
                                return Tuple.Create(g.Template, shift);
                            })
                            .ToDictionary(gs => gs.Item1, gs => gs.Item2);
                    var adjacencies = new Dictionary<ShapeTemplate, List<ShapeTemplate>>();
                    ShapeTemplate lastParent1 = null;
                    ShapeTemplate lastParent2 = null;
                    foreach (var genePair in parent1.Genes.Zip(parent2.Genes, (g1, g2) => Tuple.Create(g1.Template, g2.Template)))
                    {
                        if (!adjacencies.ContainsKey(genePair.Item1))
                        {
                            adjacencies[genePair.Item1] = new List<ShapeTemplate>();
                        }
                        if (!adjacencies.ContainsKey(genePair.Item2))
                        {
                            adjacencies[genePair.Item2] = new List<ShapeTemplate>();
                        }
                        if (lastParent1 != null)
                        {
                            adjacencies[genePair.Item1].Add(lastParent1);
                            adjacencies[lastParent1].Add(genePair.Item1);
                        }
                        if (lastParent2 != null)
                        {
                            adjacencies[genePair.Item2].Add(lastParent2);
                            adjacencies[lastParent2].Add(genePair.Item2);
                        }
                        lastParent1 = genePair.Item1;
                        lastParent2 = genePair.Item2;
                    }
                    var templateOrder = new Dictionary<ShapeTemplate, int>();
                    int orderCounter = 0;
                    ShapeTemplate currTemplate = parent1.Genes.ChooseSingle().Template;
                    templateOrder[currTemplate] = orderCounter;
                    while(templateOrder.Count() < parent1.Genes.Count())
                    {
                        orderCounter += 1;
                        var adjs = adjacencies[currTemplate]
                            .Where(t => !templateOrder.ContainsKey(t))
                            .Mode().ToList();
                        if (adjs.Count() > 0)
                        {
                            currTemplate = adjs.ChooseSingle();
                        }
                        else
                        {
                            currTemplate = parent1.Genes.Where(g => !templateOrder.ContainsKey(g.Template)).ToList().ChooseSingle().Template;
                        }
                        templateOrder[currTemplate] = orderCounter;
                    }
                    refPoint = new Point(0, 0);
                    foreach (var genePair in genePairs.OrderBy(gp => templateOrder[gp.Item1.Template]))
                    {
                        var geneShift = (new List<Tuple<Gene, Point>>{ Tuple.Create(genePair.Item1, parent1Shifts[genePair.Item1.Template]),
                                                                  Tuple.Create(genePair.Item2, parent2Shifts[genePair.Item2.Template]) })
                                    .ChooseSingle(i => stock.Length - i.Item2.X);
                        refPoint = refPoint + geneShift.Item2;
                        childGenes.Add(new Gene(genePair.Item1.Template, refPoint, geneShift.Item1.Rotation));
                    }
                }
                else
                {
                    foreach (var genePair in genePairs)
                    {
                        var gene = (new List<Gene>{ genePair.Item1, genePair.Item2}).ChooseSingle(g => stock.Length - g.Origin.X);
                        childGenes.Add(new Gene(gene.Template, gene.Origin, gene.Rotation));
                    }
                }
                Func<bool, IEnumerable<SolutionGenome>, Func<SolutionGenome, double>, double> MutateValue = (mutate, ps, getValue) => {
                    var value = getValue(ps.ToList().ChooseSingle());
                    if (!mutate)
                    {
                        return value;
                    }
                    var randValue = value + 0.05 * (CmnRandom.Random.NextDouble() - 0.5);
                    return Math.Min(Math.Max(randValue, 0), 1);
                };
                var child = new SolutionGenome(
                    childGenes,
                    parent1.AdaptiveCrossover,
                    MutateValue(parent1.AdaptiveCrossover, new SolutionGenome[] {parent1, parent2}, (parent) => parent.RateAdjacencyCrossover),
                    parent1.AdaptiveMutation,
                    MutateValue(parent1.AdaptiveMutation, new SolutionGenome[] {parent1, parent2}, (parent) => parent.RateCreepRandom),
                    MutateValue(parent1.AdaptiveMutation, new SolutionGenome[] {parent1, parent2}, (parent) => parent.RateRotateRandom),
                    MutateValue(parent1.AdaptiveMutation, new SolutionGenome[] {parent1, parent2}, (parent) => parent.RateSlideRandom)
                );
                child.Repair(stock);
                return child;
            };
        }
    }
}
