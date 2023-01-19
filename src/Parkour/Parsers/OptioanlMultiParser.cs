namespace Parkour.Parsers;

public sealed class OptionalMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly MultiParser<TInput, TOutput> _parser;
    private readonly Func<IEnumerable<TOutput>>? _fnMissing;
    private readonly bool _isRequired;

    public OptionalMultiParser(
        Parser<TInput, IReadOnlyList<TOutput>> parser, 
        Func<IEnumerable<TOutput>>? fnMissing = null,
        bool isRequired = false)
    {
        _parser = parser.ToMultiParser();
        _fnMissing = fnMissing;
        _isRequired = isRequired;
    }

    public override bool IsRequired => _isRequired;

    public override string DebugContent => $"[{_parser.DebugContent}]";

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        if (!_parser.ParseInto(input, outputList, out remainingInput)
            && _fnMissing != null)
        {
            outputList.AddRange(_fnMissing());
        }

        return true;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        _parser.Scan(input, out remainingInput);
        return true;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);
        if (!_parser.Search(input, ref afterMissing, out remainingInput, fnCallback)
            && _isRequired)
        {
            afterMissing = _isRequired;
        }

        return true;
    }
}