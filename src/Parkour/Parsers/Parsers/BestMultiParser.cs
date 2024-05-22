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

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        MultiParser<TInput, TOutput>? bestParser = null;
        int bestLength = -1;

        // use scan to find the parser that consumes the most input
        foreach (var parser in _parsers)
        {
            var (success, length) = parser.Scan(input);
            if (success && length > bestLength)
            {
                bestParser = parser;
                bestLength = length;
            }
        }

        if (bestParser != null)
        {
            return bestParser.ParseInto(input, outputList);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        MultiParser<TInput, TOutput>? bestParser = null;
        ScanResult bestResult = default;

        foreach (var parser in _parsers)
        {
            var result = parser.Scan(input);
            if (result.Success && (bestParser == null || result.Length > bestResult.Length))
            {
                bestParser = parser;
                bestResult = result;
            }
        }

        if (bestParser != null)
        {
            return bestResult;
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        MultiParser<TInput, TOutput>? bestParser = null;
        SearchResult bestResult = default;

        foreach (var parser in _parsers)
        {
            var result = parser.Search(input, afterMissing, fnCallback);
            if (result.Success && (bestParser == null || result.Length > bestResult.Length))
            {
                bestParser = parser;
                bestResult = result;
            }
        }

        if (bestParser != null)
        {
            return bestResult;
        }
        else
        {
            return default;
        }
    }
}