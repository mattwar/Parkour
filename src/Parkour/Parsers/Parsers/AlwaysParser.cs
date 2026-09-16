namespace Parkour.Parsers;

/// <summary>
/// A parser that always produces an output without consuming any input.
/// /// </summary>
public sealed class AlwaysParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    /// <summary>
    /// The function that generates the output
    /// </summary>
    private readonly Func<TOutput> _selector;

    public override ImmutableList<object> Annotations { get; }

    public AlwaysParser(Func<TOutput> selector, ImmutableList<object>? annotations = null)
    {
        _selector = selector;
        Annotations = annotations ?? ImmutableList<object>.Empty;
    }

    public override string DebugContent => Term ?? "<always>";

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