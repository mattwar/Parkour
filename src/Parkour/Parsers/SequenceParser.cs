namespace Parkour;

public sealed class SequenceParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly IReadOnlyList<Parser<TInput>> _parsers;
    private readonly Func<IReadOnlyList<object>, TOutput> _fnMapper;
    private readonly string? _term;

    public SequenceParser(Parser<TInput>[] parsers, Func<IReadOnlyList<object>, TOutput> fnMapper, string? term = null)
    {
        _parsers = parsers;
        _fnMapper = fnMapper;
        _term = term;
    }

    public override string DebugContent => $"{_term ?? string.Join(" ", _parsers.Select(p => $"{p.DebugContent}"))}";

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        List<object>? list = null;

        remainingInput = input;
        foreach (var parser in _parsers)
        {
            if (!parser.ParseAsObject(remainingInput, out var items, out remainingInput))
            {
                remainingInput = input;
                output = default!;
                return false;
            }

            if (list == null)
                list = new List<object>();

            list.Add(items);
        }

        if (list != null)
        {
            output = _fnMapper(list);
            return true;
        }
        else
        {
            remainingInput = input;
            output = default!;
            return false;
        }
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        remainingInput = input;
        foreach (var parser in _parsers)
        {
            if (!parser.Scan(remainingInput, out remainingInput))
            {
                remainingInput = input;
                return false;
            }
        }

        return true;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        remainingInput = input;

        foreach (var parser in _parsers)
        {
            if (!parser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
            {
                remainingInput = input;
                afterMissing = initialAfterMissing;
                return false;
            }
        }

        return true;
    }
}