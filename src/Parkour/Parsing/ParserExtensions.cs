namespace Parkour;
using Parsers;

public static partial class ParserExtensions
{
    /// <summary>
    /// A parser that succeeds if both parsers succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> leftParser,
        Parser<TInput, IReadOnlyList<TOutput>> rightParser) =>
        new AndMultiParser<TInput, TOutput>(leftParser, rightParser);

    /// <summary>
    /// A parser that succeeds if both parsers succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Parser<TInput, IReadOnlyList<TOutput>> rightParser) =>
        new AndMultiParser<TInput, TOutput>(leftParser.ToMultiParser(), rightParser);

    /// <summary>
    /// A parser that succeeds if both parsers succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> leftParser,
        Parser<TInput, TOutput> rightParser) =>
        new AndMultiParser<TInput, TOutput>(leftParser, rightParser.ToMultiParser());

    /// <summary>
    /// A parser that succeeds if both parsers succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> And<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Parser<TInput, TOutput> rightParser) =>
        new AndMultiParser<TInput, TOutput>(leftParser.ToMultiParser(), rightParser.ToMultiParser());

    /// <summary>
    /// A parser that applies the output of the left parser to the right parser and converts both outputs into a new value.
    /// </summary>
    public static Parser<TInput, TOutput3> Apply<TInput, TOutput1, TOutput2, TOutput3>(
        this Parser<TInput, TOutput1> parser,
        Func<Func<TOutput1>, Parser<TInput, TOutput2>> fnNextParser,
        Func<TOutput1, TOutput2, TOutput3> fnMapper) =>
        new ApplyParser<TInput, TOutput1, TOutput2, TOutput3>(parser, fnNextParser, fnMapper);

    /// <summary>
    /// A parser that applies the output of the left parser to the right parser.
    /// </summary>
    public static Parser<TInput, TOutput2> Apply<TInput, TOutput1, TOutput2>(
        this Parser<TInput, TOutput1> leftParser,
        Func<Func<TOutput1>, Parser<TInput, TOutput2>> fnRightParser) =>
        Apply(leftParser, fnRightParser, (output1, output2) => output2);

    /// <summary>
    /// A parser that applies the output of the left parser optinally to the right parser.
    /// This parse is the same as LeftReduce.
    /// </summary>
    public static Parser<TInput, TOutput> ApplyOptional<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser) =>
        new LeftReduceParser<TInput, TOutput>(leftParser, fnRightParser, once: true);

    /// <summary>
    /// A parser that applies the output of the left parser to the rigth parser and 
    /// then repeatedly applies the output of the right parser back to itself
    /// until the right no longer succeeds.
    /// This parser is the same as LeftReduce.
    /// </summary>
    public static Parser<TInput, TOutput> ApplyRepeat<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser) =>
        new LeftReduceParser<TInput, TOutput>(leftParser, fnRightParser, once: false);

    /// <summary>
    /// A parser that converts the input items that would be parsed by this parser into a value.
    /// </summary>
    public static Parser<TInput, TOutput> Convert<TInput, TOutput>(
        this Parser<TInput> parser,
        Converter<TInput, TOutput> converter,
        string? term = null) =>
        new ConvertParser<TInput, TOutput>(parser, converter, term);

    /// <summary>
    /// A parser the returns the output of the first parser to succeed.
    /// </summary>
    public static Parser<TInput, TOutput> Else<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput, TOutput> nextParser) =>
        new FirstParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the first parser to succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Else<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, IReadOnlyList<TOutput>> nextParser) =>
        new FirstMultiParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the first parser to succeed.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Else<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, TOutput> nextParser) =>
        new FirstMultiParser<TInput, TOutput>(parser, nextParser.ToMultiParser());

    /// <summary>
    /// A parser the returns the output of the first parser to succeed.
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
    /// A parser that applies the output of the left parser to the rigth parser and 
    /// then repeatedly applies the output of the right parser back to itself
    /// until the right no longer succeeds.
    /// </summary>
    public static Parser<TInput, TOutput> LeftReduce<TInput, TOutput>(
        this Parser<TInput, TOutput> leftParser,
        Func<Func<TOutput>, Parser<TInput, TOutput>> fnRightParser,
        bool once = false) =>
        new LeftReduceParser<TInput, TOutput>(leftParser, fnRightParser, once);

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
    /// A parser that returns a single input item if there negated parser fails.
    /// </summary>
    public static Parser<TInput, TInput> Not<TInput>(
        this Parser<TInput, TInput> parser) =>
        new NotParser<TInput>(parser);

    /// <summary>
    /// A parser that produces one or more output.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> OneOrMore<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser) =>
        parser.Repeat(minCount: 1);

    /// <summary>
    /// A parser that produces one or more output.
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
    /// A parser the returns the output of the parser that consumes the most input.
    /// </summary>
    public static Parser<TInput, TOutput> Or<TInput, TOutput>(
        this Parser<TInput, TOutput> parser,
        Parser<TInput, TOutput> nextParser) =>
        new BestParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the parser that consumes the most input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Or<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, IReadOnlyList<TOutput>> nextParser) =>
        new BestMultiParser<TInput, TOutput>(parser, nextParser);

    /// <summary>
    /// A parser the returns the output of the parser that consumes the most input.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Or<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser,
        Parser<TInput, TOutput> nextParser) =>
        new BestMultiParser<TInput, TOutput>(parser, nextParser.ToMultiParser());

    /// <summary>
    /// A parser the returns the output of the parser that consumes the most input.
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
    /// A parser that produces one or more output.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> Repeat<TInput, TOutput>(
        this Parser<TInput, IReadOnlyList<TOutput>> parser, int minCount, int maxCount = 0) =>
        new RepeatMultiParser<TInput, TOutput>(parser, minCount, maxCount);

    /// <summary>
    /// A parser that produces one or more output.
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
    /// A parser that repeatedly applies left parser output values with the output of the right parser.
    /// </summary>
    public static Parser<TInput, TOutput2> RightReduce<TInput, TOutput1, TOutput2>(
        this Parser<TInput, TOutput1> leftParser,
        Parser<TInput, TOutput2> rightParser,
        Func<TOutput1, TOutput2, TOutput2> fnAggregator) =>
        new RightReduceParser<TInput, TOutput1, TOutput2>(leftParser, rightParser, fnAggregator);

    /// <summary>
    /// A parser that converts the output of this parser into a new value.
    /// </summary>
    public static Parser<TInput, TOutput2> Select<TInput, TOutput, TOutput2>(
        this Parser<TInput, TOutput> parser,
        Func<TOutput, TOutput2> selector) =>
        new SelectParser<TInput, TOutput, TOutput2>(parser, selector);

    /// <summary>
    /// A parser that succeeds if both parsers succeed and converts the output of both into a new value.
    /// This is a synonym for working with LINQ.
    /// </summary>
    public static Parser<TInput, TOutput3> SelectMany<TInput, TOutput1, TOutput2, TOutput3>(
        this Parser<TInput, TOutput1> parser,
        Parser<TInput, TOutput2> nextParser,
        Func<TOutput1, TOutput2, TOutput3> fnMapper) =>
        new ApplyParser<TInput, TOutput1, TOutput2, TOutput3>(parser, _ => nextParser, fnMapper);

    /// <summary>
    /// A parser that succeeds if both parsers succeed and converts the output of both into a new value.
    /// </summary>
    public static Parser<TInput, TOutput3> Then<TInput, TOutput1, TOutput2, TOutput3>(
        this Parser<TInput, TOutput1> parser,
        Parser<TInput, TOutput2> nextParser,
        Func<TOutput1, TOutput2, TOutput3> fnMapper) =>
        new ApplyParser<TInput, TOutput1, TOutput2, TOutput3>(parser, _ => nextParser, fnMapper);

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
    /// A parser that produces zero or more output.
    /// </summary>
    public static Parser<TInput, IReadOnlyList<TOutput>> ZeroOrMore<TInput, TOutput>(
        this Parser<TInput, TOutput> parser) =>
        parser.ToMultiParser().ZeroOrMore();



    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext context, IEnumerable<TInput> items, Parser<TInput, TOutput> parser)
        where TInput : notnull
    {
        return new SwitchParserContext<TInput, TOutput>().Case(items, parser);
    }

    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext context, IEnumerable<TInput> items, Func<IReadOnlyList<TInput>, TOutput> selector)
        where TInput : notnull
    {
        return new SwitchParserContext<TInput, TOutput>().Case(items, selector);
    }

    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext context, string text, Parser<char, TOutput> parser) =>
        Case(context, (IEnumerable<char>)text, parser);

    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext context, string text, Func<string, TOutput> selector) =>
        Case(context, (IEnumerable<char>)text, CharParserFactory.Text(text).Select(selector));

    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext<TInput, TOutput> context, IEnumerable<TInput> items, Parser<TInput, TOutput> parser)
        where TInput : notnull
    {
        return context.Add(items.ToArray(), parser);
    }

    public static SwitchParserContext<TInput, TOutput> Case<TInput, TOutput>(this SwitchParserContext<TInput, TOutput> context, IEnumerable<TInput> items, Func<IReadOnlyList<TInput>, TOutput> selector)
        where TInput : notnull
    {
        var list = items.ToArray();
        return context.Add(list, ParserFactory<TInput>.MatchAll(list, EqualityComparer<TInput>.Default).Select(selector));
    }

    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext<char, TOutput> context, string text, Parser<char, TOutput> parser) =>
        Case(context, (IEnumerable<char>)text, parser);

    public static SwitchParserContext<char, TOutput> Case<TOutput>(this SwitchParserContext<char, TOutput> context, string text, Func<string, TOutput> selector) =>
        Case(context, (IEnumerable<char>)text, CharParserFactory.Text(text).Select(selector));

}