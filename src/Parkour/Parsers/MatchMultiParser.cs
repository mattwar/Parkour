namespace Parkour.Parsers;

public sealed class MatchMultiParser<TInput> : MultiParser<TInput, TInput>
{
    private readonly Matcher<TInput> _matcher;
    private string? _term;

    public MatchMultiParser(Matcher<TInput> matcher, string? term)
    {
        _matcher = matcher;
        _term = term;
    }

    public override string? Term => _term;

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TInput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        var length = _matcher(input);
        if (length > 0 && length <= input.Length)
        {
            for (int i = 0; i < length; i++)
            {
                outputList.Add(input[i]);
            }

            remainingInput = input[length..];
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        var length = _matcher(input);
        if (length > 0)
        {
            remainingInput = input[length..];
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        if (Scan(input, out remainingInput))
        {
            afterMissing = false;
            return true;
        }

        return false;
    }
}
