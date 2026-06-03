using System;
using System.Collections.Generic;
using System.Reflection;

namespace Plugins.Unity.Extension
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public static class CollectionExtensions
    {
        public static int GetValueOrDefault(this Dictionary<int, int> dictionary, int key, int defaultValue)
        {
            return dictionary == null
                ? throw new ArgumentNullException(nameof(dictionary))
                : dictionary.TryGetValue(key, out int value) ? value : defaultValue;
        }
    }
}