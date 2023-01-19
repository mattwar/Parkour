namespace Parkour;
using Parsers;

public static class ParserFactory<TInput>
{
    public static Parser<TInput, TInput> Any =
        Match(span => span.Length > 0 ? 1 : 0, "<any>");

    public static Parser<TInput, TOutput> Best<TOutput>(params Parser<TInput, TOutput>[] parsers) =>
        new BestParser<TInput, TOutput>(parsers);

    public static Parser <TInput, IReadOnlyList<TOutput>> Best<TOutput>(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers) =>
        new BestMultiParser<TInput, TOutput>(parsers);

    public static Parser<TInput, TOutput> First<TOutput>(params Parser<TInput, TOutput>[] parsers) =>
        new FirstParser<TInput, TOutput>(parsers);

    public static Parser<TInput, IReadOnlyList<TOutput>> First<TOutput>(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers) =>
        new FirstMultiParser<TInput, TOutput>(parsers);

    public static Parser<TInput, TOutput> Forward<TOutput>(Func<Parser<TInput, TOutput>> fnParser, string? term = null) =>
        new ForwardParser<TInput, TOutput>(fnParser, term);

    public static Parser<TInput, TOutput> LeftReduce<TOutput>(
        Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser) =>
        new LeftReduceParser<TInput, TOutput>(leftParser, fnRightParser, once: false);

    public static Parser<TInput, TResult> Map<TOutput1, TOutput2, TResult>(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Func<TOutput1, TOutput2, TResult> fnMapper,
        string? term = null) =>
        new SequenceParser<TInput, TResult>(
            new Parser<TInput>[] { parser1, parser2 },
            list => fnMapper((TOutput1)list[0], (TOutput2)list[1]),
            term);

    public static Parser<TInput, TResult> Map<TOutput1, TOutput2, TOutput3, TResult>(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Parser<TInput, TOutput3> parser3,
        Func<TOutput1, TOutput2, TOutput3, TResult> fnMapper,
        string? term = null) =>
        new SequenceParser<TInput, TResult>(
            new Parser<TInput>[] { parser1, parser2, parser3 },
            list => fnMapper((TOutput1)list[0], (TOutput2)list[1], (TOutput3)list[2]),
            term);

    public static Parser<TInput, TResult> Map<TOutput1, TOutput2, TOutput3, TOutput4, TResult>(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Parser<TInput, TOutput3> parser3,
        Parser<TInput, TOutput4> parser4,
        Func<TOutput1, TOutput2, TOutput3, TOutput4, TResult> fnMapper,
        string? term = null) =>
        new SequenceParser<TInput, TResult>(
            new Parser<TInput>[] { parser1, parser2, parser3, parser4 },
            list => fnMapper((TOutput1)list[0], (TOutput2)list[1], (TOutput3)list[2], (TOutput4)list[3]),
            term);

    public static Parser<TInput, TInput> Match(Func<TInput, bool> predicate, string? term = null) =>
        new MatchParser<TInput, TInput>(span => span.Length > 0 && predicate(span[0]) ? 1 : 0, span => span[0], term);

    public static Parser<TInput, TInput> Match(Matcher<TInput> matcher, string? term = null) =>
        new MatchParser<TInput, TInput>(matcher, span => span[0], term);

    public static Parser<TInput, IReadOnlyList<TInput>> MatchAll(
        Matcher<TInput> matcher,
        string? term = null) =>
        new MatchMultiParser<TInput>(matcher, term);

    public static Parser<TInput, IReadOnlyList<TInput>> MatchAll(
        IReadOnlyList<TInput> items,
        Func<TInput, TInput, bool> comparer,
        string? term = null)
    {
        return MatchAll(input =>
        {
            if (input.Length >= items.Count)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (!comparer(input[i], items[i]))
                    {
                        return -1;
                    }
                }

                return items.Count;
            }

            return -1;
        },
        term);
    }

    public static Parser<TInput, TInput> Not(Parser<TInput> parser) =>
        new NotParser<TInput>(parser);

    public static Parser<TInput, IReadOnlyList<TOutput>> OneOrMore<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.OneOrMore();

    public static Parser<TInput, IReadOnlyList<TOutput>> OneOrMore<TOutput>(Parser<TInput, TOutput> parser) =>
        parser.OneOrMore();

    public static Parser<TInput, TOutput> Operators<TOutput>(
        Parser<TInput, TOutput> primaryParser,
        Parser<TInput, TOutput> secondaryParser,
        Action<OperatorsParser<TInput, TOutput>.OperatorBuilder> builder) =>
        new OperatorsParser<TInput, TOutput>(primaryParser, secondaryParser, builder);

    public static Parser<TInput, TOutput> Operators<TOutput>(
        Parser<TInput, TOutput> primaryParser,
        Action<OperatorsParser<TInput, TOutput>.OperatorBuilder> builder) =>
        new OperatorsParser<TInput, TOutput>(primaryParser, primaryParser, builder);

    public static Parser<TInput, TOutput> Optional<TOutput>(Parser<TInput, TOutput> parser) =>
        parser.Optional();

    public static Parser<TInput, IReadOnlyList<TOutput>> Optional<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.Optional();

    public static Parser<TInput, TOutput> Required<TOutput>(Parser<TInput, TOutput> parser, Func<TOutput> fnMissing) =>
        new RequiredParser<TInput, TOutput>(parser, fnMissing);

    public static Parser<TInput, TOutput2> RightReduce<TOutput1, TOutput2>(
        Parser<TInput, TOutput1> leftParser,
        Parser<TInput, TOutput2> rightParser,
        Func<TOutput1, TOutput2, TOutput2> fnAggregator) =>
        new RightReduceParser<TInput, TOutput1, TOutput2>(leftParser, rightParser, fnAggregator);

    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.ZeroOrMore();

    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TOutput>(Parser<TInput, TOutput> parser) =>
        parser.ZeroOrMore();
}