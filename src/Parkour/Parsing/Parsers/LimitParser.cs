namespace Parkour.Parsers;

public sealed class LimitParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput> _limiter;
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