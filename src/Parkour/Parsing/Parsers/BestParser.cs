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

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        Parser<TInput, TOutput>? bestParser = null;
        int bestLength = -1;

        // use scan to find the parser that consumes the most input
        foreach (var parser in _parsers)
        {
            var result = parser.Scan(input);
            if (result.Success && result.Length > bestLength)
            {
                bestParser = parser;
                bestLength = result.Length;
            }
        }

        if (bestParser != null)
        {
            return bestParser.Parse(input);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        Parser<TInput, TOutput>? bestParser = null;
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

        Parser<TInput, TOutput>? bestParser = null;
        SearchResult bestResult = default;

        // find parser that consumes most input
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