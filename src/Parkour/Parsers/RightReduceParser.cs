using Parkour;

namespace Parkour.Parsers;

public sealed class RightReduceParser<TInput, TOutput1, TOutput2> : Parser<TInput, TOutput2>
{
    private readonly Parser<TInput, TOutput1> _parser1;
    private readonly Parser<TInput, TOutput2> _parser2;
    private readonly Func<TOutput1, TOutput2, TOutput2> _fnAggregator;
    private readonly bool _once;

    public RightReduceParser(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Func<TOutput1, TOutput2, TOutput2> fnAggregate)
    {
        _parser1 = parser1;
        _parser2 = parser2;
        _fnAggregator = fnAggregate;
    }

    public override string DebugContent => $"{{{_parser1.DebugContent}}} {_parser2.DebugContent})";

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput2 output, out ReadOnlySpan<TInput> remainingInput)
    {
        List<TOutput1>? list = null;

        remainingInput = input;
        while (_parser1.Parse(remainingInput, out var output1, out remainingInput))
        {
            if (list == null)
                list = new List<TOutput1>();
            list.Add(output1);
        }

        if (_parser2.Parse(remainingInput, out output, out remainingInput))
        {
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    output = _fnAggregator(list[i], output);
                }
            }

            return true;
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        remainingInput = input;
        while (_parser1.Scan(remainingInput, out remainingInput))
        {
        }

        if (_parser2.Scan(remainingInput, out remainingInput))
        {
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        var initialAfterMissing = afterMissing;
        remainingInput = input;

        while (_parser1.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
        {
        }

        if (_parser2.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
        {
            return true;
        }

        remainingInput = input;
        afterMissing = initialAfterMissing;
        return false;
    }
}