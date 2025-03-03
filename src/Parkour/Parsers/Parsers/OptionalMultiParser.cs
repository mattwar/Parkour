namespace Parkour.Parsers;

public sealed class OptionalMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly MultiParser<TInput, TOutput> _parser;
    private readonly Func<IEnumerable<TOutput>>? _fnMissing;

    public OptionalMultiParser(
        Parser<TInput, IReadOnlyList<TOutput>> parser, 
        Func<IEnumerable<TOutput>>? fnMissing = null,
        bool isRequired = false)
    {
        _parser = parser.ToMultiParser();
        _fnMissing = fnMissing;
        this.IsRequired = isRequired;
    }

    public override bool IsRequired { get; }

    public override string DebugContent => $"[{_parser.DebugContent}]";

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        var result = _parser.ParseInto(input, outputList);
        if (!result.Success
            && _fnMissing != null)
        {
            outputList.AddRange(_fnMissing());
        }

        return new ParseIntoResult(true, result.Length);
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

        if (!result.Success && this.IsRequired)
        {
            // failure after a required means it item was missing
            return new SearchResult(true, result.Length, AfterMissing: true);
        }

        return new SearchResult(true, result.Length, result.AfterMissing);
    }
}