namespace Parkour.Parsers;

public sealed class LimitParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput> _limiter;
    private readonly Parser<TInput, TOutput> _parser;

    public LimitParser(Parser<TInput> limiter, Parser<TInput, TOutput> parser)
    {
        _limiter = limiter;
        _parser = parser;
    }

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_limiter.Scan(input, out remainingInput))
        {
            var limitLength = input.Length - remainingInput.Length;
            var limitedInput = input[..limitLength];
            if (_parser.Parse(limitedInput, out output, out var limitedRemainingInput))
            {
                var consumedLength = limitedInput.Length - limitedRemainingInput.Length;
                remainingInput = input[consumedLength..];
                return true;
            }

        }

        remainingInput = input;
        output = default!;
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

        remainingInput = input;
        return false;
    }
}