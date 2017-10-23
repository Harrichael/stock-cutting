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

        static readonly int MaxRepairDistance = 12;
        public bool Repair(Gene[,] sheet, Stock stock, ShapeCut reserved)
        {
            var sheetPoints = Enumerable.Range(Origin.X - MaxRepairDistance/2, Origin.X + MaxRepairDistance/2)
                .SelectMany(x => Enumerable.Range(Origin.Y - MaxRepairDistance/2, Origin.Y + MaxRepairDistance/2).Select(y => new Point(x, y)))
                .Where(p => p.X >= 0 && p.X < sheet.GetLength(0) && p.Y >= 0 && p.Y < sheet.GetLength(1))
                .Where(p => sheet[p.X, p.Y] == null)
                .Where(p => p.ManhattanDistance(Origin) <= MaxRepairDistance)
                .OrderBy(p => p.ManhattanDistance(Origin) + p.ManhattanDistance(new Point(0, Origin.Y)))
                .ToArray();
            foreach(var point in sheetPoints)
            {
                Origin = point;
                foreach(var rotation in Template.Rotations)
                {
                    Rotation = rotation;
                    var pheno = Phenotype();
                    if (pheno.IsInBounds(stock) && !pheno.Intersects(reserved))
                    {
                        var conflict = pheno.Place(sheet, this);
                        if (conflict == null)
                        {
                            pheno.UnPlace(sheet);
                            return true;
                        }
                    }
                }
            }
            return false;
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

        public void RotateRandom()
        {
            var rotations = Template.Rotations;
            Rotation = CmnRandom.Random.NextFrom(rotations);
        }
    }
}
