namespace StockCutter.StockCutRepr
{
    public class Stock
    {
        public int Width;
        public int Length;

        public Stock(int width, int length)
        {
            Width = width;
            Length = length;
        }

        public T[,] GetSheet<T>()
        {
            return new T[Length, Width];
        }
    }
}