namespace Parkour.Parsing.Parsers;

public sealed class FirstMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly IReadOnlyList<MultiParser<TInput, TOutput>> _parsers;

    public FirstMultiParser(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers)
    {
        var allParsers = new List<MultiParser<TInput, TOutput>>();

        foreach (var parser in parsers)
        {
            if (parser is FirstMultiParser<TInput, TOutput> firstParser)
            {
                allParsers.AddRange(firstParser._parsers);
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
        foreach (var parser in _parsers)
        {
            var result = parser.ParseInto(input, outputList);
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

        MultiParser<TInput, TOutput>? firstParser = null;
        SearchResult firstResult = default;

        foreach (var parser in _parsers)
        {
            var result = 
                parser.Search(input, afterMissing, fnCallback);

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