namespace Parkour.Parsers;

public sealed class NotParser<TInput> : Parser<TInput, TInput>
{
    private readonly Parser<TInput> _parser;

    public NotParser(Parser<TInput> parser)
    {
        _parser = parser;
    }

    public override string DebugContent => $"not({_parser.DebugContent})";

    public override bool Parse(ReadOnlySpan<TInput> input, out TInput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (!_parser.Scan(input, out remainingInput) && input.Length > 0)
        {
            output = input[0];
            remainingInput = input[1..];
            return true;
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        if (!_parser.Scan(input, out _) && input.Length > 0)
        {
            remainingInput = input[1..];
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        if (!_parser.Scan(input, out _) && input.Length > 0)
        {
            remainingInput = input[1..];
            return true;
        }

        remainingInput = input;
        return false;
    }
}