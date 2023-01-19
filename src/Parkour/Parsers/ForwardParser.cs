using Parkour;

namespace Parkour.Parsers;

public sealed class ForwardParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Func<Parser<TInput, TOutput>> _fnParser;
    private readonly string? _term;

    public ForwardParser(Func<Parser<TInput, TOutput>> fnParser, string? term = null)
    {
        _fnParser = fnParser;
        _term = term;
    }

    public override string? Term => _term ?? _fnParser().Term ?? "...";

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        return _fnParser().Parse(input, out output, out remainingInput);
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        return _fnParser().Scan(input, out remainingInput);
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);
        return _fnParser().Search(input, ref afterMissing, out remainingInput, fnCallback);
    }
}