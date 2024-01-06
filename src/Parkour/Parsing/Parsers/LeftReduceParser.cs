using Parkour;

namespace Parkour.Parsers;

public sealed class LeftReduceParser<TInput, TOutput> : Parser<TInput, TOutput>
{
    private readonly Parser<TInput, TOutput> _parser1;
    private readonly Parser<TInput, TOutput> _parser2;
    private readonly bool _once;

    private TOutput _currentOutput;

    public LeftReduceParser(
        Parser<TInput, TOutput> parser1,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnParser2,
        bool once = false)
    {
        _parser1 = parser1;
        Func<TOutput> fnOutput = () => _currentOutput!;
        _parser2 = fnParser2(fnOutput);
        _once = once;
        _currentOutput = default!;
    }

    public override string DebugContent => $"{_parser1.DebugContent} {{{_parser2.DebugContent}}}";

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        var consumed = 0;

        // first parser must succeed but not second parser
        var result = _parser1.Parse(input);
        if (result.Success)       
        {
            while (true)
            {
                consumed += result.Length;
                input = input.Slice(result.Length);

                _currentOutput = result.Output;

                var result2 = _parser2.Parse(input);
                if (!result2.Success)
                    return new ParseResult<TOutput>(true, consumed, result.Output);

                result = result2;

                if (_once)
                    return new ParseResult<TOutput>(true, consumed + result.Length, result.Output);
            }
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var consumed = 0;

        // first parser must succeed but not second parser
        var result = _parser1.Scan(input);
        if (result.Success)
        {
            while (true)
            {
                consumed += result.Length;
                input = input.Slice(result.Length);

                var result2 = _parser2.Scan(input);
                if (!result2.Success)
                    return new ScanResult(true, consumed);

                result = result2;

                if (_once)
                    return new ScanResult(true, consumed + result.Length);
            }
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var consumed = 0;

        // first parser must succeed but not second parser
        var result = _parser1.Search(input, afterMissing, fnCallback);
        if (result.Success)
        {
            while (true)
            {
                consumed += result.Length;
                input = input.Slice(result.Length);

                var result2 = _parser2.Search(input, result.AfterMissing, fnCallback);
                if (!result2.Success)
                    return new SearchResult(true, consumed, result.AfterMissing);

                result = result2;

                if (_once)
                    return new SearchResult(true, consumed + result.Length, result.AfterMissing);
            }
        }

        return default;
    }
}