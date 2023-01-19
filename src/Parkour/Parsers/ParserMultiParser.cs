namespace Parkour.Parsers;

public class ParserMultiParser<TInput, TOutput> : MultiParser<TInput, TOutput>
{
    private readonly Parser<TInput> _parser;

    public ParserMultiParser(Parser<TInput, TOutput> parser)
    {
        _parser = parser;
    }

    public ParserMultiParser(Parser<TInput, IReadOnlyList<TOutput>> parser)
    {
        _parser = parser;
    }

    public override string DebugContent => _parser.DebugContent;

    public override bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput)
    {
        if (_parser is Parser<TInput, TOutput> outputParser
            && outputParser.Parse(input, out var outputItem, out remainingInput))
        {
            outputList.Add(outputItem);
            return true;
        }
        else if (_parser is Parser<TInput, IReadOnlyList<TOutput>> listParser
            && listParser.Parse(input, out var outputItems, out remainingInput))
        {
            outputList.AddRange(outputItems);
            return true;
        }

        remainingInput = input;
        return false;
    }

    public override bool Scan(ReadOnlySpan<TInput> input, out ReadOnlySpan<TInput> remainingInput)
    {
        return _parser.Scan(input, out remainingInput);
    }

    public override bool Search(ReadOnlySpan<TInput> input, ref bool afterMissing, out ReadOnlySpan<TInput> remainingInput, SearchCallback<TInput> fnCallback)
    {
        fnCallback(this, input, afterMissing);
        return _parser.Search(input, ref afterMissing, out remainingInput, fnCallback);
    }
}