namespace Parkour.Parsing;
using Parsers;

public static class ParserFactory<TInput> where TInput : notnull
{
    /// <summary>
    /// A parser that always succeeds with the specified output, without consuming any input.
    /// </summary>
    public static Parser<TInput, TOutput> Always<TOutput>(Func<TOutput> fnOutput) =>
        new AlwaysParser<TInput, TOutput>(fnOutput);

    /// <summary>
    /// A parser that always succeeds with the specified output, without consuming any input.
    /// </summary>
    public static Parser<TInput, TOutput> Always<TOutput>(TOutput outputItem) =>
        new AlwaysParser<TInput, TOutput>(() => outputItem);

    /// <summary>
    /// A parser that matches one item of input (producing the input as output) as long as any input exists.
    /// </summary>
    public static Parser<TInput, TInput> Any =
        Match(span => span.Length > 0 ? 1 : 0, "<any>");

    /// <summary>
    /// A parser that produces that output from the parser that would consume the most input.
    /// </summary>
    public static Parser<TInput, TOutput> Best<TOutput>(params Parser<TInput, TOutput>[] parsers) =>
        new BestParser<TInput, TOutput>(parsers);

    /// <summary>
    /// A parser that produces that output from the parser that would consume the most input.
    /// </summary>
    public static Parser <TInput, IReadOnlyList<TOutput>> Best<TOutput>(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers) =>
        new BestMultiParser<TInput, TOutput>(parsers);

    /// <summary>
    /// A parser that produces the output of the first parser that would succeed.
    /// </summary>
    public static Parser<TInput, TOutput> First<TOutput>(params Parser<TInput, TOutput>[] parsers) =>
        new FirstParser<TInput, TOutput>(parsers);

    /// <summary>
    /// A parser that produces the output of the first parser that would succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> First<TOutput>(params Parser<TInput, IReadOnlyList<TOutput>>[] parsers) =>
        new FirstMultiParser<TInput, TOutput>(parsers);

    /// <summary>
    /// A parser that forwards to another parser. 
    /// This is typically used to form cycles in the grammar.
    /// </summary>
    public static Parser<TInput, TOutput> Forward<TOutput>(Func<Parser<TInput, TOutput>> fnParser, ImmutableList<object>? annotations = null) =>
        new ForwardParser<TInput, TOutput>(fnParser, annotations);

    /// <summary>
    /// A parser that forwards to another parser. 
    /// This is typically used to form cycles in the grammar.
    /// </summary>
    public static Parser<TInput, TOutput> Forward<TOutput>(Func<Parser<TInput, TOutput>> fnParser, string term) =>
        Forward(fnParser, [term]);

    /// <summary>
    /// A parser attempts to parse the second parser only if the first condition parser would have succeeded.
    /// </summary>
    public static Parser<TInput, TOutput> If<TOutput>(Parser<TInput> conditionParser, Parser<TInput, TOutput> parser) =>
        new IfParser<TInput, TOutput>(conditionParser, parser);

    /// <summary>
    /// A parser attempts to parse the second parser only if the first condition parser would have succeeded.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> If<TOutput>(Parser<TInput> conditionParser, Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        new IfMultiParser<TInput, TOutput>(conditionParser, parser.ToMultiParser());

    /// <summary>
    /// A parser that aggregates the output of the first parser with zero or more outputs of the second parser.
    /// This is typically used with postfix operators: x ++ ++ 
    /// </summary>
    public static Parser<TInput, TOutput1> LeftReduce<TOutput1, TOutput2>(
        Parser<TInput, TOutput1> leftParser,
        Parser<TInput, TOutput2> rightParser,
        Func<TOutput1, TOutput2, TOutput1> fnAggregate) =>
        new LeftReduceParser<TInput, TOutput1, TOutput2>(leftParser, rightParser, fnAggregate);

    /// <summary>
    /// A parser that maps the output of all parsers, if all parsers succeed in succession.
    /// </summary>
    public static Parser<TInput, TResult> Map<TOutput, TResult>(
        Parser<TInput, TOutput> parser,
        Func<TOutput, TResult> fnMapper,
        ImmutableList<object>? annotations = null) =>
        new MapParser<TInput, TOutput, TResult>(parser, fnMapper, annotations);

    /// <summary>
    /// A parser that maps the output of all parsers, if all parsers succeed in succession.
    /// </summary>
    public static Parser<TInput, TResult> Map<TOutput1, TOutput2, TResult>(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Func<TOutput1, TOutput2, TResult> fnMapper,
        ImmutableList<object>? annotations = null) =>
        new SequenceParser<TInput, TResult>(
            new Parser<TInput>[] { parser1, parser2 },
            list => fnMapper((TOutput1)list[0], (TOutput2)list[1]),
            annotations);

    /// <summary>
    /// A parser that maps the output of all parsers, if all parsers succeed in succession.
    /// </summary>
    public static Parser<TInput, TResult> Map<TOutput1, TOutput2, TOutput3, TResult>(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Parser<TInput, TOutput3> parser3,
        Func<TOutput1, TOutput2, TOutput3, TResult> fnMapper,
        ImmutableList<object>? annotations = null) =>
        new SequenceParser<TInput, TResult>(
            new Parser<TInput>[] { parser1, parser2, parser3 },
            list => fnMapper((TOutput1)list[0], (TOutput2)list[1], (TOutput3)list[2]),
            annotations);

    /// <summary>
    /// A parser that maps the output of all parsers, if all parsers succeed in succession.
    /// </summary>
    public static Parser<TInput, TResult> Map<TOutput1, TOutput2, TOutput3, TOutput4, TResult>(
        Parser<TInput, TOutput1> parser1,
        Parser<TInput, TOutput2> parser2,
        Parser<TInput, TOutput3> parser3,
        Parser<TInput, TOutput4> parser4,
        Func<TOutput1, TOutput2, TOutput3, TOutput4, TResult> fnMapper,
        ImmutableList<object>? annotations = null) =>
        new SequenceParser<TInput, TResult>(
            new Parser<TInput>[] { parser1, parser2, parser3, parser4 },
            list => fnMapper((TOutput1)list[0], (TOutput2)list[1], (TOutput3)list[2], (TOutput4)list[3]),
            annotations);

    /// <summary>
    /// A parser that outputs the next input if the next input item matches the predicate.
    /// </summary>
    public static Parser<TInput, TInput> Match(Func<TInput, bool> predicate, ImmutableList<object>? annotations = null) =>
        new MatchParser<TInput, TInput>(span => span.Length > 0 && predicate(span[0]) ? 1 : -1, span => span[0], annotations);

    /// <summary>
    /// A parser that outputs the next input if the next input item matches the predicate.
    /// </summary>
    public static Parser<TInput, TInput> Match(Func<TInput, bool> predicate, string term) =>
        Match(predicate, [term]);

    /// <summary>
    /// A parser that outputs the next input if the next input item matches the matcher.
    /// The matcher has visibility to all the remaining input items.
    /// </summary>
    public static Parser<TInput, TInput> Match(Matcher<TInput> matcher, ImmutableList<object>? annotations = null) =>
        new MatchParser<TInput, TInput>(matcher, span => span[0], annotations);

    /// <summary>
    /// A parser that outputs the next input if the next input item matches the matcher.
    /// The matcher has visibility to all the remaining input items.
    /// </summary>
    public static Parser<TInput, TInput> Match(Matcher<TInput> matcher, string term) =>
        Match(matcher, [term]);

    /// <summary>
    /// A parser that converts all the next matching input items into its output.
    /// </summary>
    public static Parser<TInput, TOutput> Match<TOutput>(Matcher<TInput> matcher, Converter<TInput, TOutput> converter, ImmutableList<object>? annotations = null) =>
        new MatchParser<TInput, TOutput>(matcher, converter, annotations);

    /// <summary>
    /// A parser that converts all the next matching input items into its output.
    /// </summary>
    public static Parser<TInput, TOutput> Match<TOutput>(Matcher<TInput> matcher, Converter<TInput, TOutput> converter, string term) =>
        Match(matcher, converter, [term]);

    /// <summary>
    /// A parser that outputs all the next matching input items.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TInput>> MatchAll(
        Matcher<TInput> matcher,
        ImmutableList<object>? annotations = null) =>
        new MatchMultiParser<TInput>(matcher, annotations);

    /// <summary>
    /// A parser that outputs all the next matching input items.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TInput>> MatchAll(
        Matcher<TInput> matcher,
        string term) =>
        MatchAll(matcher, [term]);

    /// <summary>
    /// A parser that outputs all the next input items that match the explicit sequence of specified items.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TInput>> MatchAll(
        IReadOnlyList<TInput> items,
        EqualityComparer<TInput> comparer,
        ImmutableList<object>? annotations = null)
    {
        return MatchAll(input =>
        {
            if (input.Length >= items.Count)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (!comparer.Equals(input[i], items[i]))
                    {
                        return 0;
                    }
                }

                return items.Count;
            }

            return 0;
        },
        annotations);
    }

    /// <summary>
    /// A parser that outputs all the next input items that match the explicit sequence of specified items.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TInput>> MatchAll(
        IReadOnlyList<TInput> items,
        EqualityComparer<TInput> comparer,
        string term) =>
        MatchAll(items, comparer, [term]);

    public static Parser<TInput, TInput> Not(Parser<TInput> parser) =>
        new NotParser<TInput>(parser);

    /// <summary>
    /// A parser that produces one or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> OneOrMore<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.OneOrMore();

    /// <summary>
    /// A parser that produces one or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
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

    /// <summary>
    /// A parser that succeeds even if the specified parser fails, instead returning the missing value or null/default.
    /// </summary>
    public static Parser<TInput, TOutput> Optional<TOutput>(Parser<TInput, TOutput> parser, Func<TOutput>? fnMissing = null) =>
        parser.Optional();

    /// <summary>
    /// A parser that succeeds even if the specified parser fails, instead returning the missing value or default.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Optional<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.Optional();

    /// <summary>
    /// A parser that succeeds even if the specified parser fails, instead returning the missing value.
    /// </summary>
    public static Parser<TInput, TOutput> Required<TOutput>(Parser<TInput, TOutput> parser, Func<TOutput> fnMissing) =>
        parser.Required(fnMissing);

    /// <summary>
    /// A parser that succeeds even if the specified parser fails, instead returning the missing value.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Required<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser, Func<IReadOnlyList<TOutput>> fnMissing) =>
        parser.Required(fnMissing);

    /// <summary>
    /// A parser that aggregates zero or more outputs of the first parser with the output of the second parser.
    /// This is typically used with prefix operators:  ++ ++ x
    /// </summary>
    public static Parser<TInput, TOutput2> RightReduce<TOutput1, TOutput2>(
        Parser<TInput, TOutput1> leftParser,
        Parser<TInput, TOutput2> rightParser,
        Func<TOutput1, TOutput2, TOutput2> fnAggregator) =>
        new RightReduceParser<TInput, TOutput1, TOutput2>(leftParser, rightParser, fnAggregator);

    /// <summary>
    /// A parser that switches to one or more parsers based on the current input 
    /// matching a sequence of one or more literal values.
    /// </summary>
    public static Parser<TInput, TOutput> Switch<TOutput>(
        Func<SwitchParserContext, SwitchParserContext<TInput, TOutput>> fnBuilder,
        EqualityComparer<TInput> comparer) =>
        new SwitchParser<TInput, TOutput>(fnBuilder, comparer);

    /// <summary>
    /// A parser that switches to one or more parsers based on the current input 
    /// matching a sequence of one or more literal values.
    /// </summary>
    public static Parser<TInput, TOutput> Switch<TOutput>(
        Func<SwitchParserContext, SwitchParserContext<TInput, TOutput>> fnBuilder) =>
        new SwitchParser<TInput, TOutput>(fnBuilder, EqualityComparer<TInput>.Default);

    /// <summary>
    /// A parser that produces zero or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TOutput>(Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.ZeroOrMore();

    /// <summary>
    /// A parser that produces zero or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TOutput>(Parser<TInput, TOutput> parser) =>
        parser.ZeroOrMore();
}