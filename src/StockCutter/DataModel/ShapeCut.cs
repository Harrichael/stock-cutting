using System;
using System.Collections.Generic;
using System.Linq;

namespace StockCutter.StockCutRepr
{
    public class ShapeCut
    {
        public Point Origin;
        public ShapeTemplate Template;
        public ClockwiseRotation Rotation;

        public ShapeCut(Point origin, ShapeTemplate template, ClockwiseRotation rotation)
        {
            Origin = origin;
            Rotation = rotation;
            Template = template;
        }

        public ShapeTemplateRotated ShapeTemplateRotation
        {
            get { return Template.TemplateAt(Rotation); }
        }

        public IEnumerable<Point> Points
        {
            get
            {
                return ShapeTemplateRotation.ReferencePoints
                    .Select(p => new Point(p.X + Origin.X, p.Y + Origin.Y));
            }
        }

        public bool IsInBounds(Stock stock)
        {
            foreach (var point in Points)
            {
                if (point.X < 0 || point.X >= stock.Length || point.Y < 0 || point.Y >= stock.Width)
                {
                    return false;
                }
            }
            return true;
        }

        public void UnPlace<T>(T[,] sheet)
        {
            foreach (var point in Points)
            {
                sheet[point.X, point.Y] = default(T);
            }
        }

        public T Place<T>(T[,] sheet, T item)
        {
            var placedPoints = new List<Point>();
            foreach (var point in Points)
            {
                if (sheet[point.X, point.Y] == null)
                {
                    sheet[point.X, point.Y] = item;
                    placedPoints.Add(point);
                }
                else
                {
                    foreach (var p in placedPoints)
                    {
                        sheet[p.X, p.Y] = default(T);
                    }
                    return sheet[point.X, point.Y];
                }
            }
            return default(T);
        }
    }
}