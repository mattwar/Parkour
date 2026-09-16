namespace Parkour.Parsers;

/// <summary>
/// A <see cref="MultiParser{TInput, TOutput}"/> that produces the output of the full <see cref="MultiParser{TInput, TOutput}"/> if the condition parser to scan.
/// This parser is used to do arbitrary look-ahead before committing to a full parse.
/// </summary>
public class IfMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    /// <summary>
    /// Tne parser that must succeed to scan to trigger the parsing of the full parser.
    /// </summary>
    private readonly Parser<TInput> _condition;

    /// <summary>
    /// The full parser.
    /// </summary>
    private readonly MultiParser<TInput, TOutput> _parser;

    public IfMultiParser(Parser<TInput> condition, MultiParser<TInput, TOutput> parser)
    {
        _condition = condition;
        _parser = parser;
    }

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        if (_condition.Scan(input).Success)
        {
            return _parser.ParseInto(input, outputList);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        if (_condition.Scan(input).Success)
        {
            return _parser.Scan(input);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        if (_condition.Search(input, afterMissing, fnCallback).Success)
        {
            return _parser.Search(input, afterMissing, fnCallback);
        }

        return default;
    }
}
