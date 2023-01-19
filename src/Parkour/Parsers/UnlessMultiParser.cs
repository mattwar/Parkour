namespace Parkour.Parsers;

public sealed class UnlessMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly MultiParser<TInput, TOutput> _parser;
    private readonly Parser<TInput> _condition;

    public UnlessMultiParser(MultiParser<TInput, TOutput> parser, Parser<TInput> condition)
    {
        _parser = parser;
        _condition = condition;
    }

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        if (Scan(input, out _))
        {
            return _parser.ParseInto(input, outputList, out remainingInput);
        }

        remainingInput = input;
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