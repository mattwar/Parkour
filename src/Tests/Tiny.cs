using System;
using Parkour;

namespace Tests
{
    using static CharParserFactory;
    using static ParserFactory<char>;

    public static class TokenKinds
    {
        public static readonly string Identifier = nameof(Identifier);
        public static readonly string Number = nameof(Number);
        public static readonly string String = nameof(String);
        public static readonly string OpenParen = nameof(OpenParen);
        public static readonly string CloseParen = nameof(CloseParen);
        public static readonly string Plus = nameof(Plus);
        public static readonly string Dash = nameof(Dash);
        public static readonly string Asterisk = nameof(Asterisk);
        public static readonly string Slash = nameof(Slash);
        public static readonly string Equal = nameof(Equal);
        public static readonly string NotEqual = nameof(NotEqual);
        public static readonly string GreaterThan = nameof(GreaterThan);
        public static readonly string GreaterThanOrEqual = nameof(GreaterThanOrEqual);
        public static readonly string LessThan = nameof(LessThan);
        public static readonly string LessThanOrEqual = nameof(LessThanOrEqual);
        public static readonly string And = nameof(And);
        public static readonly string Or = nameof(Or);
        public static readonly string Not = nameof(Not);
        public static readonly string Unknown = nameof(Unknown);
        public static readonly string EndOfText = nameof(EndOfText);
    }

    public static class TokenTexts
    {
        public static readonly string OpenParen = "(";
        public static readonly string CloseParen = ")";
        public static readonly string Plus = "+";
        public static readonly string Dash = "-";
        public static readonly string Asterisk = "*";
        public static readonly string Slash = "/";
        public static readonly string Equal = "==";
        public static readonly string NotEqual = "!=";
        public static readonly string GreaterThan = ">";
        public static readonly string GreaterThanOrEqual = ">=";
        public static readonly string LessThan = "<";
        public static readonly string LessThanOrEqual = "<=";
        public static readonly string And = "and";
        public static readonly string Or = "or";
        public static readonly string Not = "not";
    }

    public class TinyLexer
    {
        private readonly Parser<char, IReadOnlyList<LexicalToken>> _parser;

        public LexicalToken[] Parse(string text)
        {
            var list = new List<LexicalToken>();

            _parser.ParseInto(text, list, out var remainingInput);

            var consumed = text.Length - remainingInput.Length;
            var remainingText = remainingInput.Length > 0 ? text.Substring(consumed, remainingInput.Length) : "";
            list.Add(new LexicalToken(TokenKinds.EndOfText, remainingText, ""));

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
                        identifier.Select(id => new LexicalToken(TokenKinds.Identifier, fnTrivia(), id)),
                        numericLiteral.Select(num => new LexicalToken(TokenKinds.Number, fnTrivia(), num)),
                        stringLiteral.Select(str => new LexicalToken(TokenKinds.String, fnTrivia(), str)),
                        Text(TokenTexts.OpenParen).Select(tk => new LexicalToken(TokenKinds.OpenParen, fnTrivia(), tk)),
                        Text(TokenTexts.CloseParen).Select(tk => new LexicalToken(TokenKinds.CloseParen, fnTrivia(), tk)),
                        Text(TokenTexts.Plus).Select(tk => new LexicalToken(TokenKinds.Plus, fnTrivia(), tk)),
                        Text(TokenTexts.Dash).Select(tk => new LexicalToken(TokenKinds.Dash, fnTrivia(), tk)),
                        Text(TokenTexts.Asterisk).Select(tk => new LexicalToken(TokenKinds.Asterisk, fnTrivia(), tk)),
                        Text(TokenTexts.Slash).Select(tk => new LexicalToken(TokenKinds.Slash, fnTrivia(), tk)),
                        Text(TokenTexts.Equal).Select(tk => new LexicalToken(TokenKinds.Equal, fnTrivia(), tk)),
                        Text(TokenTexts.NotEqual).Select(tk => new LexicalToken(TokenKinds.NotEqual, fnTrivia(), tk)),
                        Text(TokenTexts.GreaterThan).Select(tk => new LexicalToken(TokenKinds.GreaterThan, fnTrivia(), tk)),
                        Text(TokenTexts.GreaterThanOrEqual).Select(tk => new LexicalToken(TokenKinds.GreaterThanOrEqual, fnTrivia(), tk)),
                        Text(TokenTexts.LessThan).Select(tk => new LexicalToken(TokenKinds.LessThan, fnTrivia(), tk)),
                        Text(TokenTexts.LessThanOrEqual).Select(tk => new LexicalToken(TokenKinds.LessThanOrEqual, fnTrivia(), tk)),
                        Text(TokenTexts.And).Select(tk => new LexicalToken(TokenKinds.And, fnTrivia(), tk)),
                        Text(TokenTexts.Not).Select(tk => new LexicalToken(TokenKinds.Not, fnTrivia(), tk)),
                        Text(TokenTexts.Or).Select(tk => new LexicalToken(TokenKinds.Or, fnTrivia(), tk)),
                        Any.Select(ch => new LexicalToken(TokenKinds.Unknown, fnTrivia(), ch.ToString()))
                        ));

            _parser = token.ZeroOrMore();
        }
    }
}

namespace Tests
{
    using static ParserFactory<LexicalToken>;

    public static class NodeKinds
    {
        public static readonly string Add = nameof(Add);
        public static readonly string Subtract = nameof(Subtract);
        public static readonly string Multiply = nameof(Multiply);
        public static readonly string Divide = nameof(Divide);
        public static readonly string Equal = nameof(Equal);
        public static readonly string NotEqual = nameof(NotEqual);
        public static readonly string LessThan = nameof(LessThan);
        public static readonly string LessThanOrEqual = nameof(LessThanOrEqual);
        public static readonly string GreaterThan = nameof(GreaterThan);
        public static readonly string GreaterThanOrEqual = nameof(GreaterThanOrEqual);
        public static readonly string And = nameof(And);
        public static readonly string Or = nameof(Or);
        public static readonly string Not = nameof(Not);
        public static readonly string Negate = nameof(Negate);
        public static readonly string ParenthesizedExpression = nameof(ParenthesizedExpression);
        public static readonly string Skipped = nameof(Skipped);
        public static readonly string Root = nameof(Root);
    }

    public class TinyParser
    {
        private readonly Parser<LexicalToken, SyntaxElement> _parser;

        public Syntax Parse(string text)
        {
            var lexer = new TinyLexer();
            var tokens = lexer.Parse(text);
            _parser.Parse(tokens.AsSpan(), out var element, out var _);
            return new Syntax(text, _parser, tokens, element);
        }

        public TinyParser()
        {
            Parser<LexicalToken, SyntaxElement> ToToken(Parser<LexicalToken, LexicalToken> parser) =>
                parser.Select(lt => (SyntaxElement) new SyntaxToken(lt));

            Parser<LexicalToken, SyntaxElement> Token(string text) =>
                ToToken(Match(t => t.Text == text, $"'{text}'"));

            Parser<LexicalToken, SyntaxElement> TokenKind(string kind) =>
                ToToken(Match(t => t.Kind == kind, $"<{kind}>"));

            var Identifier = TokenKind(TokenKinds.Identifier);
            var Number = TokenKind(TokenKinds.Number);

            Parser<LexicalToken, SyntaxElement>? expressionCore = null;
            var Expression =
                Forward(() => expressionCore!, "<Expression>");

            var RequiredCloseParen = Required(Token(TokenTexts.CloseParen),
                () => (SyntaxElement)new SyntaxToken(TokenKinds.CloseParen, "", "", new Diagnostic("Missing )")));

            var ParenthesizeExpression =
                Map(Token(TokenTexts.OpenParen), Expression, RequiredCloseParen,
                (open, expr, close) => (SyntaxElement)new SyntaxNode(NodeKinds.ParenthesizedExpression, open, expr, close));

            var Primitive =
                First(
                    Identifier,
                    Number,
                    ParenthesizeExpression);

            var RequiredPrimitive =
                Required(Primitive, CreateMissingExpression);

            var Negative =
                RightReduce(
                    Token(TokenTexts.Dash), RequiredPrimitive,
                    (dash, expr) => new SyntaxNode(NodeKinds.Negate, dash, expr));

            var RequiredNegative =
                Required(Negative, CreateMissingExpression);

            var Multiplicative =
                Negative.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.Asterisk), RequiredNegative,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.Multiply, fnLeft(), op, right)),
                        Map(Token(TokenTexts.Slash), RequiredNegative,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.Divide, fnLeft(), op, right))
                        ));

            var RequiredMultiplicative =
                Required(Multiplicative, CreateMissingExpression);

            var Additive =
                Multiplicative.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.Plus), RequiredMultiplicative,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.Add, fnLeft(), op, right)),
                        Map(Token(TokenTexts.Dash), RequiredMultiplicative,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.Subtract, fnLeft(), op, right))
                        ));

            var RequiredAdditive
                = Required(Additive, CreateMissingExpression);

            var Inequality =
                Additive.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.GreaterThan), RequiredAdditive,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.GreaterThan, fnLeft(), op, right)),
                        Map(Token(TokenTexts.GreaterThanOrEqual), RequiredAdditive,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.GreaterThanOrEqual, fnLeft(), op, right)),
                        Map(Token(TokenTexts.LessThan), RequiredAdditive,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.LessThan, fnLeft(), op, right)),
                        Map(Token(TokenTexts.LessThanOrEqual), RequiredAdditive,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.LessThanOrEqual, fnLeft(), op, right))
                        ));

            var RequiredInequality =
                Required(Inequality, CreateMissingExpression);

            var Equality =
                Inequality.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.Equal), RequiredInequality,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.Equal, fnLeft(), op, right)),
                        Map(Token(TokenTexts.NotEqual), RequiredInequality,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.NotEqual, fnLeft(), op, right))
                        ));

            var RequiredEquality =
                Required(Equality, CreateMissingExpression);

            var LogicalNot =
                RightReduce(
                    Token(TokenTexts.Not), RequiredEquality,
                    (not, exp) => new SyntaxNode(NodeKinds.Not, not, exp));

            var RequiredLogicalNot =
                Required(LogicalNot, CreateMissingExpression);

            var Logical =
                LogicalNot.LeftReduce(fnLeft =>
                    First(
                        Map(Token(TokenTexts.And), RequiredLogicalNot,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.And, fnLeft(), op, right)),
                        Map(Token(TokenTexts.Or), RequiredLogicalNot,
                            (op, right) => (SyntaxElement)new SyntaxNode(NodeKinds.Or, fnLeft(), op, right))
                        ));

            expressionCore = Logical;

            var Skipped = ZeroOrMore(ToToken(Any))
                    .Select(list => (SyntaxElement)new SyntaxNode(NodeKinds.Skipped, list));

            var Root =
                Map(Expression, Skipped,
                    (expr, rest) => (SyntaxElement)new SyntaxNode(NodeKinds.Root, expr, rest));

            _parser = Root;
        }

        public static SyntaxElement CreateMissingExpression()
        {
            return new SyntaxToken(TokenKinds.Identifier, "", "", new Diagnostic("Expression expected"));
        }
    }
} 