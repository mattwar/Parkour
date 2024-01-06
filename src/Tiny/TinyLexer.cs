using Parkour;
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
            list.Add(new LexicalToken(TokenKinds.EndOfTextToken, remainingText, ""));

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
                        numericLiteral.Select(num => new LexicalToken(TokenKinds.NumberToken, fnTrivia(), num)),
                        identifier.Select(id => new LexicalToken(TokenKinds.IdentifierToken, fnTrivia(), id)),
                        stringLiteral.Select(str => new LexicalToken(TokenKinds.StringToken, fnTrivia(), str)),

                        Switch(builder => builder
                            .Case(TokenTexts.OpenParen, tx => new LexicalToken(TokenKinds.OpenParenToken, fnTrivia(), tx))
                            .Case(TokenTexts.CloseParen, tx => new LexicalToken(TokenKinds.CloseParenToken, fnTrivia(), tx))
                            .Case(TokenTexts.Plus, tx => new LexicalToken(TokenKinds.PlusToken, fnTrivia(), tx))
                            .Case(TokenTexts.Dash, tx => new LexicalToken(TokenKinds.DashToken, fnTrivia(), tx))
                            .Case(TokenTexts.Asterisk, tx => new LexicalToken(TokenKinds.AsteriskToken, fnTrivia(), tx))
                            .Case(TokenTexts.Slash, tx => new LexicalToken(TokenKinds.SlashToken, fnTrivia(), tx))
                            .Case(TokenTexts.Equal, tx => new LexicalToken(TokenKinds.EqualToken, fnTrivia(), tx))
                            .Case(TokenTexts.EqualEqual, tx => new LexicalToken(TokenKinds.EqualEqualToken, fnTrivia(), tx))
                            .Case(TokenTexts.NotEqual, tx => new LexicalToken(TokenKinds.NotEqualToken, fnTrivia(), tx))
                            .Case(TokenTexts.GreaterThan, tx => new LexicalToken(TokenKinds.GreaterThanToken, fnTrivia(), tx))
                            .Case(TokenTexts.GreaterThanEqual, tx => new LexicalToken(TokenKinds.GreaterThanEqualToken, fnTrivia(), tx))
                            .Case(TokenTexts.LessThan, tx => new LexicalToken(TokenKinds.LessThanToken, fnTrivia(), tx))
                            .Case(TokenTexts.LessThanEqual, tx => new LexicalToken(TokenKinds.LessThanEqualToken, fnTrivia(), tx))
                            .Case(TokenTexts.And, tx => new LexicalToken(TokenKinds.AndToken, fnTrivia(), tx))
                            .Case(TokenTexts.Not, tx => new LexicalToken(TokenKinds.NotToken, fnTrivia(), tx))
                            .Case(TokenTexts.Or, tx => new LexicalToken(TokenKinds.OrToken, fnTrivia(), tx))
                            .Case(TokenTexts.Let, tx => new LexicalToken(TokenKinds.LetToken, fnTrivia(), tx))
                            .Case(TokenTexts.Colon, tx => new LexicalToken(TokenKinds.ColonToken, fnTrivia(), tx))
                            .Case(TokenTexts.Comma, tx => new LexicalToken(TokenKinds.CommaToken, fnTrivia(), tx))
                            .Case(TokenTexts.QuestionMark, tx => new LexicalToken(TokenKinds.QuestionMarkToken, fnTrivia(), tx))
                            ),

                        Any.Select(ch => new LexicalToken(TokenKinds.UnknownToken, fnTrivia(), ch.ToString()))
                        ));

            _parser = token.ZeroOrMore();
        }
    }
}
