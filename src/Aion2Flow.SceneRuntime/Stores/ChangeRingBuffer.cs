namespace Cloris.Aion2Flow.SceneRuntime.Stores;

/// <summary>
/// Change log with an optional fixed capacity. When <c>maxCapacity</c> is positive the buffer
/// never grows beyond it and overwrites the oldest entry on overflow (no allocation on Add once
/// the backing array exists). When zero it grows like a list (legacy behavior).
/// </summary>
public sealed class ChangeRingBuffer<T>
{
    private static readonly T[] Empty = [];

    private readonly int _maxCapacity;
    private T[] _items;
    private int _head;
    private int _count;

    public ChangeRingBuffer(int initialCapacity = 0, int maxCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCapacity);
        _maxCapacity = maxCapacity;
        if (maxCapacity > 0 && initialCapacity > maxCapacity)
            initialCapacity = maxCapacity;
        _items = initialCapacity > 0 ? new T[initialCapacity] : Empty;
    }

    public int Count => _count;
    public int Capacity => _items.Length;
    public int MaxCapacity => _maxCapacity;

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _items[(_head + index) % _items.Length];
        }
    }

    public void Add(in T item)
    {
        if (_count == _items.Length)
        {
            if (_maxCapacity > 0 && _count >= _maxCapacity)
            {
                _items[_head] = item;
                _head = (_head + 1) % _items.Length;
                return;
            }
            Grow(_count + 1);
        }

        _items[(_head + _count) % _items.Length] = item;
        _count++;
    }

    public void EnsureCapacity(int capacity)
    {
        if (_maxCapacity > 0 && capacity > _maxCapacity)
            capacity = _maxCapacity;
        if (capacity > _items.Length)
            Resize(capacity);
    }

    public void Clear()
    {
        Array.Clear(_items);
        _head = 0;
        _count = 0;
    }

    public T[] GetRange(int start, int count)
    {
        if (start < 0 || count < 0 || start > _count - count)
            throw new ArgumentOutOfRangeException(nameof(start));
        var result = new T[count];
        for (var i = 0; i < count; i++)
            result[i] = _items[(_head + start + i) % _items.Length];
        return result;
    }

    private void Grow(int required)
    {
        var next = Math.Max(required, _items.Length == 0 ? 4 : _items.Length * 2);
        if (_maxCapacity > 0 && next > _maxCapacity)
            next = _maxCapacity;
        Resize(next);
    }

    private void Resize(int capacity)
    {
        var next = new T[capacity];
        for (var i = 0; i < _count; i++)
            next[i] = _items[(_head + i) % _items.Length];
        _items = next;
        _head = 0;
    }
}
