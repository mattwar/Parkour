namespace Parkour.Parsers;

/// <summary>
/// A parser that will parse only up to the input limit set by the amount of input theoretically consumed by the limiter.
/// </summary>
public sealed class LimitParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    /// <summary>
    /// The parser that is scanned to determine the number of input items to limit the full parser to.
    /// </summary>
    private readonly Parser<TInput> _limiter;

    /// <summary>
    /// The full parser, parsed with limited input.
    /// </summary>
    private readonly Parser<TInput, TOutput> _parser;

    public LimitParser(Parser<TInput> limiter, Parser<TInput, TOutput> parser)
    {
        _limiter = limiter;
        _parser = parser;
    }

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        var limitResult = _limiter.Scan(input);
        if (limitResult.Success)
        {
            var limitedInput = input.Slice(0, limitResult.Length);
            return _parser.Parse(limitedInput);

        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var limitResult = _limiter.Scan(input);
        if (limitResult.Success)
        {
            var limitedInput = input.Slice(0, limitResult.Length);
            return _parser.Scan(limitedInput);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var limitResult = _limiter.Scan(input);
        if (limitResult.Success)
        {
            var limitedInput = input.Slice(0, limitResult.Length);
            return _parser.Search(limitedInput, afterMissing, fnCallback);
        }

        return default;
    }
}