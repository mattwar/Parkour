namespace Parkour.Parsers;

/// <summary>
/// A parser that maps the output of all parsers, if all parsers succeed in succession.
/// </summary>
public sealed class SequenceParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly ImmutableList<Parser<TInput>> _parsers;
    private readonly Func<IReadOnlyList<object>, TOutput> _fnMapper;
    public override ImmutableList<object> Annotations { get;}

    public SequenceParser(
        IEnumerable<Parser<TInput>> parsers, 
        Func<IReadOnlyList<object>, TOutput> fnMapper, 
        ImmutableList<object>? annotations = null)
    {
        _parsers = parsers.ToImmutableList();
        _fnMapper = fnMapper;
        Annotations = annotations ?? ImmutableList<object>.Empty;
    }

    public override string DebugContent => $"{Term ?? string.Join(" ", _parsers.Select(p => $"{p.DebugContent}"))}";

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        List<object>? list = null;

        var remainingInput = input;
        foreach (var parser in _parsers)
        {
            var result = parser.ParseAsObject(remainingInput);
            if (!result.Success)
                return default;

            remainingInput = remainingInput.Slice(result.Length);

            if (list == null)
                list = new List<object>();

            list.Add(result.Output);
        }

        if (list != null)
        {
            var output = _fnMapper(list);
            return new ParseResult<TOutput>(true, input.Length - remainingInput.Length, output);
        }
        else
        {
            return default;
        }
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var remainingInput = input;

        foreach (var parser in _parsers)
        {
            var result = parser.Scan(remainingInput);
            if (!result.Success)
                return default;
            remainingInput = remainingInput.Slice(result.Length);
        }

        return new ScanResult(true, input.Length - remainingInput.Length);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var remainingInput = input;

        foreach (var parser in _parsers)
        {
            var result = parser.Search(remainingInput, afterMissing, fnCallback);
            if (!result.Success)
                return default;
            remainingInput = remainingInput.Slice(result.Length);
            afterMissing = result.AfterMissing;
        }

        return new SearchResult(true, input.Length - remainingInput.Length, afterMissing);
    }
}