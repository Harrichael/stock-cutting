using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace StockCutter.StockCutRepr
{
    public static class ProblemFile
    {
        public static Tuple<Stock, List<ShapeTemplate>> Parse(string problemFile)
        {
            var file = new StreamReader(problemFile);
            int width = Convert.ToInt32(file.ReadLine().Split()[0]);
            var shapes = new List<ShapeTemplate>();
            string line;
            while ((line = file.ReadLine()) != null)
            {
                var shape = ShapeTemplate.ConstructFromProblemDef(line.Split(), width);
                shapes.Add(shape);
            }
            int length = shapes.Sum(s => s.MaxLength);
            var stock = new Stock(width, length);
            return Tuple.Create(stock, shapes);
        }
    }
}