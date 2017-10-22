using System;

namespace StockCutter.StockCutRepr
{
    public class Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override int GetHashCode()
        {
            return string.Format("{0},{1}", X, Y).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var otherPoint = obj as Point;
            return otherPoint != null && X == otherPoint.X && Y == otherPoint.Y;
        }

        public static Point operator -(Point lhs, Point rhs)
        {
            return new Point(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        public static Point operator +(Point lhs, Point rhs)
        {
            return new Point(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }
    }
}