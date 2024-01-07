namespace Parkour.Parsing.Parsers;

public sealed class RepeatMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly MultiParser<TInput, TOutput> _parser;
    private readonly int _minCount;
    private readonly int _maxCount;

    public RepeatMultiParser(Parser<TInput, IReadOnlyList<TOutput>> parser, int minCount, int maxCount)
    {
        _parser = parser.ToMultiParser();
        _minCount = minCount;
        _maxCount = maxCount;
    }

    public RepeatMultiParser(Parser<TInput, IReadOnlyList<TOutput>> parser, int minCount)
        : this(parser, minCount, 0)
    {
    }

    public override string DebugContent
    {
        get
        {
            if (_maxCount <= 0)
            {
                switch (_minCount)
                {
                    case 0:
                        return $"({_parser.DebugContent})*";
                    case 1:
                        return $"({_parser.DebugContent})+";
                    default:
                        return $"({_parser.DebugContent}){_minCount}+";
                }
            }
            else
            {
                return $"({_parser.DebugContent}){_minCount}:{_maxCount}";
            }
        }
    }

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList)
    {
        var remainingInput = input;
        var outputStart = outputList.Count;

        var count = 0;
        for (; count < _minCount; count++)
        {
            var result = _parser.ParseInto(remainingInput, outputList);
            if (!result.Success)
            {
                var removeCount = outputList.Count - outputStart;
                if (removeCount > 0)
                    outputList.RemoveRange(outputStart, removeCount);
                return default;
            }

            remainingInput = remainingInput.Slice(result.Length);
        }

        for (; _maxCount <= 0 || count < _maxCount; count++)
        {
            var result = _parser.ParseInto(remainingInput, outputList);
            if (!result.Success)
                break;
            remainingInput = remainingInput.Slice(result.Length);
        }

        return new ParseIntoResult(true, input.Length - remainingInput.Length);
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var remainingInput = input;

        var count = 0;
        for (; count < _minCount; count++)
        {
            var result = _parser.Scan(remainingInput);
            if (!result.Success)
                return default;
            remainingInput = remainingInput.Slice(result.Length);
        }

        for (; _maxCount <= 0 || count < _maxCount; count++)
        {
            var result = _parser.Scan(remainingInput);
            if (!result.Success)
                break;
            remainingInput = remainingInput.Slice(result.Length);
        }

        return new ScanResult(true, input.Length - remainingInput.Length);
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var remainingInput = input;

        var count = 0;
        for (; count < _minCount; count++)
        {
            var result = _parser.Search(remainingInput, afterMissing, fnCallback);
            if (!result.Success)
                return default;
            remainingInput = remainingInput.Slice(result.Length);
            afterMissing = result.AfterMissing;
        }

        for (; _maxCount <= 0 || count < _maxCount; count++)
        {
            var result = _parser.Search(remainingInput, afterMissing, fnCallback);
            if (!result.Success)
                break;
            remainingInput = remainingInput.Slice(result.Length);
            afterMissing = result.AfterMissing;
        }

        return new SearchResult(true, input.Length - remainingInput.Length, afterMissing);
    }
}