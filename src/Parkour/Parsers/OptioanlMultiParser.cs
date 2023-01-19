namespace Parkour.Parsers;

public sealed class OptionalMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly MultiParser<TInput, TOutput> _parser;

    public OptionalMultiParser(Parser<TInput, IReadOnlyList<TOutput>> parser)
    {
        _parser = parser.ToMultiParser();
    }

    public override string DebugContent => $"[{_parser.DebugContent}]";

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        _parser.ParseInto(input, outputList, out remainingInput);
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