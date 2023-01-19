namespace Parkour.Parsers;

public sealed class RequiredParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser;
    private readonly Func<TOutput> _fnMissing;

    public RequiredParser(Parser<TInput, TOutput> parser, Func<TOutput> fnMissing)
    {
        _parser = parser;
        _fnMissing = fnMissing;
    }

    public override bool IsRequired => true;

    public override string DebugContent => $"req({_parser.DebugContent})";

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (!_parser.Parse(input, out output, out remainingInput))
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

        if (!_parser.Search(input, ref afterMissing, out remainingInput, fnCallback))
        {
            afterMissing = true;
        }

        return true;
    }
}