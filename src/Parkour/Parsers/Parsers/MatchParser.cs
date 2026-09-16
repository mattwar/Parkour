
namespace Parkour.Parsers;

/// <summary>
/// A parser that directly converts a number of matched and consumed input items into an output.
/// </summary>
public sealed class MatchParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    /// <summary>
    /// The function that determines how many input items to consume/convert
    /// </summary>
    private readonly Matcher<TInput> _matcher;

    /// <summary>
    /// A function that converts a sequence of item input into an output value.
    /// </summary>
    private readonly Converter<TInput, TOutput> _converter;

    public override ImmutableList<object> Annotations { get; }

    public MatchParser(Matcher<TInput> matcher, Converter<TInput, TOutput> converter, ImmutableList<object>? annotations = null)
    {
        _matcher = matcher;
        _converter = converter;
        Annotations = annotations ?? ImmutableList<object>.Empty;
    }

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        var length = _matcher(input);
        if (length > 0)
        {
            var output = _converter(input.Slice(0, length));
            return new ParseResult<TOutput>(true, length, output);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var length = _matcher(input);
        if (length > 0)
        {
            return new ScanResult(true, length);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var result = Scan(input);
        if (result.Success)
        {
            return new SearchResult(true, result.Length, false);
        }

        return default;
    }
}