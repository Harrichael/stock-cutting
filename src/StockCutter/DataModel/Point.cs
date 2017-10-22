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

        public int ManhattanDistance(Point other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public override bool Equals(object other)
        {
            return this.Equals(other as Point);
        }

        public bool Equals(Point other)
        {
            if (Object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (Object.ReferenceEquals(other, this))
            {
                return true;
            }
            
            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return (X == other.X) && (Y == other.Y);
        }

        public override int GetHashCode()
        {
            return string.Format("{0},{1}", X, Y).GetHashCode();
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
