namespace Parkour.Parsing;

public static class CharParserExtensions
{
    public static Parser<char, string> Text(this Parser<char> parser) =>
        CharParserFactory.Text(parser);
}