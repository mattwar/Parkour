using Parkour;

namespace Parkour.Parsers;

public sealed class ForwardParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Func<Parser<TInput, TOutput>> _fnParser;
    private readonly string? _term;

    public ForwardParser(Func<Parser<TInput, TOutput>> fnParser, string? term = null)
    {
        _fnParser = fnParser;
        _term = term;
    }

    public override string? Term => _term ?? _fnParser().Term ?? "...";

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        return _fnParser().Parse(input);
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        return _fnParser().Scan(input);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);
        return _fnParser().Search(input, afterMissing, fnCallback);
    }
}