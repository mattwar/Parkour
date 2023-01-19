using Parkour;

namespace Parkour.Parsers;

public sealed class FirstParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly IReadOnlyList<Parser<TInput, TOutput>> _parsers;

    public FirstParser(params Parser<TInput, TOutput>[] parsers)
    {
        var allParsers = new List<Parser<TInput, TOutput>>();

        foreach (var parser in parsers)
        {
            if (parser is FirstParser<TInput, TOutput> firstParser)
            {
                allParsers.AddRange(firstParser._parsers);
            }
            else
            {
                allParsers.Add(parser);
            }
        }

        _parsers = allParsers;
    }

    public override string DebugContent => $"{string.Join(" | ", _parsers.Select(p => p.DebugContent))}";

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        foreach (var parser in _parsers)
        {
            if (parser.Parse(input, out output, out remainingInput))
                return true;
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        foreach (var parser in _parsers)
        {
            if (parser.Scan(input, out remainingInput))
                return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        Parser<TInput, TOutput>? firstParser = null;
        var firstRemainingInput = input;
        var firstAfterMissing = afterMissing;

        foreach (var parser in _parsers)
        {
            afterMissing = initialAfterMissing;
            if (parser.Search(input, ref afterMissing, out remainingInput, fnCallback))
            {
                if (firstParser == null)
                {
                    firstParser = parser;
                    firstRemainingInput = remainingInput;
                    firstAfterMissing = afterMissing;
                }
            }
        }

        if (firstParser != null)
        {
            remainingInput = firstRemainingInput;
            afterMissing = firstAfterMissing;
            return true;
        }
        else
        {
            remainingInput = input;
            afterMissing = initialAfterMissing;
            return false;
        }
    }
}