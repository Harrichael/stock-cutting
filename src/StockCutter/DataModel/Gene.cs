using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Utility;
using StockCutter.Extensions;

namespace StockCutter.StockCutRepr
{
    public class Gene
    {
        public ShapeTemplate Template;
        public ClockwiseRotation Rotation;
        public Point Origin;

        public Gene(
            ShapeTemplate shapeTemplate,
            Point origin,
            ClockwiseRotation absoluteRotation
        )
        {
            Template = shapeTemplate;
            Origin = origin;
            Rotation = absoluteRotation;
        }

        public static Gene ConstructRandom(ShapeTemplate shapeTemplate, int stockLength)
        {
            var rotation = shapeTemplate.Rotations.ToList()[CmnRandom.Random.Next(shapeTemplate.Rotations.Count())];
            var template = shapeTemplate.TemplateAt(rotation);
            int absXVal = CmnRandom.Random.Next(0 - template.MinX, stockLength - template.MaxX - 1);
            int absYVal = CmnRandom.Random.Next(0 - template.MinY, template.StockWidth - template.MaxY - 1);
            var absPoint = new Point(absXVal, absYVal);
            return new Gene(shapeTemplate, absPoint, rotation);
        }

        public ShapeCut Phenotype()
        {
            return new ShapeCut(Origin, Template, Rotation);
        }

        public void CreepRandomize(int stockLength)
        {
            var rotations = Template.Rotations;
            Rotation = CmnRandom.Random.NextFrom(rotations);
            var template = Template.TemplateAt(Rotation);
            int absXVal = CmnRandom.Random.NextBiased(0 - template.MinX, stockLength - template.MaxX - 1, Origin.X);
            int absYVal = CmnRandom.Random.NextBiased(0 - template.MinY, template.StockWidth - template.MaxY - 1, Origin.Y);
            Origin = new Point(absXVal, absYVal);
        }
    }
}
