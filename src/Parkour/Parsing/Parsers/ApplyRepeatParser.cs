namespace Parkour.Parsing.Parsers;

/// <summary>
/// A parser that aggregates the output of the first parser with zero or more outputs of the second parser.
/// This is typically used with infix operators:  x and y and z
/// </summary>
public sealed class ApplyRepeatParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser1;
    private readonly Parser<TInput, TOutput> _parser2;
    private readonly int _minCount;
    private readonly int _maxCount;

    private TOutput _currentOutput;

    public ApplyRepeatParser(
        Parser<TInput, TOutput> parser1,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnParser2,
        int minCount,
        int maxCount = -1)
    {
        _parser1 = parser1;
        Func<TOutput> fnOutput = () => _currentOutput!;
        _parser2 = fnParser2(fnOutput);
        _minCount = minCount;
        _maxCount = maxCount > 0 ? maxCount : Int32.MaxValue;
        _currentOutput = default!;
    }

    public override string DebugContent => $"{_parser1.DebugContent} {{{_parser2.DebugContent}}}";

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        var consumed = 0;
        var count = 0;

        // first parser must succeed and second parser must succeed within min/max range.
        var result1 = _parser1.Parse(input);
        if (result1.Success)
        {
            consumed += result1.Length;
            input = input.Slice(result1.Length);
            _currentOutput = result1.Output;

            while (count < _maxCount)
            {
                var result2 = _parser2.Parse(input);
                if (!result2.Success)
                    break;

                consumed += result2.Length;
                input = input.Slice(result2.Length);
                _currentOutput = result2.Output;
                count++;
            }

            if (count >= _minCount && count <= _maxCount)
                return new ParseResult<TOutput>(true, consumed, _currentOutput);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var consumed = 0;
        var count = 0;

        // first parser must succeed but not second parser
        var result1 = _parser1.Scan(input);
        if (result1.Success)
        {
            consumed += result1.Length;
            input = input.Slice(result1.Length);

            while (count < _maxCount)
            {
                var result2 = _parser2.Scan(input);
                if (!result2.Success)
                    break;

                consumed += result2.Length;
                input = input.Slice(result2.Length);
                count++;
            }

            if (count >= _minCount && count <= _maxCount)
                return new ScanResult(true, consumed);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var consumed = 0;
        var count = 0;

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
                count++;
            }

            if (count >= _minCount && count <= _maxCount)
                return new SearchResult(true, consumed, afterMissing);
        }

        return default;
    }
}