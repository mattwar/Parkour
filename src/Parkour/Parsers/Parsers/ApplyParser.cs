namespace Parkour.Parsers;

/// <summary>
/// Applies the output of parser one to parser2 and 
/// </summary>
public sealed class ApplyParser<TInput, TOutput1, TOutput2> : Parser<TInput, TOutput2>
{
    private readonly Parser<TInput, TOutput1> _parser1;
    private readonly Parser<TInput, TOutput2> _parser2;

    private TOutput1 _currentOutput1;

    public ApplyParser(
        Parser<TInput, TOutput1> parser1,
        Func<Func<TOutput1>, Parser<TInput, TOutput2>> fnParser2)
    {
        _parser1 = parser1;
        Func<TOutput1> fnOutput1 = () => _currentOutput1!;
        _parser2 = fnParser2(fnOutput1);
        _currentOutput1 = default!;
    }

    public override string DebugContent => $"{_parser1.DebugContent} {_parser2.DebugContent}";

    public override ParseResult<TOutput2> Parse(ReadOnlySpan<TInput> input)
    {
        var result1 = _parser1.Parse(input);
        if (result1.Success)
        {
            var prevOutput1 = _currentOutput1;
            _currentOutput1 = result1.Output!;

            var input2 = input.Slice(result1.Length);
            var result2 = _parser2.Parse(input2);
            if (result2.Success)
            {
                _currentOutput1 = prevOutput1;
                return new ParseResult<TOutput2>(true, result1.Length + result2.Length, result2.Output);
            }

            _currentOutput1 = prevOutput1;
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var result1 = _parser1!.Scan(input);
        if (result1.Success)
        {
            var input2 = input.Slice(result1.Length);
            var result2 = _parser2!.Scan(input2);
            if (result2.Success)
            {
                return new ScanResult(true, result1.Length + result2.Length);
            }
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var result1 = _parser1.Search(input, afterMissing, fnCallback);
        if (result1.Success)
        {
            var input2 = input.Slice(result1.Length);
            var result2 = _parser2.Search(input2, result1.AfterMissing, fnCallback);
            if (result2.Success)
            {
                return new SearchResult(true, result1.Length + result2.Length, result2.AfterMissing);
            }
        }

        return default;
    }
}