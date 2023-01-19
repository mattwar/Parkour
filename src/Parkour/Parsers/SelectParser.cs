namespace Parkour.Parsers;

public sealed class SelectParser<TInput, TOutput, TOutput2> : Parser<TInput, TOutput2>
{
    private readonly Parser<TInput, TOutput> _parser;
    private readonly Func<TOutput, TOutput2> _selector;
    private readonly string? _term;

    public SelectParser(Parser<TInput, TOutput> parser, Func<TOutput, TOutput2> selector, string? term = null)
    {
        _parser = parser;
        _selector = selector;
        _term = term;
    }

    public override string? Term => _term;
    public override string DebugContent => _term != null ? _term : _parser.DebugContent;

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput2 output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_parser.Parse(input, out var item, out remainingInput))
        {
            output = _selector(item);
            return true;
        }

        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        return _parser.Scan(input, out remainingInput);
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);
        return _parser.Search(input, ref afterMissing, out remainingInput, fnCallback);
    }
}