namespace Parkour.Parsers;

public class IfMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly Parser<TInput> _condition;
    private readonly MultiParser<TInput, TOutput> _parser;

    public IfMultiParser(Parser<TInput> condition, MultiParser<TInput, TOutput> parser)
    {
        _condition = condition;
        _parser = parser;
    }

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_condition.Scan(input, out remainingInput))
        {
            return _parser.ParseInto(input, outputList, out remainingInput);
        }

        remainingInput = input;
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
