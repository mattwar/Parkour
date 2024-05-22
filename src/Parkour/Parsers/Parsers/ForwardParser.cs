
namespace Parkour.Parsers;

public sealed class ForwardParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Func<Parser<TInput, TOutput>> _fnParser;

    public override ImmutableList<object> Annotations { get; }

    public ForwardParser(Func<Parser<TInput, TOutput>> fnParser, ImmutableList<object>? annotations = null)
    {
        _fnParser = fnParser;
        Annotations = annotations ?? ImmutableList<object>.Empty;
    }

    public override string? Term => base.Term ?? _fnParser().Term;

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