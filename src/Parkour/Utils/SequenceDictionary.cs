namespace Parkour;

/// <summary>
/// An optimized dictionary that maps sequences of key elements to values.
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
    /// Tries to get the value associated with the key that best matches the items in the buffer.
    /// </summary>
    public bool TryGetBestValue(ReadOnlySpan<TKeyElement> buffer, out TValue value, out int length) =>
        TryGetValue(_root, buffer, out value, out length);

    /// <summary>
    /// Tries to get the value associated with the key (as exact match)
    /// </summary>
    public bool TryGetValue(ReadOnlySpan<TKeyElement> key, out TValue value) =>
        TryGetBestValue(key, out value, out var length) && key.Length == length;

    private class Node
    {
        internal TValue? _value;
        internal Dictionary<TKeyElement, Node>? _map;
    }

    private void Add(Node node, ReadOnlySpan<TKeyElement> items, TValue value)
    {
        for (int index = 0; index <= items.Length; index++)
        {
            if (index == items.Length)
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

                if (node._map.TryGetValue(items[index], out var nextNode))
                {
                    node = nextNode;
                }
                else
                {
                    var newNode = new Node();
                    node._map.Add(items[index], newNode);
                    node = newNode;
                }
            }
        }
    }

    private bool TryGetValue(Node node, ReadOnlySpan<TKeyElement> input, out TValue value, out int length)
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

            if (index < input.Length
                && node._map != null
                && node._map.TryGetValue(input[index], out var nextNode))
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
