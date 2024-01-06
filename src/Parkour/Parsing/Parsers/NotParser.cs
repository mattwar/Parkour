namespace Parkour.Parsers;

public sealed class NotParser<TInput> : Parser<TInput, TInput>
{
    private readonly Parser<TInput> _parser;

    public NotParser(Parser<TInput> parser)
    {
        _parser = parser;
    }

    public override string DebugContent => $"not({_parser.DebugContent})";

    public override ParseResult<TInput> Parse(ReadOnlySpan<TInput> input)
    {
        var scanResult = _parser.Scan(input);
        if (!scanResult.Success && input.Length > 0)
        {
            var output = input[0];
            return new ParseResult<TInput>(true, 1, input[0]);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var result = _parser.Scan(input);
        if (!result.Success && input.Length > 0)
        {
            return new ScanResult(true, 1);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var scanResult = _parser.Scan(input);
        if (!scanResult.Success && input.Length > 0)
        {
            return new SearchResult(true, 1, afterMissing);
        }

        return default;
    }
}