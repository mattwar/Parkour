using Parkour;

namespace Parkour.Parsers;

public sealed class ConvertParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput> _parser;
    private readonly Converter<TInput, TOutput> _converter;

    public override string? Term { get; }

    public ConvertParser(Parser<TInput> parser, Converter<TInput, TOutput> converter, string? term = null)
    {
        _parser = parser;
        _converter = converter;
        this.Term = term;
    }

    public override string DebugContent => _parser.DebugContent;

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        var scanResult = _parser.Scan(input);
        if (scanResult.Success)
        {
            var output = _converter(input.Slice(0, scanResult.Length));
            return new ParseResult<TOutput>(true, scanResult.Length, output);
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