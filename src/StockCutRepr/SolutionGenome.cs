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
        public List<AdjacencyGene> Genes;
        public bool AdaptiveMutation;
        public double RatePerOffspring;
        public double RateCreepRandom;
        public double RateCreepStableRandom;
        public double RateSwapPosition;
        public double RateSwapInsertion;
        public bool ForceValid;
        public bool AdaptivePenalty;
        public double PenaltyWeight;
        public bool AdaptivePenaltyRepair;
        public double RepairRate;

        public SolutionGenome(
            IEnumerable<AdjacencyGene> genes,
            bool adaptiveMutation,
            double ratePerOffspring,
            double rateCreepRandom,
            double rateCreepStableRandom,
            double rateSwapPosition,
            double rateSwapInsertion,
            bool forceValid,
            bool adaptivePenalty,
            double penaltyWeight,
            bool adaptivePenaltyRepair,
            double repairRate
        )
        {
            Genes = genes.ToList();
            AdaptiveMutation = adaptiveMutation;
            RatePerOffspring = ratePerOffspring;
            RateCreepRandom = rateCreepRandom;
            RateCreepStableRandom = rateCreepStableRandom;
            RateSwapPosition = rateSwapPosition;
            RateSwapInsertion = rateSwapInsertion;
            ForceValid = forceValid;
            AdaptivePenalty = adaptivePenalty;
            PenaltyWeight = penaltyWeight;
            AdaptivePenaltyRepair = adaptivePenaltyRepair;
            RepairRate = repairRate;
        }

        public static SolutionGenome ConstructRandom(IEnumerable<ShapeTemplate> shapeTemplates, int stockLength, EAConfig config)
        {
            var refPoint = new Point(0, 0);
            var genes = shapeTemplates
                .OrderBy(s => CmnRandom.Random.Next())
                .Select(s =>
                {
                    var currGene = AdjacencyGene.ConstructRandom(s, refPoint, stockLength);
                    refPoint = refPoint + currGene.RelativeShift;
                    return currGene;
                });
            return new SolutionGenome(
                genes,
                config.Mutations.Adaptive,
                config.Mutations.RatePerOffspring,
                config.Mutations.RateCreepRandom,
                config.Mutations.RateCreepStableRandom,
                config.Mutations.RateSwapPosition,
                config.Mutations.RateSwapInsertion,
                config.ForceValid,
                config.AdaptivePenalty,
                config.PenaltyWeight,
                config.AdaptivePenaltyRepair,
                config.RepairRate
            );
        }

        public int SolutionLength
        {
            get
            {
                return Phenotype().Max(sc => sc.Origin.X + sc.ShapeTemplateRotation.MaxX);
            }
        }

        public IEnumerable<Tuple<AdjacencyGene, ShapeCut>> GenePhenoPairs()
        {
            var refPoint = new Point(0, 0);
            foreach(var gene in Genes)
            {
                yield return Tuple.Create(gene, gene.Phenotype(refPoint));
                refPoint = refPoint + gene.RelativeShift;
            }
        }

        public IEnumerable<ShapeCut> Phenotype()
        {
            return GenePhenoPairs().Select(p => p.Item2);
        }

        public void SwapPosition(ShapeTemplate geneTemp1, ShapeTemplate geneTemp2)
        {
            var tempToPair = GenePhenoPairs().ToDictionary(p => p.Item1.Template, p => p);
            var gene1 = tempToPair[geneTemp1].Item1;
            var gene2 = tempToPair[geneTemp2].Item1;
            var absOriginGene1 = tempToPair[geneTemp1].Item2.Origin;
            tempToPair[geneTemp1].Item2.Origin = tempToPair[geneTemp2].Item2.Origin;
            tempToPair[geneTemp2].Item2.Origin = absOriginGene1;
        }

        public void SwapInsertion(ShapeTemplate geneTemp1, ShapeTemplate geneTemp2)
        {
            var tempToIndex = Genes
                .Select((g, i) => new {gene = g, index = i})
                .ToDictionary(p => p.gene.Template, p => p.index);
            var geneIndex1 = tempToIndex[geneTemp1];
            var geneIndex2 = tempToIndex[geneTemp2];
            Genes.Swap(geneIndex1, geneIndex2);
        }

        public void Repair(Stock stock, bool forceRepair=false)
        {
            var sheet = stock.GetSheet<AdjacencyGene>();
            var unPlacedGenes = Genes.ToList();
            var prevGenes = Genes
                .SelectTwo((prevGene, gene) => Tuple.Create(prevGene, gene))
                .ToDictionary(p => p.Item2.Template, p => p.Item1);
            var nextGenes = Genes
                .SelectTwo((prevGene, gene) => Tuple.Create(prevGene, gene))
                .ToDictionary(p => p.Item1.Template, p => p.Item2);
            Func<AdjacencyGene, Point> calcAbsOrigin = (gene) =>
            {
                var absOrigin = new Point(0, 0);
                var currGene = gene;
                while (prevGenes.ContainsKey(currGene.Template))
                {
                    absOrigin = absOrigin + currGene.RelativeShift;
                    currGene = prevGenes[currGene.Template];
                }
                absOrigin = absOrigin + currGene.RelativeShift;
                return absOrigin;
            };
            while (unPlacedGenes.Count > 0)
            {
                var gene = unPlacedGenes[0];
                unPlacedGenes.RemoveAt(0);
                var shape = gene.Phenotype(calcAbsOrigin(gene) - gene.RelativeShift);
                if (!shape.IsInBounds(stock))
                {
                    var refPoint = shape.Origin - gene.RelativeShift;
                    gene.CreepStableRandomize(stock.Length, refPoint, nextGenes.SafeGetValue(gene.Template));
                    shape = gene.Phenotype(refPoint);
                }
                // Don't remove overlaps if we have penalty on
                while (ForceValid || forceRepair || CmnRandom.Random.NextDouble() < RepairRate)
                {
                    var conflict = shape.Place(sheet, gene);
                    if (conflict == null)
                    {
                        break;
                    }
                    else
                    {
                        var conflictRefPoint = calcAbsOrigin(conflict) - conflict.RelativeShift;
                        var conflictShape = conflict.Phenotype(conflictRefPoint);
                        conflictShape.UnPlace(sheet);
                        conflict.CreepStableRandomize(stock.Length, conflictRefPoint, nextGenes.SafeGetValue(conflict.Template));
                        unPlacedGenes.Add(conflict);
                    }
                }
            }
        }

        public int NumOverlaps()
        {
            int totalShapePoints = 0;
            var placePoints = new HashSet<Point>();
            var refPoint = new Point(0, 0);
            foreach(var gene in Genes)
            {
                var shape = gene.Phenotype(refPoint);
                totalShapePoints += shape.Points.Count();
                placePoints.UnionWith(shape.Points);
                refPoint = refPoint + gene.RelativeShift;
            }
            return totalShapePoints - placePoints.Count();
        }

        public static Func<IEnumerable<SolutionGenome>, SolutionGenome>
            GetParentBreeder(Stock stock)
        {
            return (_parents) =>
            {
                var parents = _parents.ToList();
                // Determine adjacencies, construct data about parents
                var shapeToGenes = new Dictionary<ShapeTemplate, List<AdjacencyGene>>();
                var lParents = parents.ToList();
                foreach (var gene in lParents.SelectMany(p => p.Genes))
                {
                    if (!shapeToGenes.ContainsKey(gene.Template))
                    {
                        shapeToGenes[gene.Template] = new List<AdjacencyGene>();
                    }
                    shapeToGenes[gene.Template].Add(gene);
                }
                var geneAdjacents = new Dictionary<AdjacencyGene, Tuple<ShapeTemplate, ShapeTemplate>>();
                var adjacents = new Dictionary<ShapeTemplate, List<ShapeTemplate>>();
                var prevShapes = new Dictionary<ShapeTemplate, List<ShapeTemplate>>();
                foreach(var parent in lParents)
                {
                    foreach(var pair in parent.Genes.SelectTwo((p, c) => new {prev = p, curr = c}))
                    {
                        if (!adjacents.ContainsKey(pair.prev.Template))
                        {
                            adjacents[pair.prev.Template] = new List<ShapeTemplate>();
                        }
                        if (!adjacents.ContainsKey(pair.curr.Template))
                        {
                            adjacents[pair.curr.Template] = new List<ShapeTemplate>();
                        }
                        if (!prevShapes.ContainsKey(pair.curr.Template))
                        {
                            prevShapes[pair.curr.Template] = new List<ShapeTemplate>();
                        }
                        if (!geneAdjacents.ContainsKey(pair.prev))
                        {
                            geneAdjacents[pair.prev] = new Tuple<ShapeTemplate, ShapeTemplate>(null, pair.curr.Template);
                        }
                        else
                        {
                            geneAdjacents[pair.prev] = Tuple.Create(geneAdjacents[pair.prev].Item1, pair.curr.Template);
                        }
                        if (!geneAdjacents.ContainsKey(pair.curr))
                        {
                            geneAdjacents[pair.curr] = new Tuple<ShapeTemplate, ShapeTemplate>(pair.curr.Template, null);
                        }
                        adjacents[pair.prev.Template].Add(pair.curr.Template);
                        adjacents[pair.curr.Template].Add(pair.prev.Template);
                        prevShapes[pair.curr.Template].Add(pair.prev.Template);
                    }
                }
                // Order child shapes
                var allShapes = adjacents.Keys.ToList();
                var frontier = new HashSet<ShapeTemplate>(allShapes);
                var childShapes = new List<ShapeTemplate>();
                //ShapeTemplate lastChildShape = allShapes.ChooseSingle();
                ShapeTemplate lastChildShape = parents.ChooseSingle().Genes.First().Template;
                childShapes.Add(lastChildShape);
                frontier.Remove(lastChildShape);
                while (frontier.Count > 0)
                {
                    var options = adjacents[lastChildShape].Where(s => frontier.Contains(s));
                    if (options.Count() > 0)
                    {
                        lastChildShape = options
                            .Mode()
                            .OrderBy(shape => CmnRandom.Random.NextDouble() * adjacents[shape].Where(s => frontier.Contains(s)).Count())
                            .First();
                    }
                    else
                    {
                        lastChildShape = allShapes.Where(s => frontier.Contains(s)).ToList().ChooseSingle();
                    }
                    childShapes.Add(lastChildShape);
                    frontier.Remove(lastChildShape);
                }
                // Construct gene data, preserve relative ordering already chosen
                var childGenes = new List<AdjacencyGene>();
                lastChildShape = null;
                var nextChildShapeIndex = 0;
                foreach (var childShape in childShapes)
                {
                    nextChildShapeIndex += 1;
                    ShapeTemplate nextChildShape = null;
                    if (childShapes.Count() > nextChildShapeIndex)
                    {
                        nextChildShape = childShapes[nextChildShapeIndex];
                    }
                    var gene = shapeToGenes[childShape]
                        .OrderBy(g => CmnRandom.Random.NextDouble())
                        .OrderBy(g => {
                            int score = 0;
                            if (geneAdjacents[g].Item1 != lastChildShape && geneAdjacents[g].Item2 != lastChildShape)
                            {
                                score += 1;
                            }
                            if (geneAdjacents[g].Item1 != nextChildShape && geneAdjacents[g].Item2 != nextChildShape)
                            {
                                score += 1;
                            }
                            return score;
                        })
                        .First();
                    Point relativeShift = gene.RelativeShift;
                    if ( (geneAdjacents[gene].Item2 == lastChildShape || geneAdjacents[gene].Item1 == nextChildShape) && nextChildShape != null )
                    {
                        var backRelativeShift = shapeToGenes[nextChildShape]
                            .OrderBy(g => CmnRandom.Random.NextDouble())
                            .OrderBy(g => {
                                int score = 0;
                                if (geneAdjacents[g].Item1 != gene.Template)
                                {
                                    score += 1;
                                }
                                return score;
                            })
                            .First().RelativeShift;
                        relativeShift = new Point(0, 0) - backRelativeShift;
                    }
                    childGenes.Add(new AdjacencyGene(childShape, relativeShift, gene.Rotation));
                    lastChildShape = childShape;
                }
                Func<List<SolutionGenome>, Func<SolutionGenome, double>, double> MutateValue = (ps, getValue) => {
                    var value = getValue(ps.ChooseSingle());
                    if (!parents.First().AdaptiveMutation)
                    {
                        return value;
                    }
                    var randValue = value + 0.35 * (CmnRandom.Random.NextDouble() - CmnRandom.Random.NextDouble());
                    return Math.Min(Math.Max(randValue, 0), 1);
                };

                Func<List<SolutionGenome>, Func<SolutionGenome, double>, double> MutatePenalty = (ps, getValue) => {
                    var value = getValue(ps.ChooseSingle());
                    if (!parents.First().AdaptivePenalty)
                    {
                        return value;
                    }
                    value = value + 0.5 * (CmnRandom.Random.NextDouble() - CmnRandom.Random.NextDouble());
                    value += ps.ChooseSingle().NumOverlaps()
                        * 0.15 * Math.Abs(CmnRandom.Random.NextDouble() - CmnRandom.Random.NextDouble());
                    return Math.Max(value, 0);
                };

                Func<List<SolutionGenome>, Func<SolutionGenome, double>, double> MutateRepair = (ps, getValue) => {
                    var value = getValue(ps.ChooseSingle());
                    if (!parents.First().AdaptivePenaltyRepair)
                    {
                        return value;
                    }
                    var randValue = value + 0.35 * (CmnRandom.Random.NextDouble() - CmnRandom.Random.NextDouble());
                    randValue -= .05 * Math.Abs(CmnRandom.Random.NextDouble() - CmnRandom.Random.NextDouble());
                    return Math.Min(Math.Max(randValue, 0), 1);
                };

                var child = new SolutionGenome(
                    childGenes,
                    parents.First().AdaptiveMutation,
                    MutateValue(parents, (parent) => parent.RatePerOffspring),
                    MutateValue(parents, (parent) => parent.RateCreepRandom),
                    MutateValue(parents, (parent) => parent.RateCreepStableRandom),
                    MutateValue(parents, (parent) => parent.RateSwapPosition),
                    MutateValue(parents, (parent) => parent.RateSwapInsertion),
                    parents.First().ForceValid,
                    parents.First().AdaptivePenalty,
                    MutatePenalty(parents, (parent) => parent.PenaltyWeight),
                    parents.First().AdaptivePenaltyRepair,
                    MutateRepair(parents, (parent) => parent.RepairRate)
                );
                child.Repair(stock);
                return child;
            };
        }
    }
}
