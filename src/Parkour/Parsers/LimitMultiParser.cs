namespace Parkour.Parsers;

public sealed class LimitMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly Parser<TInput> _limiter;
    private readonly MultiParser<TInput, TOutput> _parser;

    public LimitMultiParser(Parser<TInput> limiter, MultiParser<TInput, TOutput> parser)
    {
        _limiter = limiter;
        _parser = parser;
    }

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_limiter.Scan(input, out remainingInput))
        {
            var limitLength = input.Length - remainingInput.Length;
            var limitedInput = input[..limitLength];
            if (_parser.ParseInto(limitedInput, outputList, out var limitedRemainingInput))
            {
                var consumedLength = limitedInput.Length - limitedRemainingInput.Length;
                remainingInput = input[consumedLength..];
                return true;
            }
        }

        remainingInput = input;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_limiter.Scan(input, out remainingInput))
        {
            var limitLength = input.Length - remainingInput.Length;
            var limitedInput = input[..limitLength];
            var success = _parser.Scan(limitedInput, out var limitedRemainingInput);
            var consumedLength = limitedInput.Length - limitedRemainingInput.Length;
            remainingInput = input[consumedLength..];
            return success;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        if (_limiter.Scan(input, out remainingInput))
        {
            var limitLength = input.Length - remainingInput.Length;
            var limitedInput = input[..limitLength];
            var success = _parser.Search(limitedInput, ref afterMissing, out var limitedRemainingInput, fnCallback);
            var consumedLength = limitedInput.Length - limitedRemainingInput.Length;
            remainingInput = input[consumedLength..];
            return success;
        }
        else
        {
            remainingInput = input;
            return false;
        }
    }
}