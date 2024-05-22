namespace Parkour.Parsers;

public sealed class LimitMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly Parser<TInput> _limiter;
    private readonly MultiParser<TInput, TOutput> _parser;

    public LimitMultiParser(Parser<TInput> limiter, MultiParser<TInput, TOutput> parser)
    {
        _limiter = limiter;
        _parser = parser;
    }

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        var limitResult = _limiter.Scan(input);
        if (limitResult.Success)
        {
            var limitedInput = input.Slice(0, limitResult.Length);
            return _parser.ParseInto(limitedInput, outputList);
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
        else
        {
            return default;
        }
    }
}