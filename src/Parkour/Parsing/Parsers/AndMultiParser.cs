namespace Parkour.Parsing.Parsers;

public sealed class AndMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly IReadOnlyList<MultiParser<TInput, TOutput>> _parsers;

    public AndMultiParser(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers)
    {
        var allParsers = new List<MultiParser<TInput, TOutput>>();

        foreach (var parser in parsers)
        {
            if (parser is AndMultiParser<TInput, TOutput> andParser)
            {
                allParsers.AddRange(andParser._parsers);
            }
            else
            {
                allParsers.Add(parser.ToMultiParser());
            }
        }

        _parsers = allParsers;
    }

    public override string DebugContent => $"{string.Join(" ", _parsers.Select(p => $"{p.DebugContent}"))}";

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        // scan first so we don't put any partial into output
        if (!Scan(input).Success)
        {
            return default;
        }

        var remainingInput = input;

        foreach (var parser in _parsers)
        {
            var result = parser.ParseInto(remainingInput, outputList);
            if (!result.Success)
                return default;
            remainingInput = remainingInput.Slice(result.Length);
        }

        return new ParseIntoResult(true, input.Length - remainingInput.Length);
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