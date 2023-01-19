using Parkour;

namespace Parkour.Parsers;

public sealed class BestMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly IReadOnlyList<MultiParser<TInput, TOutput>> _parsers;

    public BestMultiParser(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers)
    {
        var allParsers = new List<MultiParser<TInput, TOutput>>();

        foreach (var parser in parsers)
        {
            if (parser is BestMultiParser<TInput, TOutput> bestParser)
            {
                allParsers.AddRange(bestParser._parsers);
            }
            else
            {
                allParsers.Add(parser.ToMultiParser());
            }
        }

        _parsers = allParsers;
    }

    public override string DebugContent => $"{string.Join(" | ", _parsers.Select(p => p.DebugContent))}";

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        var bestReamainingInput = input;
        MultiParser<TInput, TOutput>? bestParser = null;

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
            && bestParser.ParseInto(input, outputList, out remainingInput))
        {
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        var bestReamainingInput = input;
        MultiParser<TInput, TOutput>? bestParser = null;

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
        MultiParser<TInput, TOutput>? bestParser = null;
        var bestReamainingInput = input;
        var bestAfterMissing = afterMissing;

        foreach (var parser in _parsers)
        {
            afterMissing = initialAfterMissing;
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