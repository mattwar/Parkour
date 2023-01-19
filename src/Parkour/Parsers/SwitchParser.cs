using System.Diagnostics.CodeAnalysis;

namespace Parkour.Parsers;

public sealed class SwitchParser<TInput, TOutput> : Parser<TInput, TOutput> where TInput: notnull
{
    private readonly MatchNode<TInput, Parser<TInput, TOutput>> _matchTree;

    public SwitchParser(
        Func<SwitchParserContext, SwitchParserContext<TInput, TOutput>> builder,
        EqualityComparer<TInput> comparer)
    {
        var list = builder(new SwitchParserContext()).MatchList;

        _matchTree = new MatchNode<TInput, Parser<TInput, TOutput>>(comparer);
        foreach (var match in list)
        {
            _matchTree.Add(match.Key.AsSpan(), match.Value);
        }
    }

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_matchTree.TryGetValue(input, out var parser, out remainingInput)
            && parser.Parse(input, out output, out remainingInput))
        {
            return true;
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_matchTree.TryGetValue(input, out var parser, out remainingInput)
            && parser.Scan(input, out remainingInput))
        {
            return true;
        }

        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        if (_matchTree.TryGetValue(input, out var parser, out remainingInput)
            && parser.Search(input, ref afterMissing, out remainingInput, fnCallback))
        {
            return true;
        }

        return false;
    }
}

public struct SwitchParserContext
{
    public SwitchParserContext() {}

    public SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(IEnumerable<TInput> items, Parser<TInput, TOutput> parser)
    {
        return new SwitchParserContext<TInput, TOutput>().Case(items, parser);
    }
}

public struct SwitchParserContext<TInput, TOutput>
{
    internal readonly List<KeyValuePair<TInput[], Parser<TInput, TOutput>>> MatchList;

    public SwitchParserContext()
    {
        this.MatchList = new List<KeyValuePair<TInput[], Parser<TInput, TOutput>>>();
    }

    public SwitchParserContext<TInput, TOutput> Case(IEnumerable<TInput> items, Parser<TInput, TOutput> parser)
    {
        MatchList.Add(new KeyValuePair<TInput[], Parser<TInput, TOutput>>(items.ToArray(), parser));
        return this;
    }
}

internal record struct Match<TKey, TValue>(TKey[] Items, TValue Value);

internal class MatchNode<TKey, TValue> where TKey : notnull
{
    private readonly EqualityComparer<TKey> _comparer;
    private TValue? _value;
    private Dictionary<TKey, MatchNode<TKey, TValue>>? _map;

    public MatchNode(EqualityComparer<TKey> comparer)
    {
        _comparer = comparer;
    }

    public void Add(ReadOnlySpan<TKey> items, TValue value)
    {
        var node = this;

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
                    node._map = new Dictionary<TKey, MatchNode<TKey, TValue>>(5, _comparer);

                if (node._map.TryGetValue(items[index], out var nextNode))
                {
                    node = nextNode;
                }
                else
                {
                    var newNode = new MatchNode<TKey, TValue>(_comparer);
                    node._map.Add(items[index], newNode);
                    node = newNode;
                }
            }
        }
    }

    public bool TryGetValue(ReadOnlySpan<TKey> input, out TValue value, out ReadOnlySpan<TKey> remainingInput)
    {
        var node = this;
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
            remainingInput = input[foundLength..];
            return true;
        }
        else
        {
            remainingInput = input;
            return false;
        }
    }
}
