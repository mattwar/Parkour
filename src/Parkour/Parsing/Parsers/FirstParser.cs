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

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        foreach (var parser in _parsers)
        {
            var result = parser.Parse(input);
            if (result.Success)
                return result;
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        foreach (var parser in _parsers)
        {
            var result = parser.Scan(input);
            if (result.Success)
                return result;
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        Parser<TInput, TOutput>? firstParser = null;
        SearchResult firstResult = default;

        foreach (var parser in _parsers)
        {
            var result = parser.Search(input, afterMissing, fnCallback);
            if (result.Success && firstParser == null)
            {
                firstParser = parser;
                firstResult = result;
            }
        }

        if (firstParser != null)
        {
            return firstResult;
        }
        else
        {
            return default;
        }
    }
}