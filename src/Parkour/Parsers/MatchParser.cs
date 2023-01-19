namespace Parkour.Parsers;

public sealed class MatchParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Matcher<TInput> _matcher;
    private readonly Converter<TInput, TOutput> _converter;
    private string? _term;

    public MatchParser(Matcher<TInput> matcher, Converter<TInput, TOutput> converter, string? term)
    {
        _matcher = matcher;
        _converter = converter;
        _term = term;
    }

    public override string? Term => _term;

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        var length = _matcher(input);
        if (length > 0)
        {
            output = _converter(input[..length]);
            remainingInput = input[length..];
            return true;
        }

        output = default!;
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