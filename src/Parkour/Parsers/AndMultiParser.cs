using Parkour;

namespace Parkour.Parsers;

public sealed class AndMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly IReadOnlyList<MultiParser<TInput, TOutput>> _parsers;

    public AndMultiParser(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers)
    {
        var allParsers = new List<MultiParser<TInput, TOutput>>();

        foreach (var parser in parsers)
        {
            if (parser is AndMultiParser<TInput, TOutput> andParser)
            {
                allParsers.AddRange(andParser._parsers);
            }
            else
            {
                allParsers.Add(parser.ToMultiParser());
            }
        }

        _parsers = allParsers;
    }

    public override string DebugContent => $"{string.Join(" ", _parsers.Select(p => $"{p.DebugContent}"))}";

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        // scan first so we don't put any partial into output
        if (!Scan(input, out remainingInput))
        {
            return false;
        }

        remainingInput = input;
        foreach (var parser in _parsers)
        {
            parser.ParseInto(remainingInput, outputList, out remainingInput);
        }

        return true;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        remainingInput = input;

        foreach (var parser in _parsers)
        {
            if (!parser.Scan(remainingInput, out remainingInput))
            {
                remainingInput = input;
                return false;
            }
        }

        return true;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        remainingInput = input;

        foreach (var parser in _parsers)
        {
            if (!parser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
            {
                remainingInput = input;
                afterMissing = initialAfterMissing;
                return false;
            }
        }

        return true;
    }
}