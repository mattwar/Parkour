namespace Parkour.Parsers;

/// <summary>
/// A set of common small parsers typically used to match characters via scanning.
/// </summary>
public class CharParserFactory
{
    /// <summary>
    /// A parser that matches and produces any digit character.
    /// Typically only used for scanning.
    /// </summary>
    public static Parser<char, char> Digit =
        ParserFactory<char>.Match(char.IsDigit, "<digit>");

    /// <summary>
    /// A parser that matches and produces any common end-of-line character combinations (CR & LF)
    /// Typically only used for scanning.
    /// </summary>
    public static Parser<char, IReadOnlyList<char>> EndOfLine =
        ParserFactory<char>.MatchAll(span =>
        {
            if (span.Length > 0)
            {
                switch (span[0])
                {
                    case '\n':
                        return 1;
                    case '\r':
                        if (span.Length > 1 && span[1] == '\n')
                            return 2;
                        return 1;
                }
            }

            return 0;
        },
        "<end-of-line>");

    /// <summary>
    /// A parser that matches and produces any matching letter character.
    /// Typically only used for scanning.
    /// </summary>
    public static Parser<char, char> Letter =
        ParserFactory<char>.Match(ch => char.IsLetter(ch), "<letter>");

    /// <summary>
    /// A parser that matches and produces any matching letter or digit character.
    /// Typically only used for scanning.
    // </summary>
    public static Parser<char, char> LetterOrDigit =
        ParserFactory<char>.Match(char.IsLetterOrDigit, "<letter-or-digit>");

    /// <summary>
    /// A parser that matches and produces any matching whitespace character.
    /// Typically only used for scanning.
    /// </summary>
    public static Parser<char, char> Whitespace =
        ParserFactory<char>.Match(char.IsWhiteSpace, "<whitespace>");

    /// <summary>
    /// A parser that matches and produces the specified character.
    /// Typically only used for scanning.
    /// </summary>
    public static Parser<char, char> Match(char ch) =>
        ParserFactory<char>.Match(c => c == ch, ch.ToString());

    /// <summary>
    /// A parser that matches and produces the characters in the specfied string sequence.
    /// Typically only used for scanning.
    /// </summary>
    public static Parser<char, IReadOnlyList<char>> Match(string text) =>
        ParserFactory<char>.MatchAll(input =>
        {
            if (input.Length >= text.Length)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (input[i] != text[i])
                    {
                        return -1;
                    }
                }

                return text.Length;
            }

            return -1;
        },
        text);

    /// <summary>
    /// A parser that matches the characters in the specified string, producing the string.
    /// </summary>
    public static Parser<char, string> Text(string text) =>
        Match(text).Convert(span => text, text);

    /// <summary>
    /// Returns a parser that produces the text of the characters consumed by the specified parser.
    /// </summary>
    public static Parser<char, string> Text(Parser<char> parser) =>
        parser.Convert(span => span.ToString());
}