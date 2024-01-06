namespace Parkour.Parsers;

public class ParserMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly Parser<TInput> _parser;

    public ParserMultiParser(Parser<TInput, TOutput> parser)
    {
        _parser = parser;
    }

    public ParserMultiParser(Parser<TInput, IReadOnlyList<TOutput>> parser)
    {
        _parser = parser;
    }

    public override string DebugContent => _parser.DebugContent;

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        if (_parser is Parser<TInput, TOutput> outputParser)
        {
            var result = outputParser.Parse(input);
            if (result.Success)
            {
                outputList.Add(result.Output);
                return new ParseIntoResult(true, result.Length);
            }
        }
        else if (_parser is Parser<TInput, IReadOnlyList<TOutput>> listParser)
        {
            var result = listParser.Parse(input);
            if (result.Success)
            {
                outputList.AddRange(result.Output);
                return new ParseIntoResult(true, result.Length);
            }
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        return _parser.Scan(input);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        return _parser.Search(input, afterMissing, fnCallback);
    }
}