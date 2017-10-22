using System;
using System.Collections.Generic;
using System.Linq;

namespace StockCutter.StockCutRepr
{
    public class ShapeTemplateRotated
    {
        public List<Point> ReferencePoints;
        public int Width;
        public int Length;
        public int StockWidth;
        public int MinX;
        public int MaxX;
        public int MinY;
        public int MaxY;

        public ShapeTemplateRotated(IEnumerable<Point> referencePoints, int stockWidth)
        {
            ReferencePoints = referencePoints.ToList();
            StockWidth = stockWidth;
            foreach (var point in referencePoints)
            {
                if (point.X < MinX)
                {
                    MinX = point.X;
                } else if (point.X > MaxX)
                {
                    MaxX = point.X;
                }

                if (point.Y < MinY)
                {
                    MinY = point.Y;
                } else if (point.Y > MaxY)
                {
                    MaxY = point.Y;
                }
            }
            Width = MaxY - MinY + 1;
            Length = MaxX - MinX + 1;
        }

        public static ShapeTemplateRotated ConstructFromProblemDef(IEnumerable<string> segments, int stockWidth)
        {
            var uniqueRefPoints = new HashSet<Point>();
            var currPoint = new Point(0, 0);
            uniqueRefPoints.Add(currPoint);
            foreach (var segment in segments)
            {
                var direction = segment[0];
                var segLength = Convert.ToInt32(segment.Substring(1, segment.Length - 1));
                List<Point> pointSegment;
                switch (direction)
                {
                    case 'U':
                        pointSegment = Enumerable.Range(1, segLength)
                            .Select(v => new Point(currPoint.X, currPoint.Y + v))
                            .ToList();
                        break;
                    case 'D':
                        pointSegment = Enumerable.Range(1, segLength)
                            .Select(v => new Point(currPoint.X, currPoint.Y - v))
                            .ToList();
                        break;
                    case 'L':
                        pointSegment = Enumerable.Range(1, segLength)
                            .Select(v => new Point(currPoint.X - v, currPoint.Y))
                            .ToList();
                        break;
                    case 'R':
                        pointSegment = Enumerable.Range(1, segLength)
                            .Select(v => new Point(currPoint.X + v, currPoint.Y))
                            .ToList();
                        break;
                    default:
                        throw new NotImplementedException(String.Format("Unknown direction in shape def {0}", direction));
                }
                uniqueRefPoints.UnionWith(pointSegment);
                currPoint = pointSegment.Last();
            }

            return new ShapeTemplateRotated(uniqueRefPoints.ToList(), stockWidth);
        }

        public ShapeTemplateRotated FromRotation(ClockwiseRotation rotation)
        {
            var rotatedRefPoints = new List<Point>(ReferencePoints);
            for (int i = 0; i < (int) rotation; i++)
            {
                rotatedRefPoints = rotatedRefPoints.Select(p => new Point(p.Y, -p.X)).ToList();
            }
            return new ShapeTemplateRotated(rotatedRefPoints, StockWidth);
        }
    }
}
