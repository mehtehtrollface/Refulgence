using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Refulgence.Collections;

public sealed class IndexedList<TKey, TItem> : IReadOnlyDictionary<TKey, TItem>, IList<TItem> where TKey : notnull
{
    private readonly Dictionary<TKey, int> _dictionary;
    private readonly List<TItem>           _list;

    public IndexedList(Func<TItem, TKey> keySelector)
    {
        KeySelector = keySelector;
        _list = [];
        _dictionary = [];
    }

    public IndexedList(int capacity, Func<TItem, TKey> keySelector)
    {
        KeySelector = keySelector;
        _list = new(capacity);
        _dictionary = new(capacity);
    }

    public IndexedList(Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        KeySelector = keySelector;
        _list = [];
        _dictionary = new(comparer);
    }

    public IndexedList(int capacity, Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        KeySelector = keySelector;
        _list = new(capacity);
        _dictionary = new(capacity, comparer);
    }

    public Func<TItem, TKey> KeySelector { get; }

    public IEqualityComparer<TKey> Comparer
        => _dictionary.Comparer;

    public TItem this[int index]
    {
        get => _list[index];
        set
        {
            var key = KeySelector(value);
            var previousKey = KeySelector(_list[index]);
            if (_dictionary.Comparer.Equals(key, previousKey)) {
                // Replace the key, in case we have a weak equality comparer.
                _dictionary.Remove(previousKey);
                try {
                    _dictionary.Add(key, index);
                } catch {
                    _dictionary.Add(previousKey, index);
                    throw;
                }
            } else {
                _dictionary.Add(key, index);
                _dictionary.Remove(previousKey);
            }

            _list[index] = value;
        }
    }

    public bool IsReadOnly
        => false;

    public void Add(TItem item)
    {
        _dictionary.Add(KeySelector(item), _list.Count);
        _list.Add(item);
    }

    public void Clear()
    {
        _list.Clear();
        _dictionary.Clear();
    }

    public bool Contains(TItem item)
        => _dictionary.TryGetValue(KeySelector(item), out var index) &&
           EqualityComparer<TItem>.Default.Equals(_list[index], item);

    public void CopyTo(TItem[] array, int arrayIndex)
        => _list.CopyTo(array, arrayIndex);

    public IEnumerator<TItem> GetEnumerator()
        => _list.GetEnumerator();

    public bool Remove(TItem item)
        => Remove(KeySelector(item));

    public int IndexOfKey(TKey key)
        => _dictionary.GetValueOrDefault(key, -1);

    public int IndexOf(TItem item)
    {
        if (!_dictionary.TryGetValue(KeySelector(item), out var index)) {
            return -1;
        }

        return EqualityComparer<TItem>.Default.Equals(item, _list[index]) ? index : -1;
    }

    public void Insert(int index, TItem item)
    {
        _dictionary.Add(KeySelector(item), index);
        _list.Insert(index, item);
        for (var i = index + 1; i < _list.Count; ++i) {
            ++_dictionary[KeySelector(_list[i])];
        }
    }

    public void RemoveAt(int index)
    {
        _dictionary.Remove(KeySelector(_list[index]));
        _list.RemoveAt(index);
        for (var i = index; i < _list.Count; ++i) {
            --_dictionary[KeySelector(_list[i])];
        }
    }

    public TItem this[TKey key]
    {
        get => _list[_dictionary[key]];
        set
        {
            var valueKey = KeySelector(value);
            if (!_dictionary.Comparer.Equals(valueKey, key)) {
                throw new ArgumentException("The assigned value doesn't match the supplied key.");
            }

            if (_dictionary.TryGetValue(key, out var index)) {
                var previousKey = KeySelector(_list[index]);
                // Replace the key, in case we have a weak equality comparer.
                _dictionary.Remove(previousKey);
                try {
                    _dictionary.Add(key, index);
                } catch {
                    _dictionary.Add(previousKey, index);
                    throw;
                }

                _list[index] = value;
            } else {
                Add(value);
            }
        }
    }

    public IEnumerable<TKey> Keys
        => _list.Select(KeySelector);

    IEnumerable<TItem> IReadOnlyDictionary<TKey, TItem>.Values
        => _list.AsReadOnly();

    public int Count
        => _list.Count;

    public bool ContainsKey(TKey key)
        => _dictionary.ContainsKey(key);

    IEnumerator<KeyValuePair<TKey, TItem>> IEnumerable<KeyValuePair<TKey, TItem>>.GetEnumerator()
        => _list.Select(item => new KeyValuePair<TKey, TItem>(KeySelector(item), item)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TItem value)
    {
        if (_dictionary.TryGetValue(key, out var index)) {
            value = _list[index];
            return true;
        }

        value = default;
        return false;
    }

    public void EnsureCapacity(int capacity)
    {
        _list.EnsureCapacity(capacity);
        _dictionary.EnsureCapacity(capacity);
    }

    public bool Remove(TKey key)
    {
        if (!_dictionary.Remove(key, out var index)) {
            return false;
        }

        RemoveAt(index);

        return true;
    }
}
