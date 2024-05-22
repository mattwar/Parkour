namespace Parkour.Parsers;

/// <summary>
/// A parser that aggregates the output of the first parser with zero or more outputs of the second parser.
/// This is typically used with postfix operators:  x ++ ++ 
/// </summary>
public sealed class LeftReduceParser<TInput, TOutput1, TOutput2> : Parser<TInput, TOutput1>
{
    private readonly Parser<TInput, TOutput1> _parser1;
    private readonly Parser<TInput, TOutput2> _parser2;
    private readonly Func<TOutput1, TOutput2, TOutput1> _fnAggregate;

    public LeftReduceParser(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Func<TOutput1, TOutput2, TOutput1> fnAggregate)
    {
        _parser1 = parser1;
        _parser2 = parser2;
        _fnAggregate = fnAggregate;
    }

    public override string DebugContent => $"{_parser1.DebugContent} {{{_parser2.DebugContent}}}";

    public override ParseResult<TOutput1> Parse(ReadOnlySpan<TInput> input)
    {
        var consumed = 0;

        // first parser must succeed but not second parser
        var result1 = _parser1.Parse(input);
        if (result1.Success)       
        {
            var output1 = result1.Output;
            consumed += result1.Length;
            input = input.Slice(result1.Length);

            while (true)
            {
                var result2 = _parser2.Parse(input);
                if (!result2.Success)
                    break;

                consumed += result2.Length;
                input = input.Slice(result2.Length);
                output1 = _fnAggregate(output1, result2.Output);
            }

            return new ParseResult<TOutput1>(true, consumed, output1);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var consumed = 0;

        // first parser must succeed but not second parser
        var result1 = _parser1.Scan(input);
        if (result1.Success)
        {
            consumed += result1.Length;
            input = input.Slice(result1.Length);

            while (true)
            {
                var result2 = _parser2.Scan(input);
                if (!result2.Success)
                    break;

                consumed += result2.Length;
                input = input.Slice(result2.Length);
            }

            return new ScanResult(true, consumed);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var consumed = 0;

        // first parser must succeed but not second parser
        var result1 = _parser1.Search(input, afterMissing, fnCallback);
        if (result1.Success)
        {
            consumed += result1.Length;
            input = input.Slice(result1.Length);
            afterMissing = result1.AfterMissing;

            while (true)
            {
                var result2 = _parser2.Search(input, afterMissing, fnCallback);
                if (!result2.Success)
                    break;

                consumed += result2.Length;
                input = input.Slice(result2.Length);
                afterMissing = result2.AfterMissing;
            }

            return new SearchResult(true, consumed, afterMissing);
        }

        return default;
    }
}