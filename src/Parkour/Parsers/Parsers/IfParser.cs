namespace Parkour.Parsers;

/// <summary>
/// A parser that produces the output of the nested parser if the condition succeeds to scan.
/// This parser is used to do arbitrary look-ahead before committing to a parse.
/// </summary>
public class IfParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput> _condition;
    private readonly Parser<TInput, TOutput> _parser;

    public IfParser(Parser<TInput> condition, Parser<TInput, TOutput> parser)
    {
        _condition = condition;
        _parser = parser;
    }

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        if (_condition.Scan(input).Success)
        {
            return _parser.Parse(input);
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
