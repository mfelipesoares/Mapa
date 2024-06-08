using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Needle.Engine
{
    internal static class ExtensionMethods
    {
#if !UNITY_2021_1_OR_NEWER
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
        }
#endif

#if !UNITY_2021_1_OR_NEWER
        public static bool Contains(this string str, string value, StringComparison comparison)
        {
            return str.IndexOf(value, comparison) >= 0;
        }
#endif
    }
}