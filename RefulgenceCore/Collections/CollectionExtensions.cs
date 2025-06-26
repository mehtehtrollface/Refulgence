namespace Refulgence.Collections;

public static class CollectionExtensions
{
    #region ReadOnlySpan<> extensions

    public static TOut[] ToArray<TIn, TOut>(this ReadOnlySpan<TIn> source, Func<TIn, TOut> selector)
    {
        var result = new TOut[source.Length];
        for (var i = 0; i < source.Length; i++) {
            result[i] = selector(source[i]);
        }

        return result;
    }

    public static TOut[] ToArray<TIn, TOut>(this ReadOnlySpan<TIn> source, InFunc<TIn, TOut> selector)
    {
        var result = new TOut[source.Length];
        for (var i = 0; i < source.Length; i++) {
            result[i] = selector(in source[i]);
        }

        return result;
    }

    public static List<T> ToList<T>(this ReadOnlySpan<T> source)
    {
        var result = new List<T>(source.Length);
        source.AddTo(result);

        return result;
    }

    public static List<TOut> ToList<TIn, TOut>(this ReadOnlySpan<TIn> source, Func<TIn, TOut> selector)
    {
        var result = new List<TOut>(source.Length);
        source.AddTo(result, selector);

        return result;
    }

    public static List<TOut> ToList<TIn, TOut>(this ReadOnlySpan<TIn> source, InFunc<TIn, TOut> selector)
    {
        var result = new List<TOut>(source.Length);
        source.AddTo(result, selector);

        return result;
    }

    public static Dictionary<TKey, TValue> ToDictionary<TIn, TKey, TValue>(this ReadOnlySpan<TIn> source,
        Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(source.Length);
        source.AddTo(result, keySelector, valueSelector);

        return result;
    }

    public static Dictionary<TKey, TValue> ToDictionary<TIn, TKey, TValue>(this ReadOnlySpan<TIn> source,
        InFunc<TIn, TKey> keySelector, InFunc<TIn, TValue> valueSelector) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(source.Length);
        source.AddTo(result, keySelector, valueSelector);

        return result;
    }

    public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<TIn, TKey, TValue>(this ReadOnlySpan<TIn> source,
        Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector) where TKey : notnull
    {
        var result = new OrderedDictionary<TKey, TValue>(source.Length);
        source.AddTo(result, keySelector, valueSelector);

        return result;
    }

    public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<TIn, TKey, TValue>(this ReadOnlySpan<TIn> source,
        InFunc<TIn, TKey> keySelector, InFunc<TIn, TValue> valueSelector) where TKey : notnull
    {
        var result = new OrderedDictionary<TKey, TValue>(source.Length);
        source.AddTo(result, keySelector, valueSelector);

        return result;
    }

    public static IndexedList<TKey, T> ToIndexedList<T, TKey>(this ReadOnlySpan<T> source, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var result = new IndexedList<TKey, T>(source.Length, keySelector);
        source.AddTo(result);

        return result;
    }

    public static IndexedList<TKey, TOut> ToIndexedList<TIn, TKey, TOut>(this ReadOnlySpan<TIn> source,
        Func<TIn, TOut> selector, Func<TOut, TKey> keySelector) where TKey : notnull
    {
        var result = new IndexedList<TKey, TOut>(source.Length, keySelector);
        source.AddTo(result, selector);

        return result;
    }

    public static IndexedList<TKey, TOut> ToIndexedList<TIn, TKey, TOut>(this ReadOnlySpan<TIn> source,
        InFunc<TIn, TOut> selector, Func<TOut, TKey> keySelector) where TKey : notnull
    {
        var result = new IndexedList<TKey, TOut>(source.Length, keySelector);
        source.AddTo(result, selector);

        return result;
    }

    public static void CopyTo<T>(this ReadOnlySpan<T> source, T[] array, int arrayIndex)
    {
        if (source.Length > array.Length - arrayIndex) {
            throw new ArgumentException("Cannot copy a span into an array that is too small.");
        }

        for (var i = 0; i < source.Length; i++) {
            array[arrayIndex++] = source[i];
        }
    }

    public static void AddTo<T>(this ReadOnlySpan<T> source, ICollection<T> destination)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(source[i]);
        }
    }

    public static void AddTo<TIn, TOut>(this ReadOnlySpan<TIn> source, ICollection<TOut> destination,
        Func<TIn, TOut> selector)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(selector(source[i]));
        }
    }

    public static void AddTo<TIn, TOut>(this ReadOnlySpan<TIn> source, ICollection<TOut> destination,
        InFunc<TIn, TOut> selector)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(selector(in source[i]));
        }
    }

    public static void AddTo<TKey, TValue>(this ReadOnlySpan<(TKey Key, TValue Value)> source,
        IDictionary<TKey, TValue> destination)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(source[i].Key, source[i].Value);
        }
    }

    public static void AddTo<TKey, TValue>(this ReadOnlySpan<KeyValuePair<TKey, TValue>> source,
        IDictionary<TKey, TValue> destination)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(source[i].Key, source[i].Value);
        }
    }

    public static void AddTo<TIn, TKey, TValue>(this ReadOnlySpan<TIn> source,
        IDictionary<TKey, TValue> destination, Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(keySelector(source[i]), valueSelector(source[i]));
        }
    }

    public static void AddTo<TIn, TKey, TValue>(this ReadOnlySpan<TIn> source,
        IDictionary<TKey, TValue> destination, InFunc<TIn, TKey> keySelector, InFunc<TIn, TValue> valueSelector)
    {
        for (var i = 0; i < source.Length; i++) {
            destination.Add(keySelector(in source[i]), valueSelector(in source[i]));
        }
    }

    public static int GetSequenceHashCode<T>(this ReadOnlySpan<T> sequence)
    {
        var hashCode = new HashCode();
        foreach (var item in sequence) {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    public static int GetSequenceHashCode<T>(this ReadOnlySpan<T> sequence, IEqualityComparer<T>? comparer)
    {
        var hashCode = new HashCode();
        foreach (var item in sequence) {
            hashCode.Add(item, comparer);
        }

        return hashCode.ToHashCode();
    }

    #endregion

    #region IEnumerable<> extensions

    public static OrderedDictionary<TKey, TValue> ToOrderedDictionary<TIn, TKey, TValue>(this IEnumerable<TIn> source,
        Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector) where TKey : notnull
    {
        var result =
            source.TryGetNonEnumeratedCount(out var count)
                ? new OrderedDictionary<TKey, TValue>(count)
                : [];
        source.Select(item => (keySelector(item), valueSelector(item))).AddTo(result);

        return result;
    }

    public static IndexedList<TKey, T> ToIndexedList<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var result = source.TryGetNonEnumeratedCount(out var count)
            ? new(count, keySelector)
            : new IndexedList<TKey, T>(keySelector);
        source.AddTo(result);

        return result;
    }

    public static IndexedList<TKey, TOut> ToIndexedList<TIn, TOut, TKey>(this IEnumerable<TIn> source,
        Func<TIn, TOut> selector, Func<TOut, TKey> keySelector) where TKey : notnull
    {
        var result = source.TryGetNonEnumeratedCount(out var count)
            ? new(count, keySelector)
            : new IndexedList<TKey, TOut>(keySelector);
        source.Select(selector).AddTo(result);

        return result;
    }

    public static void CopyTo<T>(this IEnumerable<T> source, T[] array, int arrayIndex)
    {
        if (source.TryGetNonEnumeratedCount(out var count) && count > array.Length - arrayIndex) {
            throw new ArgumentException("Cannot copy a collection into an array that is too small.");
        }

        foreach (var item in source) {
            array[arrayIndex++] = item;
        }
    }

    public static void CopyTo<T>(this IEnumerable<T> source, Span<T> destination)
    {
        if (source.TryGetNonEnumeratedCount(out var count) && count > destination.Length) {
            throw new ArgumentException("Cannot copy a collection into an array that is too small.");
        }

        var i = 0;
        foreach (var item in source) {
            destination[i++] = item;
        }
    }

    public static void AddTo<T>(this IEnumerable<T> source, ICollection<T> destination)
    {
        foreach (var item in source) {
            destination.Add(item);
        }
    }

    public static void AddTo<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> source,
        IDictionary<TKey, TValue> destination)
    {
        foreach (var item in source) {
            destination.Add(item.Key, item.Value);
        }
    }

    public static void AddTo<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source,
        IDictionary<TKey, TValue> destination)
    {
        foreach (var item in source) {
            destination.Add(item.Key, item.Value);
        }
    }

    public static ISet<T> ToSet<T>(this IEnumerable<T> enumerable)
        => enumerable as ISet<T> ?? enumerable.ToHashSet();

    public static int GetSequenceHashCode<T>(this IEnumerable<T> sequence)
    {
        var hashCode = new HashCode();
        foreach (var item in sequence) {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    public static int GetSequenceHashCode<T>(this IEnumerable<T> sequence, IEqualityComparer<T>? comparer)
    {
        var hashCode = new HashCode();
        foreach (var item in sequence) {
            hashCode.Add(item, comparer);
        }

        return hashCode.ToHashCode();
    }

    #endregion

    public delegate TResult InFunc<T, out TResult>(in T arg)
        where T : allows ref struct
        where TResult : allows ref struct;
}
