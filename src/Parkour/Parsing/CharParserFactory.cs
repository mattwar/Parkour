namespace Parkour.Parsing;

public class CharParserFactory
{
    public static Parser<char, char> Digit =
        ParserFactory<char>.Match(char.IsDigit, "<digit>");

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

    public static Parser<char, char> Letter =
        ParserFactory<char>.Match(ch => char.IsLetter(ch), "<letter>");

    public static Parser<char, char> LetterOrDigit =
        ParserFactory<char>.Match(char.IsLetterOrDigit, "<letter-or-digit>");

    public static Parser<char, char> Whitespace =
        ParserFactory<char>.Match(char.IsWhiteSpace, "<whitespace>");

    public static Parser<char, char> Match(char ch) =>
        ParserFactory<char>.Match(c => c == ch, ch.ToString());

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

    public static Parser<char, string> Text(string text) =>
        Match(text).Convert(span => text, text);

    public static Parser<char, string> Text(Parser<char> parser) =>
        parser.Convert(span => span.ToString());
}