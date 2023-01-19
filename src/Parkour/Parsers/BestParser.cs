using Parkour;

namespace Parkour.Parsers;

public sealed class BestParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly IReadOnlyList<Parser<TInput, TOutput>> _parsers;

    public BestParser(params Parser<TInput, TOutput>[] parsers)
    {
        var allParsers = new List<Parser<TInput, TOutput>>();

        foreach (var parser in parsers)
        {
            if (parser is BestParser<TInput, TOutput> bestParser)
            {
                allParsers.AddRange(bestParser._parsers);
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
        var bestReamainingInput = input;
        Parser<TInput, TOutput>? bestParser = null;

        // use scan to find the parser that consumes the most input
        foreach (var parser in _parsers)
        {
            if (parser.Scan(input, out remainingInput))
            {
                if (bestParser == null || remainingInput.Length < bestReamainingInput.Length)
                {
                    bestParser = parser;
                    bestReamainingInput = remainingInput;
                }
            }
        }

        if (bestParser != null
            && bestParser.Parse(input, out output, out remainingInput))
        {
            return true;
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        var bestReamainingInput = input;
        Parser<TInput, TOutput>? bestParser = null;

        foreach (var parser in _parsers)
        {
            if (parser.Scan(input, out remainingInput))
            {
                if (bestParser == null || remainingInput.Length < bestReamainingInput.Length)
                {
                    bestParser = parser;
                    bestReamainingInput = remainingInput;
                }
            }
        }

        if (bestParser != null)
        {
            remainingInput = bestReamainingInput;
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        Parser<TInput, TOutput>? bestParser = null;
        var bestReamainingInput = input;
        var bestAfterMissing = afterMissing;

        // find parser that consumes most input
        foreach (var parser in _parsers)
        {
            afterMissing = bestAfterMissing;
            if (parser.Search(input, ref afterMissing, out remainingInput, fnCallback))
            {
                if (bestParser == null || remainingInput.Length < bestReamainingInput.Length)
                {
                    bestParser = parser;
                    bestReamainingInput = remainingInput;
                    bestAfterMissing = afterMissing;
                }
            }
        }

        if (bestParser != null)
        {
            remainingInput = bestReamainingInput;
            afterMissing = bestAfterMissing;
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