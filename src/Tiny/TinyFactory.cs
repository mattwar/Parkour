using Parkour;
using Parkour.Diagnostics;
using Parkour.Syntax;

namespace Tiny
{
    public static class TinyFactory
    {
        public static SyntaxElement IdentifierToken(string trivia, string text) =>
            new SyntaxToken(TokenKinds.IdentifierToken, trivia, text);

        public static SyntaxElement IdentifierToken(string text) =>
            IdentifierToken("", text);

        public static SyntaxElement NumberToken(string trivia, string text) =>
            new SyntaxToken(TokenKinds.NumberToken, trivia, text);

        public static SyntaxElement NumberToken(string text) =>
            NumberToken("", text);

        public static SyntaxElement StringToken(string trivia, string text) =>
            new SyntaxToken(TokenKinds.StringToken, trivia, text);

        public static SyntaxElement StringToken(string text) =>
            StringToken("", text);

        public static SyntaxElement PlusToken(string trivia, string text = TokenTexts.Plus) =>
            new SyntaxToken(TokenKinds.PlusToken, trivia, text);

        public static SyntaxElement PlusToken(string text = TokenTexts.Plus) =>
            PlusToken("", text);

        public static SyntaxElement DashToken(string trivia, string text = TokenTexts.Dash) =>
            new SyntaxToken(TokenKinds.DashToken, trivia, text);

        public static SyntaxElement DashToken(string text = TokenTexts.Dash) =>
            DashToken("", text);

        public static SyntaxElement AsteriskToken(string trivia, string text = TokenTexts.Asterisk) =>
            new SyntaxToken(TokenKinds.AsteriskToken, trivia, text);

        public static SyntaxElement AsteriskToken(string text = TokenTexts.Asterisk) =>
            AsteriskToken("", text);

        public static SyntaxElement SlashToken(string trivia, string text = TokenTexts.Slash) =>
            new SyntaxToken(TokenKinds.SlashToken, trivia, text);

        public static SyntaxElement SlashToken(string text = TokenTexts.Slash) =>
            SlashToken("", text);

        public static SyntaxElement EqualEqualToken(string trivia, string text = TokenTexts.EqualEqual) =>
            new SyntaxToken(TokenKinds.EqualEqualToken, trivia, text);

        public static SyntaxElement EqualEqualToken(string text = TokenTexts.EqualEqual) =>
            EqualEqualToken("", text);

        public static SyntaxElement NotEqualToken(string trivia, string text = TokenTexts.NotEqual) =>
            new SyntaxToken(TokenKinds.NotEqualToken, trivia, text);

        public static SyntaxElement NotEqualToken(string text = TokenTexts.NotEqual) =>
            NotEqualToken("", text);

        public static SyntaxElement LessThanToken(string trivia, string text = TokenTexts.LessThan) =>
            new SyntaxToken(TokenKinds.LessThanToken, trivia, text);

        public static SyntaxElement LessThanToken(string text = TokenTexts.LessThan) =>
            LessThanToken("", text);

        public static SyntaxElement LessThanOrEqualToken(string trivia, string text = TokenTexts.LessThanEqual) =>
            new SyntaxToken(TokenKinds.LessThanEqualToken, trivia, text);

        public static SyntaxElement LessThanOrEqualToken(string text = TokenTexts.LessThanEqual) =>
            LessThanToken("", text);

        public static SyntaxElement GreaterThanToken(string trivia, string text = TokenTexts.GreaterThan) =>
            new SyntaxToken(TokenKinds.GreaterThanToken, trivia, text);

        public static SyntaxElement GreaterThanToken(string text = TokenTexts.LessThan) =>
            GreaterThanToken("", text);

        public static SyntaxElement GreaterThanOrEqualToken(string trivia, string text = TokenTexts.GreaterThanEqual) =>
            new SyntaxToken(TokenKinds.GreaterThanEqualToken, trivia, text);

        public static SyntaxElement GreaterThanOrEqualToken(string text = TokenTexts.GreaterThanEqual) =>
            GreaterThanToken("", text);

        public static SyntaxElement AndToken(string trivia, string text = TokenTexts.And) =>
            new SyntaxToken(TokenKinds.AndToken, trivia, text);

        public static SyntaxElement AndToken(string text = TokenTexts.And) =>
            AndToken("", text);

        public static SyntaxElement OrToken(string trivia, string text = TokenTexts.Or) =>
            new SyntaxToken(TokenKinds.OrToken, trivia, text);

        public static SyntaxElement OrToken(string text = TokenTexts.Or) =>
            OrToken("", text);

        public static SyntaxElement NotToken(string trivia, string text = TokenTexts.Not) =>
            new SyntaxToken(TokenKinds.NotToken, trivia, text);

        public static SyntaxElement NotToken(string text = TokenTexts.Not) =>
            NotToken("", text);

        public static SyntaxElement EndOfTextToken(string trivia = "") =>
            new SyntaxToken(TokenKinds.EndOfTextToken, trivia, "");

        public static SyntaxElement Add(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.Add, new[] { left, op, right });

        public static SyntaxElement Subtract(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.Subtract, new[] { left, op, right });

        public static SyntaxElement Multiply(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.Multiply, new[] { left, op, right });

        public static SyntaxElement Divide(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.Divide, new[] { left, op, right });

        public static SyntaxElement Equal(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.Equal, new[] { left, op, right });

        public static SyntaxElement NotEqual(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.NotEqual, new[] { left, op, right });

        public static SyntaxElement LessThan(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.LessThan, new[] { left, op, right });

        public static SyntaxElement LessThanOrEqual(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.LessThanOrEqual, new[] { left, op, right });

        public static SyntaxElement GreaterThan(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.GreaterThan, new[] { left, op, right });

        public static SyntaxElement GreaterThanOrEqual(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.GreaterThanOrEqual, new[] { left, op, right });

        public static SyntaxElement And(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.And, new[] { left, op, right });

        public static SyntaxElement Or(SyntaxElement left, SyntaxElement op, SyntaxElement right) =>
            new SyntaxList(NodeKinds.Or, new[] { left, op, right });

        public static SyntaxElement Not(SyntaxElement not, SyntaxElement operand) =>
            new SyntaxList(NodeKinds.Not, new[] { not, operand });

        public static SyntaxElement Negate(SyntaxElement negation, SyntaxElement operand) =>
            new SyntaxList(NodeKinds.Negate, new[] { negation, operand });

        public static SyntaxElement ParenthesizedExpression(SyntaxElement open, SyntaxElement expression, SyntaxElement close) =>
            new SyntaxList(NodeKinds.ParenthesizedExpression, new[] { open, expression, close });

        public static SyntaxElement Skipped(IEnumerable<SyntaxElement> skipped) =>
            new SyntaxList(NodeKinds.Skipped, skipped);

        public static SyntaxElement Skipped(params SyntaxElement[] skipped) =>
            Skipped((IEnumerable<SyntaxElement>)skipped);

        public static SyntaxElement Root(SyntaxElement expression, SyntaxElement? remainder = null) =>
            new SyntaxList(NodeKinds.Root, expression, remainder ?? Skipped(EndOfTextToken("")));

        public static SyntaxElement MissingExpression() =>
            new SyntaxToken(TokenKinds.IdentifierToken, "", "", new Diagnostic("Expression expected"));

        public static SyntaxElement MissingCloseParen() =>
            new SyntaxToken(TokenKinds.CloseParenToken, "", "", new Diagnostic("Missing )"));
    }
}
