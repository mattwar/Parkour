namespace Parkour.Parsing.Parsers;

public class IfMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly Parser<TInput> _condition;
    private readonly MultiParser<TInput, TOutput> _parser;

    public IfMultiParser(Parser<TInput> condition, MultiParser<TInput, TOutput> parser)
    {
        _condition = condition;
        _parser = parser;
    }

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        if (_condition.Scan(input).Success)
        {
            return _parser.ParseInto(input, outputList);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        if (_condition.Scan(input).Success)
        {
            return _parser.Scan(input);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        if (_condition.Search(input, afterMissing, fnCallback).Success)
        {
            return _parser.Search(input, afterMissing, fnCallback);
        }

        return default;
    }
}
