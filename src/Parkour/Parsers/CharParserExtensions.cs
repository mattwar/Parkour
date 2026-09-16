namespace Parkour.Parsers;

public static class CharParserExtensions
{
    /// <summary>
    /// Returns a parser that produces the text of the characters consumed by the specified parser.
    /// </summary>
    public static Parser<char, string> Text(this Parser<char> parser) =>
        CharParserFactory.Text(parser);
}