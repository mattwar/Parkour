namespace Parkour.Parsers;

/// <summary>
/// A parser that produces the output of the nested parser unless the condition is also met in the input following the parse.
/// </summary>
public sealed class UnlessMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    /// <summary>
    /// The parser that produces the output
    /// </summary>
    private readonly MultiParser<TInput, TOutput> _parser;

    /// <summary>
    /// the condition that is tested against the input after the parser would have consumed
    /// </summary>
    private readonly Parser<TInput> _condition;

    public UnlessMultiParser(MultiParser<TInput, TOutput> parser, Parser<TInput> condition)
    {
        _parser = parser;
        _condition = condition;
    }

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        var scanResult = Scan(input);
        if (scanResult.Success)
        {
             return _parser.ParseInto(input, outputList);
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