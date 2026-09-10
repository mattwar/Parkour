namespace Parkour;

/// <summary>
/// An optimized dictionary where the key is a sequence of elements, 
/// and is compared by matching the elements not the sequence's identity.
/// </summary>
internal class SequenceDictionary<TKeyElement, TValue> 
    where TKeyElement : notnull
{
    private readonly EqualityComparer<TKeyElement> _comparer;
    private Node _root;

    public SequenceDictionary(EqualityComparer<TKeyElement> comparer)
    {
        _comparer = comparer;
        _root = new Node();
    }

    public SequenceDictionary(
        EqualityComparer<TKeyElement> comparer, 
        IEnumerable<KeyValuePair<IEnumerable<TKeyElement>, TValue>> keyValuePairs)
        : this(comparer)
    {
        foreach (var pair in keyValuePairs)
        {
            Add(pair.Key.ToArray(), pair.Value);
        }
    }

    /// <summary>
    /// Adds the key and value pair to the dictionary.
    /// </summary>
    public void Add(ReadOnlySpan<TKeyElement> key, TValue value) =>
        Add(_root, key, value);

    /// <summary>
    /// Gets the value corresponding to the longest sequence match of the key.
    /// </summary>
    public bool TryGetBestValue(ReadOnlySpan<TKeyElement> buffer, out TValue value, out int length) =>
        TryGetValue(_root, buffer, out value, out length);

    /// <summary>
    /// Tries to get the value corresponding to the key (as exact match only).
    /// </summary>
    public bool TryGetValue(ReadOnlySpan<TKeyElement> key, out TValue value) =>
        TryGetBestValue(key, out value, out var length) && key.Length == length;

    /// <summary>
    /// An tree of nodes for optimized retrieval of values associated with sequence keys.
    /// The node tree and sequence are traversed in tandem.
    /// The next element is used to map to the next node.
    /// If next element does not map to a next node, the sequence has no value.
    /// If the last element maps to a node, the value at that node is the result.
    /// </summary>
    private class Node
    {
        /// <summary>
        /// The value if the sequence ends at this node.
        /// </summary>
        internal TValue? _value;

        /// <summary>
        /// The nodes that match the next element in the sequence
        /// </summary>
        internal Dictionary<TKeyElement, Node>? _map;
    }

    /// <summary>
    /// Adds the key value pair to the node.
    /// </summary>
    private void Add(Node node, ReadOnlySpan<TKeyElement> key, TValue value)
    {
        for (int index = 0; index <= key.Length; index++)
        {
            if (index == key.Length)
            {
                if (node._value == null)
                {
                    node._value = value;
                }
                else
                {
                    throw new InvalidCastException("duplicate case");
                }
            }
            else
            {
                if (node._map == null)
                    node._map = new Dictionary<TKeyElement, Node>(5, _comparer);

                if (node._map.TryGetValue(key[index], out var nextNode))
                {
                    node = nextNode;
                }
                else
                {
                    var newNode = new Node();
                    node._map.Add(key[index], newNode);
                    node = newNode;
                }
            }
        }
    }

    /// <summary>
    /// Gets the value corresponding to the key, starting from the specified node.
    /// </summary>
    private bool TryGetValue(Node node, ReadOnlySpan<TKeyElement> key, out TValue value, out int length)
    {
        var foundLength = 0;
        value = default!;

        var index = 0;
        while (true)
        {
            if (node._value != null)
            {
                value = node._value;
                foundLength = index;
            }

            if (index < key.Length
                && node._map != null
                && node._map.TryGetValue(key[index], out var nextNode))
            {
                node = nextNode;
                index++;
            }
            else
            {
                break;
            }
        }

        if (foundLength > 0)
        {
            length = foundLength;
            return true;
        }
        else
        {
            length = 0;
            return false;
        }
    }
}
