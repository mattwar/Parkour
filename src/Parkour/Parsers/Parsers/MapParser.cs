
namespace Parkour.Parsers;

/// <summary>
/// A parser that maps the output of another parser.
/// This is an single-parser optimized version of <see cref="SequenceParser"/>.
/// </summary>
public sealed class MapParser<TInput, TOutput1, TOutput2> : Parser<TInput, TOutput2>
{
    private readonly Parser<TInput, TOutput1> _parser;
    private readonly Func<TOutput1, TOutput2> _fnMap;

    public override ImmutableList<object> Annotations { get; }

    public MapParser(
        Parser<TInput, TOutput1> parser, 
        Func<TOutput1, TOutput2> fnMap, 
        ImmutableList<object>? annotations = null)
    {
        _parser = parser;
        _fnMap = fnMap;
        Annotations = annotations ?? ImmutableList<object>.Empty;
    }

    public override string DebugContent => Term ?? _parser.DebugContent;

    public override ParseResult<TOutput2> Parse(ReadOnlySpan<TInput> input)
    {
        var result = _parser.Parse(input);
        if (result.Success)
        {
            var output = _fnMap(result.Output);
            return new ParseResult<TOutput2>(true, result.Length, output);
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