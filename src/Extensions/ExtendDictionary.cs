using System;
using System.Collections.Generic;
using System.Linq;
using StockCutter.Utility;

namespace StockCutter.Extensions
{
    public static class ExtendDictionary
    {
        public static V SafeGetValue<K, V>(this IDictionary<K, V> source, K key)
        {
            if (!source.ContainsKey(key))
            {
                return default(V);
            }
            else
            {
                return source[key];
            }
        }
    }
}
