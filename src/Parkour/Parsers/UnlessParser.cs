namespace Parkour.Parsers;

public sealed class UnlessParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser;
    private readonly Parser<TInput> _condition;

    public UnlessParser(Parser<TInput, TOutput> parser, Parser<TInput> condition)
    {
        _parser = parser;
        _condition = condition;
    }

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (Scan(input, out _))
        {
            return _parser.Parse(input, out output, out remainingInput);
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_parser.Scan(input, out remainingInput)
            && !_condition.Scan(remainingInput, out _))
        {
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;

        if (_parser.Search(input, ref afterMissing, out remainingInput, fnCallback)
            && !_condition.Scan(remainingInput, out _))
        {
            return true;
        }

        remainingInput = input;
        afterMissing = initialAfterMissing;
        return false;
    }
}