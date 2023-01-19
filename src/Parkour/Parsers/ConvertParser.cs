using Parkour;

namespace Parkour.Parsers;

public sealed class ConvertParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput> _parser;
    private readonly Converter<TInput, TOutput> _converter;
    private readonly string? _term;

    public ConvertParser(Parser<TInput> parser, Converter<TInput, TOutput> converter, string? term = null)
    {
        _parser = parser;
        _converter = converter;
        _term = term;
    }

    public override string DebugContent => _parser.DebugContent;

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_parser.Scan(input, out remainingInput))
        {
            var convertLength = input.Length - remainingInput.Length;
            output = _converter(input[..convertLength]);
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
        var success = _parser.Search(input, ref afterMissing, out remainingInput, fnCallback);
        return success;
    }
}