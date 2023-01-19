namespace Parkour.Parsers;

public class IfParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput> _condition;
    private readonly Parser<TInput, TOutput> _parser;

    public IfParser(Parser<TInput> condition, Parser<TInput, TOutput> parser)
    {
        _condition = condition;
        _parser = parser;
    }

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_condition.Scan(input, out remainingInput))
        {
            return _parser.Parse(input, out output, out remainingInput);
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_condition.Scan(input, out remainingInput))
        {
            return _parser.Scan(input, out remainingInput);
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        if (_condition.Search(input, ref afterMissing, out remainingInput, fnCallback))
        {
            afterMissing = initialAfterMissing;
            return _parser.Search(input, ref afterMissing, out remainingInput, fnCallback);
        }

        remainingInput = input;
        return false;
    }
}
