namespace Parkour.Parsing.Parsers;

public sealed class OptionalParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser;
    private readonly bool _isRequired;
    private readonly Func<TOutput>? _fnMissing;

    public OptionalParser(Parser<TInput, TOutput> parser, Func<TOutput>? fnMissing = null, bool isRequired = false)
    {
        _parser = parser;
        _fnMissing = fnMissing;
        _isRequired = isRequired;
    }

    public override bool IsRequired => _isRequired;

    public override string DebugContent => $"[{_parser.DebugContent}]";

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        var result = _parser.Parse(input);
        if (!result.Success
            && _fnMissing != null)
        {
            var output = _fnMissing();
            return new ParseResult<TOutput>(true, result.Length, output);
        }

        return new ParseResult<TOutput>(true, result.Length, result.Output);
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var result = _parser.Scan(input);
        return new ScanResult(true, result.Length);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var result = _parser.Search(input, afterMissing, fnCallback);
        if (!result.Success && _isRequired)
        {
            return new SearchResult(true, result.Length, AfterMissing: true);
        }

        return new SearchResult(true, result.Length, result.AfterMissing);
    }
}