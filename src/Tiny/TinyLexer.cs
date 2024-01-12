using Parkour;
using Parkour.Parsing;
using Parkour.Syntax;

namespace Tiny
{
    using static CharParserFactory;
    using static ParserFactory<char>;

    public class TinyLexer
    {
        private readonly Parser<char, IReadOnlyList<LexicalToken>> _parser;

        public LexicalToken[] Parse(string text)
        {
            var list = new List<LexicalToken>();

            var result = _parser.ParseInto(text, list);

            var remainingText = text.Substring(result.Length);
            list.Add(new LexicalToken(TinyTokenKinds.EndOfTextToken, remainingText, ""));

            return list.ToArray();
        }

        public TinyLexer()
        {
            var commentChars =
                Match("//").And(ZeroOrMore(Not(EndOfLine))).And(Optional(EndOfLine));

            var trivia = ZeroOrMore(Whitespace.Else(commentChars)).Text();

            var identifier =
                Letter.And(LetterOrDigit.ZeroOrMore()).Text();

            var numericLiteral =
                OneOrMore(Digit).And(Optional(Match(".").And(OneOrMore(Digit)))).Text();

            var stringEscape =
                Match("\\r").Else(Match("\\n")).Else(Match("\\t"));

            var stringLiteral =
                Match("'").And(ZeroOrMore(Not(Match("'").Else(EndOfLine)))).And(Match("'")).Text();

            var token =
                trivia.Apply(fnTrivia =>
                    Best(
                        numericLiteral.Select(num => new LexicalToken(TinyTokenKinds.NumberToken, fnTrivia(), num)),
                        identifier.Select(id => new LexicalToken(TinyTokenKinds.IdentifierToken, fnTrivia(), id)),
                        stringLiteral.Select(str => new LexicalToken(TinyTokenKinds.StringToken, fnTrivia(), str)),

                        Switch(builder => builder
                            .Case(TinyTokenTexts.OpenParen, tx => new LexicalToken(TinyTokenKinds.OpenParenToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.CloseParen, tx => new LexicalToken(TinyTokenKinds.CloseParenToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Plus, tx => new LexicalToken(TinyTokenKinds.PlusToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Dash, tx => new LexicalToken(TinyTokenKinds.DashToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Asterisk, tx => new LexicalToken(TinyTokenKinds.AsteriskToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Slash, tx => new LexicalToken(TinyTokenKinds.SlashToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Equal, tx => new LexicalToken(TinyTokenKinds.EqualToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.EqualEqual, tx => new LexicalToken(TinyTokenKinds.EqualEqualToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.NotEqual, tx => new LexicalToken(TinyTokenKinds.NotEqualToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.GreaterThan, tx => new LexicalToken(TinyTokenKinds.GreaterThanToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.GreaterThanEqual, tx => new LexicalToken(TinyTokenKinds.GreaterThanEqualToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.LessThan, tx => new LexicalToken(TinyTokenKinds.LessThanToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.LessThanEqual, tx => new LexicalToken(TinyTokenKinds.LessThanEqualToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.And, tx => new LexicalToken(TinyTokenKinds.AndToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Not, tx => new LexicalToken(TinyTokenKinds.NotToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Or, tx => new LexicalToken(TinyTokenKinds.OrToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Let, tx => new LexicalToken(TinyTokenKinds.LetToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Colon, tx => new LexicalToken(TinyTokenKinds.ColonToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.Comma, tx => new LexicalToken(TinyTokenKinds.CommaToken, fnTrivia(), tx))
                            .Case(TinyTokenTexts.QuestionMark, tx => new LexicalToken(TinyTokenKinds.QuestionMarkToken, fnTrivia(), tx))
                            ),

                        Any.Select(ch => new LexicalToken(TinyTokenKinds.UnknownToken, fnTrivia(), ch.ToString()))
                        ));

            _parser = token.ZeroOrMore();
        }
    }
}
