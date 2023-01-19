namespace Parkour.Parsers;

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

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        remainingInput = input;
        var outputStart = outputList.Count;

        var count = 0;
        for (; count < _minCount; count++)
        {
            if (!_parser.ParseInto(remainingInput, outputList, out remainingInput))
            {
                var removeCount = outputList.Count - outputStart;
                if (removeCount > 0)
                    outputList.RemoveRange(outputStart, removeCount);
                remainingInput = input;
                return false;
            }
        }

        for (; _maxCount <= 0 || count < _maxCount; count++)
        {
            if (!_parser.ParseInto(remainingInput, outputList, out remainingInput))
                break;
        }

        return true;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        remainingInput = input;

        var count = 0;
        for (; count < _minCount; count++)
        {
            if (!_parser.Scan(remainingInput, out remainingInput))
            {
                remainingInput = input;
                return false;
            }
        }

        for (; _maxCount <= 0 || count < _maxCount; count++)
        {
            if (!_parser.Scan(remainingInput, out remainingInput))
                break;
        }

        return true;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        remainingInput = input;

        var count = 0;
        for (; count < _minCount; count++)
        {
            if (!_parser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
            {
                remainingInput = input;
                afterMissing = initialAfterMissing;
                return false;
            }
        }

        for (; _maxCount <= 0 || count < _maxCount; count++)
        {
            if (!_parser.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
                break;
        }

        return true;
    }
}