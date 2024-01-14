namespace Parkour.Parsing;
using Parsers;

public static partial class ParserExtensions
{
    /// <summary>
    /// A parser that succeeds if both parsers succeed in sequence.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> leftParser,
        Parser<TInput, IReadOnlyList<TOutput>> rightParser) =>
        new SequenceMultiParser<TInput, TOutput>([leftParser, rightParser]);

    /// <summary>
    /// A parser that succeeds if both parsers succeed in sequence.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Parser<TInput, IReadOnlyList<TOutput>> rightParser) =>
        new SequenceMultiParser<TInput, TOutput>([leftParser.ToMultiParser(), rightParser]);

    /// <summary>
    /// A parser that succeeds if both parsers succeed in sequence.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> leftParser,
        Parser<TInput, TOutput> rightParser) =>
        new SequenceMultiParser<TInput, TOutput>([leftParser, rightParser.ToMultiParser()]);

    /// <summary>
    /// A parser that succeeds if both parsers succeed in sequence.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Parser<TInput, TOutput> rightParser) =>
        new SequenceMultiParser<TInput, TOutput>([leftParser.ToMultiParser(), rightParser.ToMultiParser()]);

    /// <summary>
    /// A parser that applies the output of the left parser to the right parser.
    /// </summary>
    public static Parser<TInput, TOutput2> Apply<TInput, TOutput1, TOutput2>(
        this Parser<TInput, TOutput1> parser,
        Func<Func<TOutput1>, Parser<TInput, TOutput2>> fnNextParser) =>
            new ApplyParser<TInput, TOutput1, TOutput2>(parser, fnNextParser);

    /// <summary>
    /// A parser that applies the output of the left parser optinally to the right parser.
    /// </summary>
    public static Parser<TInput, TOutput> ApplyOptional<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser) =>
        new ApplyRepeatParser<TInput, TOutput>(leftParser, fnRightParser, 0, 1);

    /// <summary>
    /// A parser that applies the output of the left parser to the right parser and 
    /// then repeatedly applies the output of the right parser back to itself
    /// until the right no longer succeeds.
    /// </summary>
    public static Parser<TInput, TOutput> ApplyRepeat<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser) =>
        new ApplyRepeatParser<TInput, TOutput>(leftParser, fnRightParser, 0);

    /// <summary>
    /// A parser that applies the output of the left parser to the right parser and 
    /// then repeatedly applies the output of the right parser back to itself
    /// within the repeatable range.
    /// </summary>
    public static Parser<TInput, TOutput> ApplyRepeat<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser,
        int minCount,
        int maxCount) =>
        new ApplyRepeatParser<TInput, TOutput>(leftParser, fnRightParser, minCount, maxCount);

    /// <summary>
    /// A parser that converts the input items that would be parsed by this parser into a value.
    /// </summary>
    public static Parser<TInput, TOutput> Convert<TInput, TOutput>(
        this Parser<TInput> parser,
        Converter<TInput, TOutput> converter,
        ImmutableList<object>? annotations = null) =>
        new ConvertParser<TInput, TOutput>(parser, converter, annotations);

    /// <summary>
    /// A parser that converts the input items that would be parsed by this parser into a value.
    /// </summary>
    public static Parser<TInput, TOutput> Convert<TInput, TOutput>(
        this Parser<TInput> parser,
        Converter<TInput, TOutput> converter,
        string term) =>
        Convert(parser, converter, [term]);

    /// <summary>
    /// A parser the returns the output of the first parser that would succeed on the same input.
    /// </summary>
    public static Parser<TInput, TOutput> Else<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput, TOutput> nextParser) =>
        new FirstParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the first parser that would succeed on the same input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Else<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, IReadOnlyList<TOutput>> nextParser) =>
        new FirstMultiParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the first parser that would succeed on the same input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Else<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, TOutput> nextParser) =>
        new FirstMultiParser<TInput, TOutput>(parser, nextParser.ToMultiParser());

    /// <summary>
    /// A parser the returns the output of the first parser that would succeed on the same input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Else<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput, IReadOnlyList<TOutput>> nextParser) =>
        new FirstMultiParser<TInput, TOutput>(parser.ToMultiParser(), nextParser);

    /// <summary>
    /// A parser that is invoked only if the condition parser succeeds on a look-ahead scan.
    /// </summary>
    public static Parser<TInput, TOutput> If<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput> conditionParser) =>
        new IfParser<TInput, TOutput>(conditionParser, parser);

    /// <summary>
    /// A parser that is invoked only if the condition parser succeeds on a look-ahead scan.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> If<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput> conditionParser) =>
        new IfMultiParser<TInput, TOutput>(conditionParser, parser.ToMultiParser());

    /// <summary>
    /// A parser that aggregates the output of the first parser with zero or more outputs of the second parser.
    /// This is typically used with postfix and infix operators:  x ++ ++ 
    /// </summary>
    public static Parser<TInput, TOutput1> LeftReduce<TInput, TOutput1, TOutput2>(
        this Parser<TInput, TOutput1> leftParser,
        Parser<TInput, TOutput2> rightParser,
        Func<TOutput1, TOutput2, TOutput1> fnAggregate) =>
        new LeftReduceParser<TInput, TOutput1, TOutput2>(leftParser, rightParser, fnAggregate);

    /// <summary>
    /// A parser that has its input limited to the range identified by the limiter.
    /// </summary>
    public static Parser<TInput, TOutput> Limit<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput> limiter) =>
        new LimitParser<TInput, TOutput>(limiter, parser);

    /// <summary>
    /// A parser that has its input limited to the range identified by the limiter.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Limit<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput> limiter) =>
        new LimitMultiParser<TInput, TOutput>(limiter, parser.ToMultiParser());

    /// <summary>
    /// Maps the output of the parser.
    /// </summary>
    public static Parser<TInput, TOutput2> Map<TInput, TOutput1, TOutput2>(
        this Parser<TInput, TOutput1> parser,
        Func<TOutput1, TOutput2> fnMapper,
        ImmutableList<object>? annotations = null) =>
        new MapParser<TInput, TOutput1, TOutput2>(parser, fnMapper, annotations);

    /// <summary>
    /// A parser that returns a single input item if there negated parser fails.
    /// </summary>
    public static Parser<TInput, TInput> Not<TInput>(
        this Parser<TInput, TInput> parser) =>
        new NotParser<TInput>(parser);

    /// <summary>
    /// A parser that produces one or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> OneOrMore<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.Repeat(minCount: 1);

    /// <summary>
    /// A parser that produces one or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> OneOrMore<TInput, TOutput>(
        this Parser<TInput, TOutput> parser) =>
        parser.ToMultiParser().OneOrMore();

    /// <summary>
    /// A parser that produces the default value if this parser does not succeed.
    /// </summary>
    public static Parser<TInput, TOutput> Optional<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Func<TOutput>? fnMissing = null) =>
        new OptionalParser<TInput, TOutput>(parser, fnMissing);

    /// <summary>
    /// A parser that produces the default value if this parser does not succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Optional<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Func<IEnumerable<TOutput>> fnMissing) =>
        new OptionalMultiParser<TInput, TOutput>(parser, fnMissing);

    /// <summary>
    /// A parser the returns the output of the parser that would consume the most input.
    /// </summary>
    public static Parser<TInput, TOutput> Or<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput, TOutput> nextParser) =>
        new BestParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the parser that would consume the most input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Or<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, IReadOnlyList<TOutput>> nextParser) =>
        new BestMultiParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the parser that would consume the most input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Or<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, TOutput> nextParser) =>
        new BestMultiParser<TInput, TOutput>(parser, nextParser.ToMultiParser());

    /// <summary>
    /// A parser the returns the output of the parser that would consume the most input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Or<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput, IReadOnlyList<TOutput>> nextParser) =>
        new BestMultiParser<TInput, TOutput>(parser.ToMultiParser());

    /// <summary>
    /// Parses zero or more values into a list.
    /// </summary>
    public static ParseIntoResult ParseInto<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        ReadOnlySpan<TInput> input,
        List<TOutput> outputList)
    {
        if (parser is MultiParser<TInput, TOutput> multiParser)
        {
            return multiParser.ParseInto(input, outputList);
        }
        else 
        {
            var result = parser.Parse(input);
            if (result.Success)
            {
                outputList.AddRange(result.Output);
                return new ParseIntoResult(true, result.Length);
            }
        }

        return default;
    }

    /// <summary>
    /// Parses zero or more values into a list.
    /// </summary>
    public static ParseIntoResult ParseInto<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        ReadOnlySpan<TInput> input,
        List<TOutput> outputList)
    {
        var result = parser.Parse(input);
        if (result.Success)
        {
            outputList.Add(result.Output);
            return new ParseIntoResult(true, result.Length);
        }

        return default;
    }

    /// <summary>
    /// A parser that produces a range of outputs from repeatedly applying 
    /// the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Repeat<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser, int minCount, int maxCount = 0) =>
        new RepeatMultiParser<TInput, TOutput>(parser, minCount, maxCount);

    /// <summary>
    /// A parser that produces a range of outputs from repeatedly applying 
    /// the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Repeat<TInput, TOutput>(
        this Parser<TInput, TOutput> parser, int minCount, int maxCout = 0) =>
        parser.ToMultiParser().Repeat(minCount, maxCout);

    /// <summary>
    /// A parser that returns a specified value when this parser fails.
    /// </summary>
    public static Parser<TInput, TOutput> Required<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Func<TOutput> fnMissing) =>
        new OptionalParser<TInput, TOutput>(parser, fnMissing, isRequired: true);

    /// <summary>
    /// A parser that returns a specified value when this parser fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Required<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Func<IReadOnlyList<TOutput>> fnMissing) =>
        new OptionalMultiParser<TInput, TOutput>(parser, fnMissing, isRequired: true);

    /// <summary>
    /// A parser that aggregates zero or more outputs of the first parser with the output of the second parser.
    /// This is typically used with prefix operators:  ++ ++ x
    /// </summary>
    public static Parser<TInput, TOutput2> RightReduce<TInput, TOutput1, TOutput2>(
        this Parser<TInput, TOutput1> leftParser,
        Parser<TInput, TOutput2> rightParser,
        Func<TOutput1, TOutput2, TOutput2> fnAggregator) =>
        new RightReduceParser<TInput, TOutput1, TOutput2>(leftParser, rightParser, fnAggregator);

    /// <summary>
    /// A parser that maps the output of this parser.
    /// This a synonym to Map that can be used with LINQ query expressions.
    /// </summary>
    public static Parser<TInput, TOutput2> Select<TInput, TOutput, TOutput2>(
        this Parser<TInput, TOutput> parser,
        Func<TOutput, TOutput2> selector,
        ImmutableList<object>? annotations = null) =>
        parser.Map(selector, annotations);

    /// <summary>
    /// A parser that succeeds if both parsers succeed and converts the output of both into a new value.
    /// This is a synonym to Apply that can be use with LINQ query expressions.
    /// </summary>
    public static Parser<TInput, TOutput3> SelectMany<TInput, TOutput1, TOutput2, TOutput3>(
        this Parser<TInput, TOutput1> parser,
        Parser<TInput, TOutput2> nextParser,
        Func<TOutput2, TOutput3> fnMapper) =>
        parser.Apply(fnLeft => nextParser).Map(fnMapper);

    /// <summary>
    /// A parser that succeeds if both parsers succeed and maps the output of both into a new value.
    /// </summary>
    public static Parser<TInput, TOutput3> Then<TInput, TOutput1, TOutput2, TOutput3>(
        this Parser<TInput, TOutput1> parser,
        Parser<TInput, TOutput2> nextParser,
        Func<TOutput1, TOutput2, TOutput3> fnMapper) where TInput : notnull =>
        ParserFactory<TInput>.Map(parser, nextParser, fnMapper);

    /// <summary>
    /// Converts a parser that returns s single item into a parser that returns a list of one item.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ToList<TInput, TOutput>(
        this Parser<TInput, TOutput> parser) =>
        new ParserMultiParser<TInput, TOutput>(parser);

    /// <summary>
    /// Returns the same parser that already returns a list.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ToList<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser;

    /// <summary>
    /// Converts a parser into a multiparser
    /// </summary>
    internal static MultiParser<TInput, TOutput> ToMultiParser<TInput, TOutput>(
        this Parser<TInput, TOutput> parser) =>
        new ParserMultiParser<TInput, TOutput>(parser);

    /// <summary>
    /// Converts a parser into a multiparser
    /// </summary>
    internal static MultiParser<TInput, TOutput> ToMultiParser<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser is MultiParser<TInput, TOutput> multiParser
            ? multiParser
            : new ParserMultiParser<TInput, TOutput>(parser);

    /// <summary>
    /// A parser that returns the output of this parser unless it is followed by input that matches the condition.
    /// </summary>
    public static Parser<TInput, TOutput> Unless<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput> condition) =>
        new UnlessParser<TInput, TOutput>(parser, condition);

    /// <summary>
    /// A parser that returns the output of this parser unless it is followed by input that matches the condition.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Unless<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput> condition) =>
        new UnlessMultiParser<TInput, TOutput>(parser.ToMultiParser(), condition);

    /// <summary>
    /// A parser that produces zero or more output.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.Repeat(minCount: 0);

    /// <summary>
    /// A parser that produces zero or more outputs from repeatedly applying the specified parser until it fails.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TInput, TOutput>(
        this Parser<TInput, TOutput> parser) =>
        parser.ToMultiParser().ZeroOrMore();

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext context, IEnumerable<TInput> items, Parser<TInput, TOutput> parser)
        where TInput : notnull
    {
        return new SwitchParserContext<TInput, TOutput>().Case(items, parser);
    }

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext context, IEnumerable<TInput> items, Func<IReadOnlyList<TInput>, TOutput> selector)
        where TInput : notnull
    {
        return new SwitchParserContext<TInput, TOutput>().Case(items, selector);
    }

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext context, string text, Parser<char, TOutput> parser) =>
        Case(context, (IEnumerable<char>)text, parser);

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext context, string text, Func<string, TOutput> selector) =>
        Case(context, (IEnumerable<char>)text, CharParserFactory.Text(text).Select(selector));

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext<TInput, TOutput> context, IEnumerable<TInput> items, Parser<TInput, TOutput> parser)
        where TInput : notnull
    {
        return context.Add(items.ToArray(), parser);
    }

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext<TInput, TOutput> context, IEnumerable<TInput> items, Func<IReadOnlyList<TInput>, TOutput> selector)
        where TInput : notnull
    {
        var list = items.ToArray();
        return context.Add(list, ParserFactory<TInput>.MatchAll(list, EqualityComparer<TInput>.Default).Select(selector));
    }

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext<char, TOutput> context, string text, Parser<char, TOutput> parser) =>
        Case(context, (IEnumerable<char>)text, parser);

    /// <summary>
    /// Constructs a case for a switch parser that matches a specified sequence of items.
    /// </summary>
    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext<char, TOutput> context, string text, Func<string, TOutput> selector) =>
        Case(context, (IEnumerable<char>)text, CharParserFactory.Text(text).Select(selector));
}