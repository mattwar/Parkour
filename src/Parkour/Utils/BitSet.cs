using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Parkour;

/// <summary>
/// An extensible set of bits where each bit is a singleton instance of a class.
/// The maximum number of unique singetons for this set is 64.
/// Do not rely on the order of bits in this set.
/// </summary>
public readonly struct BitSet<TBit>
    : IEquatable<BitSet<TBit>>,
      IEnumerable<TBit>,
      IEnumerable
    where TBit : class
{
    /// <summary>
    /// the actual bits that are set
    /// </summary>
    private readonly ulong _bits;

    private BitSet(ulong bits)
    {
        _bits = bits;
    }

    private static int _nextIndex;

    private static ImmutableDictionary<int, TBit> _indexToBitMap =
        ImmutableDictionary<int, TBit>.Empty;

    private static ImmutableDictionary<TBit, int> _bitToIndexMap =
        ImmutableDictionary<TBit, int>.Empty;

    private static ulong GetMask(TBit bit)
    {
        if (!_bitToIndexMap.TryGetValue(bit, out var index))
        {
            // allocate index to new bit
            index = ImmutableInterlocked.GetOrAdd(ref _bitToIndexMap, bit, _bit => _nextIndex++);
            ImmutableInterlocked.GetOrAdd(ref _indexToBitMap, index, _index => bit);
        }

        return 1UL << index;
    }

    /// <summary>
    /// Represents no bits are set.
    /// </summary>
    public static readonly BitSet<TBit> Empty = default;

    /// <summary>
    /// Returns true if this set of bits contains any of the bits in the other set.
    /// </summary>
    public bool Contains(BitSet<TBit> bitset) =>
        (_bits & bitset._bits) != 0;

    /// <summary>
    /// Returns true if this set of bits contains the specified bit
    /// </summary>
    public bool Contains(TBit bit) =>
        (_bits & GetMask(bit)) != 0;

    /// <summary>
    /// Adds the bits from the same family together.
    /// </summary>
    public BitSet<TBit> Add(BitSet<TBit> bitset) =>
        new BitSet<TBit>(_bits | bitset._bits);

    /// <summary>
    /// Adds the bits from the same family together.
    /// </summary>
    public readonly BitSet<TBit> Add(TBit bit) =>
        new BitSet<TBit>(_bits | GetMask(bit));

    /// <summary>
    /// Returns the bitset with the bits of the same family removed.
    /// </summary>
    public BitSet<TBit> Remove(BitSet<TBit> bitset) =>
        new BitSet<TBit>(_bits & ~bitset._bits);

    /// <summary>
    /// Returns the bitset with the bit from the same family removed.
    /// </summary>
    public BitSet<TBit> Remove(TBit bit) =>
        new BitSet<TBit>(_bits & ~GetMask(bit));

    /// <summary>
    /// The set bits in common between the two sets.
    /// </summary>
    public BitSet<TBit> Intersect(BitSet<TBit> bitset) =>
        new BitSet<TBit>(_bits & bitset._bits);

    /// <summary>
    /// Enumerates the bits in the set
    /// </summary>
    public IEnumerator<TBit> GetEnumerator()
    {
        for (int i = 0; i < _nextIndex; i++)
        {
            var bitMask = 1UL << i;
            if ((_bits & bitMask) != 0
                && _indexToBitMap.TryGetValue(i, out var bit))
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
    public bool Equals(BitSet<TBit> other) =>
        _bits == other._bits;

    /// <summary>
    /// True if the sets of bits are the same.
    /// </summary>
    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is BitSet<TBit> bs && Equals(bs);

    public override int GetHashCode() =>
        unchecked((int)_bits);

    public override string ToString() =>
        // return ordered by modifier name for test stability
        string.Join(" | ", this.Select(b => b.ToString()).OrderBy(s => s));


    public static bool operator ==(BitSet<TBit> a, BitSet<TBit> b) =>
        a.Equals(b);

    public static bool operator !=(BitSet<TBit> a, BitSet<TBit> b) =>
        !a.Equals(b);

    public static implicit operator BitSet<TBit>(TBit bit) =>
        new BitSet<TBit>(GetMask(bit));

    public static BitSet<TBit> operator |(BitSet<TBit> a, BitSet<TBit> b) =>
        a.Add(b);

    public static BitSet<TBit> operator |(BitSet<TBit> a, TBit b) =>
    a.Add(b);

    public static BitSet<TBit> operator +(BitSet<TBit> a, BitSet<TBit> b) =>
        a.Add(b);

    public static BitSet<TBit> operator +(BitSet<TBit> a, TBit b) =>
        a.Add(b);

    public static BitSet<TBit> operator -(BitSet<TBit> a, BitSet<TBit> b) =>
        a.Remove(b);

    public static BitSet<TBit> operator -(BitSet<TBit> a, TBit b) =>
        a.Remove(b);
}