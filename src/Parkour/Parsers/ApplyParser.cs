using Parkour;

namespace Parkour.Parsers;

public sealed class ApplyParser<TInput, TOutput1, TOutput2, TOutput3> : Parser<TInput, TOutput3>
{
    private readonly Parser<TInput, TOutput1> _parser1;
    private readonly Parser<TInput, TOutput2> _parser2;
    private readonly Func<TOutput1, TOutput2, TOutput3> _fnMapper;

    private TOutput1 _currentOutput1;

    public ApplyParser(
        Parser<TInput, TOutput1> parser1,
        Func<Func<TOutput1>, Parser<TInput, TOutput2>> fnParser2,
        Func<TOutput1, TOutput2, TOutput3> fnMapper)
    {
        _parser1 = parser1;
        Func<TOutput1> fnOutput1 = () => _currentOutput1!;
        _parser2 = fnParser2(fnOutput1);
        _fnMapper = fnMapper;
        _currentOutput1 = default!;
    }

    public override string DebugContent => $"{_parser1.DebugContent} {_parser2.DebugContent}";


    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput3 output, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_parser1.Parse(input, out var output1, out remainingInput))
        {
            var prevOutput1 = _currentOutput1;
            _currentOutput1 = output1;

            if (_parser2.Parse(remainingInput, out var output2, out remainingInput))
            {
                _currentOutput1 = prevOutput1;
                output = _fnMapper(output1, output2);
                return true;
            }

            _currentOutput1 = prevOutput1;
        }

        remainingInput = input;
        output = default!;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_parser1.Scan(input, out remainingInput)
            && _parser2.Scan(remainingInput, out remainingInput))
        {
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        var initialAfterMissing = afterMissing;
        fnCallback(this, input, afterMissing);

        if (_parser1.Search(input, ref afterMissing, out remainingInput, fnCallback)
            && _parser2.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
        {
            return true;
        }

        remainingInput = input;
        afterMissing = initialAfterMissing;
        return false;
    }
}