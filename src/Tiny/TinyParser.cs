using Parkour;
using Parkour.Parsing;
using Parkour.Syntax;

namespace Tiny
{
    using static ParserFactory<LexicalToken>;

    public class TinyParser
    {
        private readonly Parser<LexicalToken, SyntaxElement> _parser;

        public SyntaxTree Parse(string name, string text)
        {
            var lexer = new TinyLexer();
            var tokens = lexer.Parse(text);
            var result = _parser.Parse(tokens.AsSpan());
            return new SyntaxTree(name, text, _parser, tokens, result.Output);
        }

        public TinyParser()
        {
            Parser<LexicalToken, SyntaxToken> ToToken(Parser<LexicalToken, LexicalToken> parser) =>
                parser.Select(lt => (SyntaxToken)new SyntaxToken(lt));

            Parser<LexicalToken, SyntaxToken> Token(string text) =>
                ToToken(Match(t => t.Text == text, $"'{text}'"));

            Parser<LexicalToken, SyntaxToken> TokenKind(string kind) =>
                ToToken(Match(t => t.Kind == kind, $"<{kind}>"));

            var IdentifierToken = TokenKind(TinyTokenKinds.IdentifierToken);
            var NumberToken = TokenKind(TinyTokenKinds.NumberToken);
            var StringToken = TokenKind(TinyTokenKinds.StringToken);

            var Identifier = Map(IdentifierToken, token => (TinyExpression)new TinyIdentifier(token));
            var StringLiteral = Map(StringToken, token => (TinyExpression)new TinyLiteralString(token));
            var NumberLiteral = Map(NumberToken, token => (TinyExpression)new TinyLiteralNumber(token));

            Parser<LexicalToken, TinyExpression>? expressionCore = null;
            var Expression =
                Forward(() => expressionCore!, "<Expression>");

            var RequiredCloseParen = Required(Token(TinyTokenTexts.CloseParen), TinyFactory.MissingCloseParen);

            var ParenthesizeExpression =
                Map(
                    Token(TinyTokenTexts.OpenParen), Expression, RequiredCloseParen,
                    (open, expr, close) => TinyFactory.ParenthesizedExpression(open, expr, close));

            var Primitive =
                First(
                    Identifier,
                    NumberLiteral,
                    StringLiteral,
                    ParenthesizeExpression);

            var RequiredPrimitive =
                Required(Primitive, TinyFactory.MissingExpression);

            var Negative =
                RightReduce(
                    Token(TinyTokenTexts.Dash), RequiredPrimitive,
                    (dash, expr) => TinyFactory.Negate(dash, expr));

            var RequiredNegative =
                Required(Negative, TinyFactory.MissingExpression);

            var Multiplicative =
                Negative.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TinyTokenTexts.Asterisk), RequiredNegative,
                            (op, right) => TinyFactory.Multiply(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.Slash), RequiredNegative,
                            (op, right) => TinyFactory.Divide(fnLeft(), op, right))
                        ));

            var RequiredMultiplicative =
                Required(Multiplicative, TinyFactory.MissingExpression);

            var Additive =
                Multiplicative.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TinyTokenTexts.Plus), RequiredMultiplicative,
                            (op, right) => TinyFactory.Add(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.Dash), RequiredMultiplicative,
                            (op, right) => TinyFactory.Subtract(fnLeft(), op, right))
                        ));

            var RequiredAdditive
                = Required(Additive, TinyFactory.MissingExpression);

            var Inequality =
                Additive.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TinyTokenTexts.GreaterThan), RequiredAdditive,
                            (op, right) => TinyFactory.GreaterThan(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.GreaterThanEqual), RequiredAdditive,
                            (op, right) => TinyFactory.GreaterThanOrEqual(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.LessThan), RequiredAdditive,
                            (op, right) => TinyFactory.LessThan(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.LessThanEqual), RequiredAdditive,
                            (op, right) => TinyFactory.LessThanOrEqual(fnLeft(), op, right))
                        ));

            var RequiredInequality =
                Required(Inequality, TinyFactory.MissingExpression);

            var Equality =
                Inequality.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TinyTokenTexts.EqualEqual), RequiredInequality,
                            (op, right) => TinyFactory.Equal(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.NotEqual), RequiredInequality,
                            (op, right) => TinyFactory.NotEqual(fnLeft(), op, right))
                        ));

            var RequiredEquality =
                Required(Equality, TinyFactory.MissingExpression);

            var LogicalNot =
                RightReduce(
                    Token(TinyTokenTexts.Not), RequiredEquality,
                    (not, exp) => TinyFactory.Not(not, exp));

            var RequiredLogicalNot =
                Required(LogicalNot, TinyFactory.MissingExpression);

            var Logical =
                LogicalNot.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TinyTokenTexts.And), RequiredLogicalNot,
                            (op, right) => TinyFactory.And(fnLeft(), op, right)),
                        Map(Token(TinyTokenTexts.Or), RequiredLogicalNot,
                            (op, right) => TinyFactory.Or(fnLeft(), op, right))
                        ));

            expressionCore = Logical;

            var Skipped = ZeroOrMore(ToToken(Any))
                    .Select(list => TinyFactory.Skipped(list));

            var Root =
                Map(Expression, Skipped,
                    (expr, remainder) => (SyntaxElement)TinyFactory.Root(expr, remainder));

            _parser = Root;
        }
    }
}
