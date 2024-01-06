namespace Parkour.Parsers;

public sealed class AlwaysParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Func<TOutput> _selector;
    private readonly string? _term;

    public AlwaysParser(Func<TOutput> selector, string? term = null)
    {
        _selector = selector;
        _term = term;
    }

    public override string? Term => _term;
    public override string DebugContent => _term != null ? _term : "<always>";

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        return new ParseResult<TOutput>(true, 0, _selector());
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        return new ScanResult(true, 0);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);
        return new SearchResult(true, 0, afterMissing);
    }
}