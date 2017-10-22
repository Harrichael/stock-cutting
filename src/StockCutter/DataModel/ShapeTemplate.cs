using System;
using System.Collections.Generic;
using System.Linq;

namespace StockCutter.StockCutRepr
{
    public class ShapeTemplate
    {
        public int StockWidth;
        private Dictionary<ClockwiseRotation, ShapeTemplateRotated> templates;

        public int MaxLength
        {
            get { return templates.Values.Max(str => str.Length); }
        }

        public ShapeTemplate(ShapeTemplateRotated originalPosition)
        {
            StockWidth = originalPosition.StockWidth;
            templates = new Dictionary<ClockwiseRotation, ShapeTemplateRotated>();
            templates[ClockwiseRotation.None] = originalPosition;
            templates[ClockwiseRotation.Quarter] = originalPosition.FromRotation(ClockwiseRotation.Quarter);
            templates[ClockwiseRotation.Half] = templates[ClockwiseRotation.Quarter]
                .FromRotation(ClockwiseRotation.Quarter);
            templates[ClockwiseRotation.ThreeQuarters] = templates[ClockwiseRotation.Half]
                .FromRotation(ClockwiseRotation.Quarter);
            if (originalPosition.Width > StockWidth)
            {
                templates.Remove(ClockwiseRotation.None);
                templates.Remove(ClockwiseRotation.Half);
            }
            if (originalPosition.Length > StockWidth)
            {
                templates.Remove(ClockwiseRotation.Quarter);
                templates.Remove(ClockwiseRotation.ThreeQuarters);
            }
            if (templates.Count == 0)
            {
                throw new NotSupportedException("Smallest side of shape greater than stock width!");
            }
        }

        public static ShapeTemplate ConstructFromProblemDef(IEnumerable<string> segments, int stockWidth)
        {
            return new ShapeTemplate(ShapeTemplateRotated.ConstructFromProblemDef(segments, stockWidth));
        }

        public IEnumerable<ClockwiseRotation> Rotations
        {
            get { return templates.Keys; }
        }

        public ShapeTemplateRotated TemplateAt(ClockwiseRotation rotation)
        {
            return templates[rotation];
        }
    }
}