#if NETSTANDARD2_1
namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    // netstandard2.1 already has GetValueOrDefault; we only need the aggregator overload
    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) =>
        System.Linq.Enumerable.ToDictionary(source, kv => kv.Key, kv => kv.Value);
}
#elif !NET
using ModelContextProtocol;

namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.GetValueOrDefault(key, default!);
    }

    public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        Throw.IfNull(dictionary);

        return dictionary.TryGetValue(key, out TValue? value) ? value : defaultValue;
    }

    public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) =>
        System.Linq.Enumerable.ToDictionary(source, kv => kv.Key, kv => kv.Value);
}
#endif
