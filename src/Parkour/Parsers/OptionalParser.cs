namespace Parkour.Parsers;

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

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (!_parser.Parse(input, out output, out remainingInput)
            && _fnMissing != null)
        {
            output = _fnMissing();
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
            afterMissing = IsRequired;
        }

        return true;
    }
}