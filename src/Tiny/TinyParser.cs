using Parkour;
using Parkour.Parsing;
using Parkour.Syntax;

namespace Tiny
{
    using static ParserFactory<LexicalToken>;

    public class TinyParser
    {
        private readonly Parser<LexicalToken, SyntaxElement> _parser;

        public SyntaxTree Parse(string text)
        {
            var lexer = new TinyLexer();
            var tokens = lexer.Parse(text);
            var result = _parser.Parse(tokens.AsSpan());
            return new SyntaxTree(text, _parser, tokens, result.Output);
        }

        public TinyParser()
        {
            Parser<LexicalToken, SyntaxElement> ToToken(Parser<LexicalToken, LexicalToken> parser) =>
                parser.Select(lt => (SyntaxElement)new SyntaxToken(lt));

            Parser<LexicalToken, SyntaxElement> Token(string text) =>
                ToToken(Match(t => t.Text == text, $"'{text}'"));

            Parser<LexicalToken, SyntaxElement> TokenKind(string kind) =>
                ToToken(Match(t => t.Kind == kind, $"<{kind}>"));

            var Identifier = TokenKind(TokenKinds.IdentifierToken);
            var Number = TokenKind(TokenKinds.NumberToken);
            var String = TokenKind(TokenKinds.StringToken);

            Parser<LexicalToken, SyntaxElement>? expressionCore = null;
            var Expression =
                Forward(() => expressionCore!, "<Expression>");

            var RequiredCloseParen = Required(Token(TokenTexts.CloseParen), TinyFactory.MissingCloseParen);

            var ParenthesizeExpression =
                Map(
                    Token(TokenTexts.OpenParen), Expression, RequiredCloseParen,
                    (open, expr, close) => TinyFactory.ParenthesizedExpression(open, expr, close));

            var Primitive =
                First(
                    Identifier,
                    Number,
                    String,
                    ParenthesizeExpression);

            var RequiredPrimitive =
                Required(Primitive, TinyFactory.MissingExpression);

            var Negative =
                RightReduce(
                    Token(TokenTexts.Dash), RequiredPrimitive,
                    (dash, expr) => TinyFactory.Negate(dash, expr));

            var RequiredNegative =
                Required(Negative, TinyFactory.MissingExpression);

            var Multiplicative =
                Negative.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.Asterisk), RequiredNegative,
                            (op, right) => TinyFactory.Multiply(fnLeft(), op, right)),
                        Map(Token(TokenTexts.Slash), RequiredNegative,
                            (op, right) => TinyFactory.Divide(fnLeft(), op, right))
                        ));

            var RequiredMultiplicative =
                Required(Multiplicative, TinyFactory.MissingExpression);

            var Additive =
                Multiplicative.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.Plus), RequiredMultiplicative,
                            (op, right) => TinyFactory.Add(fnLeft(), op, right)),
                        Map(Token(TokenTexts.Dash), RequiredMultiplicative,
                            (op, right) => TinyFactory.Subtract(fnLeft(), op, right))
                        ));

            var RequiredAdditive
                = Required(Additive, TinyFactory.MissingExpression);

            var Inequality =
                Additive.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.GreaterThan), RequiredAdditive,
                            (op, right) => TinyFactory.GreaterThan(fnLeft(), op, right)),
                        Map(Token(TokenTexts.GreaterThanEqual), RequiredAdditive,
                            (op, right) => TinyFactory.GreaterThanOrEqual(fnLeft(), op, right)),
                        Map(Token(TokenTexts.LessThan), RequiredAdditive,
                            (op, right) => TinyFactory.LessThan(fnLeft(), op, right)),
                        Map(Token(TokenTexts.LessThanEqual), RequiredAdditive,
                            (op, right) => TinyFactory.LessThanOrEqual(fnLeft(), op, right))
                        ));

            var RequiredInequality =
                Required(Inequality, TinyFactory.MissingExpression);

            var Equality =
                Inequality.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.EqualEqual), RequiredInequality,
                            (op, right) => TinyFactory.Equal(fnLeft(), op, right)),
                        Map(Token(TokenTexts.NotEqual), RequiredInequality,
                            (op, right) => TinyFactory.NotEqual(fnLeft(), op, right))
                        ));

            var RequiredEquality =
                Required(Equality, TinyFactory.MissingExpression);

            var LogicalNot =
                RightReduce(
                    Token(TokenTexts.Not), RequiredEquality,
                    (not, exp) => TinyFactory.Not(not, exp));

            var RequiredLogicalNot =
                Required(LogicalNot, TinyFactory.MissingExpression);

            var Logical =
                LogicalNot.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.And), RequiredLogicalNot,
                            (op, right) => TinyFactory.And(fnLeft(), op, right)),
                        Map(Token(TokenTexts.Or), RequiredLogicalNot,
                            (op, right) => TinyFactory.Or(fnLeft(), op, right))
                        ));

            expressionCore = Logical;

            var Skipped = ZeroOrMore(ToToken(Any))
                    .Select(list => TinyFactory.Skipped(list));

            var Root =
                Map(Expression, Skipped,
                    (expr, remainder) => TinyFactory.Root(expr, remainder));

            _parser = Root;
        }
    }
}
