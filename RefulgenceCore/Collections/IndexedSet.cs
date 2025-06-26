using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Refulgence.Collections;

public sealed class IndexedSet<TKey, TItem> : IReadOnlyDictionary<TKey, TItem>, ISet<TItem> where TKey : notnull
{
    private readonly Dictionary<TKey, TItem> _dictionary;

    public IndexedSet(Func<TItem, TKey> keySelector)
    {
        KeySelector = keySelector;
        _dictionary = [];
    }

    public IndexedSet(int capacity, Func<TItem, TKey> keySelector)
    {
        KeySelector = keySelector;
        _dictionary = new(capacity);
    }

    public IndexedSet(Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        KeySelector = keySelector;
        _dictionary = new(comparer);
    }

    public IndexedSet(int capacity, Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        KeySelector = keySelector;
        _dictionary = new(capacity, comparer);
    }

    public Func<TItem, TKey> KeySelector { get; }

    public IEnumerator<TItem> GetEnumerator()
        => _dictionary.Values.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TItem>> IEnumerable<KeyValuePair<TKey, TItem>>.GetEnumerator()
        => _dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _dictionary.Values.GetEnumerator();

    void ICollection<TItem>.Add(TItem item)
        => Add(item);

    public void ExceptWith(IEnumerable<TItem> other)
    {
        foreach (var item in other) {
            Remove(item);
        }
    }

    public void IntersectWith(IEnumerable<TItem> other)
    {
        var otherSet = other.ToSet();
        foreach (var (key, value) in _dictionary) {
            if (!otherSet.Contains(value)) {
                _dictionary.Remove(key);
            }
        }
    }

    public bool IsProperSubsetOf(IEnumerable<TItem> other)
    {
        var otherSet = other.ToSet();
        return otherSet.Count > _dictionary.Count && _dictionary.Values.All(otherSet.Contains);
    }

    public bool IsProperSupersetOf(IEnumerable<TItem> other)
    {
        var otherSet = other.ToSet();
        return otherSet.Count < _dictionary.Count && otherSet.All(Contains);
    }

    public bool IsSubsetOf(IEnumerable<TItem> other)
    {
        var otherSet = other.ToSet();
        return otherSet.Count >= _dictionary.Count && _dictionary.Values.All(otherSet.Contains);
    }

    public bool IsSupersetOf(IEnumerable<TItem> other)
        => other.All(Contains);

    public bool Overlaps(IEnumerable<TItem> other)
        => other.Any(Contains);

    public bool SetEquals(IEnumerable<TItem> other)
    {
        var otherSet = other.ToSet();
        return otherSet.Count == _dictionary.Count && otherSet.All(Contains);
    }

    public void SymmetricExceptWith(IEnumerable<TItem> other)
    {
        foreach (var item in other.Distinct()) {
            if (!Remove(item)) {
                Add(item);
            }
        }
    }

    public void UnionWith(IEnumerable<TItem> other)
    {
        foreach (var item in other) {
            Add(item);
        }
    }

    public bool Add(TItem item)
    {
        var key = KeySelector(item);
        if (_dictionary.TryAdd(key, item)) {
            return true;
        }

        if (!EqualityComparer<TItem>.Default.Equals(_dictionary[key], item)) {
            throw new ArgumentException($"This indexed set already contains an element with the same key");
        }

        return false;
    }

    public void Clear()
        => _dictionary.Clear();

    public bool Contains(TItem item)
        => _dictionary.TryGetValue(KeySelector(item), out var value) &&
           EqualityComparer<TItem>.Default.Equals(value, item);

    public void CopyTo(TItem[] array, int arrayIndex)
        => _dictionary.Values.CopyTo(array, arrayIndex);

    public bool Remove(TKey key)
        => _dictionary.Remove(key);

    public bool Remove(TKey key, [MaybeNullWhen(false)] out TItem value)
        => _dictionary.Remove(key, out value);

    public bool Remove(TItem item)
    {
        var key = KeySelector(item);
        if (!_dictionary.Remove(key, out var value)) {
            return false;
        }

        if (EqualityComparer<TItem>.Default.Equals(value, item)) {
            return true;
        }

        _dictionary.Add(key, value);
        return false;
    }

    public int Count
        => _dictionary.Count;

    public bool IsReadOnly
        => false;

    public bool ContainsKey(TKey key)
        => _dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TItem value)
        => _dictionary.TryGetValue(key, out value);

    public TItem this[TKey key]
        => _dictionary[key];

    public IEnumerable<TKey> Keys
        => _dictionary.Keys;

    IEnumerable<TItem> IReadOnlyDictionary<TKey, TItem>.Values
        => _dictionary.Values;
}
