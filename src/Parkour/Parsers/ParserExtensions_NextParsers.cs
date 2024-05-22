namespace Parkour.Parsers;

public static partial class ParserExtensions
{
    /// <summary>
    /// Gets the set of parsers that would be invoked at the specified input position.
    /// </summary>
    public static void GetNextParsers<TInput>(
        this Parser<TInput> parser,       
        ReadOnlySpan<TInput> input,
        int inputIndex,
        Func<Parser<TInput>, bool, bool> filter,
        List<Parser<TInput>> nextParsers)
    {
        // trim input so we don't search beyond this point
        var trimmedInput = input[..Math.Min(input.Length, inputIndex + 2)];
        var trimmedInputLength = trimmedInput.Length;

        parser.Search(input,
            false,
            (parser, parserInput, afterMissing) =>
            {
                var position = trimmedInputLength - parserInput.Length;
                if (position == inputIndex)
                {
                    // don't add terms for items immediately after a missing value
                    if (filter(parser, afterMissing))
                        nextParsers.Add(parser);
                }
            });
    }
}