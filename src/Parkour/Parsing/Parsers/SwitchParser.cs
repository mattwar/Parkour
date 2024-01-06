namespace Parkour.Parsers;

public sealed class SwitchParser<TInput, TOutput> : Parser<TInput, TOutput> where TInput: notnull
{
    private readonly SequenceDictionary<TInput, Parser<TInput, TOutput>> _matchTable;

    public SwitchParser(
        Func<SwitchParserContext, SwitchParserContext<TInput, TOutput>> builder,
        EqualityComparer<TInput> comparer)
    {
        // call builder to construct list of input-sequences to parsers
        var matchList = builder(new SwitchParserContext()).MatchList;

        _matchTable = new SequenceDictionary<TInput, Parser<TInput, TOutput>>(comparer, matchList);
    }

    public override ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input)
    {
        if (_matchTable.TryGetBestValue(input, out var parser, out _))
        {
            return parser.Parse(input);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        if (_matchTable.TryGetBestValue(input, out var parser, out _))
        {
            return parser.Scan(input);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        if (_matchTable.TryGetBestValue(input, out var parser, out _))
        {
            return parser.Search(input, afterMissing, fnCallback);
        }

        return default;
    }
}

public struct SwitchParserContext
{
    public SwitchParserContext() {}
}

public struct SwitchParserContext<TInput, TOutput>
    where TInput : notnull
{
    internal readonly List<KeyValuePair<IEnumerable<TInput>, Parser<TInput, TOutput>>> MatchList;

    public SwitchParserContext()
    {
        this.MatchList = new List<KeyValuePair<IEnumerable<TInput>, Parser<TInput, TOutput>>>();
    }

    internal SwitchParserContext<TInput, TOutput> Add(IEnumerable<TInput> input, Parser<TInput, TOutput> parser)
    {
        this.MatchList.Add(KeyValuePair.Create(input, parser));
        return this;
    }
}