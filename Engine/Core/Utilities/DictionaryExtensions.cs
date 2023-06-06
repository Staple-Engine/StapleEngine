using System.Collections.Generic;

namespace Staple
{
    internal static class DictionaryExtensions
    {
        public static void AddOrSetKey<K, T>(this IDictionary<K, T> dictionary, K key, T value)
        {
            if(dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }
        }
    }
}
