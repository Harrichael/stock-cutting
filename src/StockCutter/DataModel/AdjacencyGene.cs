using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Utility;
using StockCutter.Extensions;

namespace StockCutter.StockCutRepr
{
    public class AdjacencyGene
    {
        public ShapeTemplate Template;
        public ClockwiseRotation Rotation;
        public Point RelativeShift;

        public AdjacencyGene(
            ShapeTemplate shapeTemplate,
            Point relativeShift,
            ClockwiseRotation absoluteRotation
        )
        {
            Template = shapeTemplate;
            RelativeShift = relativeShift;
            Rotation = absoluteRotation;
        }

        public static AdjacencyGene ConstructRandom(ShapeTemplate shapeTemplate, Point refPoint, int stockLength)
        {
            var rotation = shapeTemplate.Rotations.ToList()[CmnRandom.Random.Next(shapeTemplate.Rotations.Count())];
            var template = shapeTemplate.TemplateAt(rotation);
            int absXVal = CmnRandom.Random.Next(0 - template.MinX, stockLength - template.MaxX - 1);
            int absYVal = CmnRandom.Random.Next(0 - template.MinY, template.StockWidth - template.MaxY - 1);
            var absPoint = new Point(absXVal, absYVal);
            return new AdjacencyGene(shapeTemplate, absPoint - refPoint, rotation);
        }

        public ShapeCut Phenotype(Point refPoint)
        {
            return new ShapeCut(refPoint + RelativeShift, Template, Rotation);
        }

        public void Randomize(int stockLength, Point refPoint)
        {
            var rotations = Template.Rotations;
            Rotation = CmnRandom.Random.NextFrom(rotations);
            var template = Template.TemplateAt(Rotation);
            int absXVal = CmnRandom.Random.Next(0 - template.MinX, stockLength - template.MaxX - 1);
            int absYVal = CmnRandom.Random.Next(0 - template.MinY, template.StockWidth - template.MaxY - 1);
            RelativeShift = new Point(absXVal, absYVal) - refPoint;
        }

        public void CreepRandomize(int stockLength, Point refPoint)
        {
            var absoluteOrigin = refPoint + RelativeShift;
            var rotations = Template.Rotations;
            Rotation = CmnRandom.Random.NextFrom(rotations);
            var template = Template.TemplateAt(Rotation);
            int absXVal = CmnRandom.Random.NextBiased(0 - template.MinX, stockLength - template.MaxX - 1, absoluteOrigin.X, template.StockWidth/2);
            int absYVal = CmnRandom.Random.NextBiased(0 - template.MinY, template.StockWidth - template.MaxY - 1, absoluteOrigin.Y);
            RelativeShift = new Point(absXVal, absYVal) - refPoint;
        }

        public void CreepStableRandomize(int stockLength, Point refPoint, AdjacencyGene nextGene)
        {
            var originalShift = RelativeShift;
            CreepRandomize(stockLength, refPoint);
            var diffShift = RelativeShift - originalShift;
            if (nextGene != null)
            {
                nextGene.RelativeShift = nextGene.RelativeShift - diffShift;
            }
        }
    }
}
