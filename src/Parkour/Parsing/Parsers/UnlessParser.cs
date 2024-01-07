namespace Parkour.Parsing.Parsers;

public sealed class UnlessParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser;
    private readonly Parser<TInput> _condition;

    public UnlessParser(Parser<TInput, TOutput> parser, Parser<TInput> condition)
    {
        _parser = parser;
        _condition = condition;
    }

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        if (Scan(input).Success)
        {
            return _parser.Parse(input);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var scanResult = _parser.Scan(input);
        if (scanResult.Success)
        {
            var conditionInput = input.Slice(scanResult.Length);
            var conditionResult = _condition.Scan(conditionInput);
            if (!conditionResult.Success)
                return scanResult;
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var scanResult = Scan(input);
        if (scanResult.Success)
        {
            return _parser.Search(input, afterMissing, fnCallback);
        }

        return default;
    }
}