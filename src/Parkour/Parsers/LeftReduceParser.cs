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

    public override bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput)
    {
        // first parser must succeed but not second parser
        if (_parser1.Parse(input, out output, out remainingInput))
        {
            var prevOutput = _currentOutput;
            _currentOutput = output;

            while (_parser2.Parse(remainingInput, out output, out remainingInput))
            {
                _currentOutput = output;
            }

            output = _currentOutput;
            return true;
        }

        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        // first parser must succeed but not second parser
        if (_parser1.Scan(input, out remainingInput))
        {
            while (_parser2.Scan(remainingInput, out remainingInput))
            {
                if (_once)
                    break;
            }

            return true;
        }

        return false;
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);

        // first parser must succeed but not second parser
        if (_parser1.Search(input, ref afterMissing, out remainingInput, fnCallback))
        {
            while (_parser2.Search(remainingInput, ref afterMissing, out remainingInput, fnCallback))
            {
                if (_once)
                    break;
            }

            return true;
        }

        return false;
    }
}