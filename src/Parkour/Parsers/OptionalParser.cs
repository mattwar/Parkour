namespace Parkour.Parsers;

public sealed class OptionalParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser;

    public OptionalParser(Parser<TInput, TOutput> parser)
    {
        _parser = parser;
    }

    public override string DebugContent => $"[{_parser.DebugContent}]";

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        _parser.Parse(input, out output, out remainingInput);
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
        _parser.Search(input, ref afterMissing, out remainingInput, fnCallback);
        return true;
    }
}