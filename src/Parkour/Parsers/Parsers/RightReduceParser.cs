namespace Parkour.Parsers;

/// <summary>
/// A parser that aggregates zero or more outputs of the first parser with the output of the second parser.
/// This is typically used with prefix operators:  ++ ++ x
/// </summary>
public sealed class RightReduceParser<TInput, TOutput1, TOutput2> : Parser<TInput, TOutput2>
{
    private readonly Parser<TInput, TOutput1> _parser1;
    private readonly Parser<TInput, TOutput2> _parser2;
    private readonly Func<TOutput1, TOutput2, TOutput2> _fnAggregate;

    public RightReduceParser(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Func<TOutput1, TOutput2, TOutput2> fnAggregate)
    {
        _parser1 = parser1;
        _parser2 = parser2;
        _fnAggregate = fnAggregate;
    }

    public override string DebugContent => $"{{{_parser1.DebugContent}}} {_parser2.DebugContent})";

    public override ParseResult<TOutput2> Parse(ReadOnlySpan<TInput> input)
    {
        List<TOutput1>? list = null;

        var remainingInput = input;

        // consume as many first parser outputs
        while (true)
        {
            var result1 = _parser1.Parse(remainingInput);
            if (!result1.Success)
                break;
            remainingInput = remainingInput.Slice(result1.Length);
            if (list == null)
                list = new List<TOutput1>();
            list.Add(result1.Output);
        }

        var result2 = _parser2.Parse(remainingInput);
        if (result2.Success)
        {
            var output = result2.Output;
            remainingInput = remainingInput.Slice(result2.Length);

            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    output = _fnAggregate(list[i], output);
                }
            }

            return new ParseResult<TOutput2>(true, input.Length - remainingInput.Length, output);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var remainingInput = input;

        while (true)
        {
            var result = _parser1.Scan(remainingInput);
            if (!result.Success)
                break;
            remainingInput = remainingInput.Slice(result.Length);
        }

        var result2 = _parser2.Scan(remainingInput);
        if (result2.Success)
        {
            remainingInput = remainingInput.Slice(result2.Length);
            return new ScanResult(true, input.Length - remainingInput.Length);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var remainingInput = input;

        while (true)
        {
            var result = _parser1.Search(remainingInput, afterMissing, fnCallback);
            if (!result.Success)
                break;
            remainingInput = remainingInput.Slice(result.Length);
            afterMissing = result.AfterMissing;
        }

        var result2 = _parser2.Search(remainingInput, afterMissing, fnCallback);
        if (result2.Success)
        {
            remainingInput = remainingInput.Slice(result2.Length);
            afterMissing = result2.AfterMissing;
            return new SearchResult(true, input.Length - remainingInput.Length, afterMissing);
        }

        return default;
    }
}