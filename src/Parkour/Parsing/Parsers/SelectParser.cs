namespace Parkour.Parsing.Parsers;

public sealed class SelectParser<TInput, TOutput, TOutput2> : Parser<TInput, TOutput2>
{
    private readonly Parser<TInput, TOutput> _parser;
    private readonly Func<TOutput, TOutput2> _selector;
    private readonly string? _term;

    public SelectParser(Parser<TInput, TOutput> parser, Func<TOutput, TOutput2> selector, string? term = null)
    {
        _parser = parser;
        _selector = selector;
        _term = term;
    }

    public override string? Term => _term;
    public override string DebugContent => _term != null ? _term : _parser.DebugContent;

    public override ParseResult<TOutput2> Parse(ReadOnlySpan<TInput> input)
    {
        var result = _parser.Parse(input);
        if (result.Success)
        {
            var output = _selector(result.Output);
            return new ParseResult<TOutput2>(true, result.Length, output);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        return _parser.Scan(input);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);
        return _parser.Search(input, afterMissing, fnCallback);
    }
}