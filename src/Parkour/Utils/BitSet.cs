using System.Collections;

namespace Parkour;

/// <summary>
/// A set of values stored using bits.
/// The maximum number of unique values for all instances of the same kind of set is 64.
/// Do not rely on the order of values in this set to remain the same across uses,
/// it is dependent on the order the values are introduced to any instance of a set.
/// </summary>
public readonly struct BitSet<TValue>
    : IEquatable<BitSet<TValue>>,
      IEnumerable<TValue>,
      IEnumerable
    where TValue : class
{
    /// <summary>
    /// The actual bits that are set in this bit set.
    /// </summary>
    private readonly ulong _bits;

    private BitSet(ulong bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// The number of bit indices that have been assigned so far
    /// for all instances of this type.
    /// </summary>
    private static int _indexCount;

    /// <summary>
    /// Maps indices to <see cref="TValue"/> values, 
    /// for all instances of this type.
    /// </summary>
    private static ImmutableDictionary<int, TValue> _indexToValueMap =
        ImmutableDictionary<int, TValue>.Empty;

    /// <summary>
    /// Maps values to assigned indices,
    /// for all instances of this type.
    /// </summary>
    private static ImmutableDictionary<TValue, int> _valueToIndexMap =
        ImmutableDictionary<TValue, int>.Empty;

    /// <summary>
    /// Gets the bit mask for a value that can be used to set or test the bit
    /// in the <see cref="_bits"/> field.
    /// </summary>
    private static ulong GetBitMask(TValue value)
    {
        return 1UL << GetBitIndex(value);
    }

    /// <summary>
    /// Gets the assigned bit index for a value, allocating a new index if needed.
    /// </summary>
    private static int GetBitIndex(TValue value)
    {
        if (!_valueToIndexMap.TryGetValue(value, out var index))
        {
            // allocate bit for value
            index = ImmutableInterlocked.GetOrAdd(ref _valueToIndexMap, value, _value => _indexCount++);
            ImmutableInterlocked.GetOrAdd(ref _indexToValueMap, index, _index => value);
        }
        return index;
    }

    /// <summary>
    /// Represents no bits are set.
    /// </summary>
    public static readonly BitSet<TValue> Empty = default;

    /// <summary>
    /// Returns true if this set of bits contains any of the bits in the other set.
    /// </summary>
    public bool Contains(BitSet<TValue> bitset) =>
        (_bits & bitset._bits) != 0;

    /// <summary>
    /// Returns true if this set of bits contains the specified bit
    /// </summary>
    public bool Contains(TValue value) =>
        (_bits & GetBitMask(value)) != 0;

    /// <summary>
    /// Adds the bits from the same family together.
    /// </summary>
    public BitSet<TValue> Add(BitSet<TValue> bitset) =>
        new BitSet<TValue>(_bits | bitset._bits);

    /// <summary>
    /// Adds the bits from the same family together.
    /// </summary>
    public readonly BitSet<TValue> Add(TValue value) =>
        new BitSet<TValue>(_bits | GetBitMask(value));

    /// <summary>
    /// Returns the bitset with the bits of the same family removed.
    /// </summary>
    public BitSet<TValue> Remove(BitSet<TValue> bitset) =>
        new BitSet<TValue>(_bits & ~bitset._bits);

    /// <summary>
    /// Returns the bitset with the bit from the same family removed.
    /// </summary>
    public BitSet<TValue> Remove(TValue value) =>
        new BitSet<TValue>(_bits & ~GetBitMask(value));

    /// <summary>
    /// The set bits in common between the two sets.
    /// </summary>
    public BitSet<TValue> Intersect(BitSet<TValue> bitset) =>
        new BitSet<TValue>(_bits & bitset._bits);

    /// <summary>
    /// Enumerates the bits in the set
    /// </summary>
    public IEnumerator<TValue> GetEnumerator()
    {
        for (int i = 0; i < _indexCount; i++)
        {
            var bitMask = 1UL << i;
            if ((_bits & bitMask) != 0
                && _indexToValueMap.TryGetValue(i, out var bit))
            {
                yield return bit;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => 
        this.GetEnumerator();

    /// <summary>
    /// True if the sets of bits are the same.
    /// </summary>
    public bool Equals(BitSet<TValue> other) =>
        _bits == other._bits;

    /// <summary>
    /// True if the sets of bits are the same.
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is BitSet<TValue> bs && Equals(bs);

    public override int GetHashCode() =>
        unchecked((int)_bits);

    public override string ToString() =>
        // return ordered by modifier name for test stability
        string.Join(" | ", this.Select(b => b.ToString()).OrderBy(s => s));


    public static bool operator ==(BitSet<TValue> a, BitSet<TValue> b) =>
        a.Equals(b);

    public static bool operator !=(BitSet<TValue> a, BitSet<TValue> b) =>
        !a.Equals(b);

    public static implicit operator BitSet<TValue>(TValue bit) =>
        new BitSet<TValue>(GetBitMask(bit));

    public static BitSet<TValue> operator |(BitSet<TValue> a, BitSet<TValue> b) =>
        a.Add(b);

    public static BitSet<TValue> operator |(BitSet<TValue> a, TValue b) =>
    a.Add(b);

    public static BitSet<TValue> operator +(BitSet<TValue> a, BitSet<TValue> b) =>
        a.Add(b);

    public static BitSet<TValue> operator +(BitSet<TValue> a, TValue b) =>
        a.Add(b);

    public static BitSet<TValue> operator -(BitSet<TValue> a, BitSet<TValue> b) =>
        a.Remove(b);

    public static BitSet<TValue> operator -(BitSet<TValue> a, TValue b) =>
        a.Remove(b);
}