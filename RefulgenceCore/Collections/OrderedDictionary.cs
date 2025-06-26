using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Refulgence.Collections;

public sealed class OldOrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>,
    IList<KeyValuePair<TKey, TValue>> where TKey : notnull
{
    private readonly Dictionary<TKey, int>            _dictionary;
    private readonly List<KeyValuePair<TKey, TValue>> _list;

    public OldOrderedDictionary()
    {
        _list = [];
        _dictionary = [];
    }

    public OldOrderedDictionary(int capacity)
    {
        _list = new(capacity);
        _dictionary = new(capacity);
    }

    public OldOrderedDictionary(IEqualityComparer<TKey>? comparer)
    {
        _list = [];
        _dictionary = new(comparer);
    }

    public OldOrderedDictionary(int capacity, IEqualityComparer<TKey>? comparer)
    {
        _list = new(capacity);
        _dictionary = new(capacity, comparer);
    }

    public IEqualityComparer<TKey> Comparer
        => _dictionary.Comparer;

    public TValue this[TKey key]
    {
        get => _list[_dictionary[key]].Value;
        set
        {
            if (_dictionary.TryGetValue(key, out var index)) {
                _list[index] = new(key, value);
            } else {
                Add(key, value);
            }
        }
    }

    public ICollection<TKey> Keys
        => new KeyCollection(this);

    public ICollection<TValue> Values
        => new ValueCollection(this);

    public int Count
        => _list.Count;

    public bool IsReadOnly
        => false;

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, _list.Count);
        _list.Add(new(key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _dictionary.Add(item.Key, _list.Count);
        _list.Add(item);
    }

    public void Clear()
    {
        _list.Clear();
        _dictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _dictionary.TryGetValue(item.Key, out var index) &&
           EqualityComparer<TValue>.Default.Equals(_list[index].Value, item.Value);

    public bool ContainsKey(TKey key)
        => _dictionary.ContainsKey(key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => _list.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public bool Remove(TKey key)
    {
        if (!_dictionary.Remove(key, out var index)) {
            return false;
        }

        RemoveAt(index);

        return true;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = IndexOf(item);
        if (index < 0) {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_dictionary.TryGetValue(key, out var index)) {
            value = _list[index].Value;
            return true;
        }

        value = default;
        return false;
    }

    public KeyValuePair<TKey, TValue> this[int index]
    {
        get => _list[index];
        set
        {
            var previous = _list[index];
            if (_dictionary.Comparer.Equals(value.Key, previous.Key)) {
                // Replace the key, in case we have a weak equality comparer.
                _dictionary.Remove(previous.Key);
                try {
                    _dictionary.Add(value.Key, index);
                } catch {
                    _dictionary.Add(previous.Key, index);
                    throw;
                }
            } else {
                _dictionary.Add(value.Key, index);
                _dictionary.Remove(previous.Key);
            }

            _list[index] = value;
        }
    }

    public int IndexOf(KeyValuePair<TKey, TValue> item)
    {
        if (!_dictionary.TryGetValue(item.Key, out var index)) {
            return -1;
        }

        return EqualityComparer<TValue>.Default.Equals(item.Value, _list[index].Value) ? index : -1;
    }

    public void Insert(int index, KeyValuePair<TKey, TValue> item)
    {
        _dictionary.Add(item.Key, index);
        _list.Insert(index, item);
        for (var i = index + 1; i < _list.Count; ++i) {
            ++_dictionary[_list[i].Key];
        }
    }

    public void RemoveAt(int index)
    {
        _dictionary.Remove(_list[index].Key);
        _list.RemoveAt(index);
        for (var i = index; i < _list.Count; ++i) {
            --_dictionary[_list[i].Key];
        }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        => Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        => Values;

    public void EnsureCapacity(int capacity)
    {
        _list.EnsureCapacity(capacity);
        _dictionary.EnsureCapacity(capacity);
    }

    private abstract class KeyOrValueCollection<T>(OldOrderedDictionary<TKey, TValue> dictionary) : ICollection<T>
    {
        protected readonly OldOrderedDictionary<TKey, TValue> _dictionary = dictionary;

        protected abstract Func<KeyValuePair<TKey, TValue>, T> Selector { get; }

        public int Count
            => _dictionary.Count;

        public bool IsReadOnly
            => true;

        public virtual bool Contains(T item)
            => AsEnumerable().Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => AsEnumerable().CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator()
            => AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(T item)
            => throw new NotSupportedException();

        public void Clear()
            => throw new NotSupportedException();

        public bool Remove(T item)
            => throw new NotSupportedException();

        private IEnumerable<T> AsEnumerable()
            => _dictionary._list.Select(Selector);
    }

    private sealed class KeyCollection(OldOrderedDictionary<TKey, TValue> dictionary)
        : KeyOrValueCollection<TKey>(dictionary)
    {
        protected override Func<KeyValuePair<TKey, TValue>, TKey> Selector
            => pair => pair.Key;

        public override bool Contains(TKey item)
            => _dictionary.ContainsKey(item);
    }

    private sealed class ValueCollection(OldOrderedDictionary<TKey, TValue> dictionary)
        : KeyOrValueCollection<TValue>(dictionary)
    {
        protected override Func<KeyValuePair<TKey, TValue>, TValue> Selector
            => pair => pair.Value;
    }
}
