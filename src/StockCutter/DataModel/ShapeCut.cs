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

        public bool Intersects(ShapeCut other)
        {
            var otherPoints = new HashSet<Point>(other.Points);
            foreach (var thisPoint in Points)
            {
                if (otherPoints.Contains(thisPoint))
                {
                    return true;
                }
            }
            return false;
        }

        public int NumOverlaps(ShapeCut other)
        {
            int overlaps = 0;
            var otherPoints = new HashSet<Point>(other.Points);
            foreach (var thisPoint in Points)
            {
                if (otherPoints.Contains(thisPoint))
                {
                    overlaps += 1;
                }
            }
            return overlaps;
        }

        public IEnumerable<Point> Points
        {
            get
            {
                return ShapeTemplateRotation.ReferencePoints
                    .Select(p => new Point(p.X + Origin.X, p.Y + Origin.Y));
            }
        }

        public IEnumerable<Point> AdjacentPoints
        {
            get
            {
                var points = new HashSet<Point>(Points);
                return new HashSet<Point>(
                        points
                            .SelectMany(p => {
                                var adjs = new List<Point>();
                                adjs.Add(p - new Point(-1, 0));
                                adjs.Add(p - new Point(1, 0));
                                adjs.Add(p - new Point(0, -1));
                                adjs.Add(p - new Point(0, 1));
                                return adjs;
                            })
                            .Where(p => !points.Contains(p))
                    ).ToList();
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
                if (sheet[point.X, point.Y] != null)
                {
                    foreach (var p in placedPoints)
                    {
                        sheet[p.X, p.Y] = default(T);
                    }
                    return sheet[point.X, point.Y];
                }
                sheet[point.X, point.Y] = item;
                placedPoints.Add(point);
            }
            return default(T);
        }
    }
}
