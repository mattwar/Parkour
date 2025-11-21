using Parkour;
using Parkour.Syntax;

namespace Tiny
{
    public static class TinyFactory
    {

        public static SyntaxToken IdentifierToken(string trivia, string text) =>
            new TinyToken(TinyTokenKinds.IdentifierToken, trivia, text);

        public static SyntaxToken IdentifierToken(string text) =>
            IdentifierToken("", text);

        public static SyntaxToken NumberToken(string trivia, string text) =>
            new TinyToken(TinyTokenKinds.NumberToken, trivia, text);

        public static SyntaxToken NumberToken(string text) =>
            NumberToken("", text);

        public static SyntaxToken StringToken(string trivia, string text) =>
            new TinyToken(TinyTokenKinds.StringToken, trivia, text);

        public static SyntaxToken StringToken(string text) =>
            StringToken("", text);

        public static SyntaxToken PlusToken(string trivia, string text = TinyTokenTexts.Plus) =>
            new TinyToken(TinyTokenKinds.PlusToken, trivia, text);

        public static SyntaxToken PlusToken(string text = TinyTokenTexts.Plus) =>
            PlusToken("", text);

        public static SyntaxToken DashToken(string trivia, string text = TinyTokenTexts.Dash) =>
            new TinyToken(TinyTokenKinds.DashToken, trivia, text);

        public static SyntaxToken DashToken(string text = TinyTokenTexts.Dash) =>
            DashToken("", text);

        public static SyntaxToken AsteriskToken(string trivia, string text = TinyTokenTexts.Asterisk) =>
            new TinyToken(TinyTokenKinds.AsteriskToken, trivia, text);

        public static SyntaxToken AsteriskToken(string text = TinyTokenTexts.Asterisk) =>
            AsteriskToken("", text);

        public static SyntaxToken SlashToken(string trivia, string text = TinyTokenTexts.Slash) =>
            new TinyToken(TinyTokenKinds.SlashToken, trivia, text);

        public static SyntaxToken SlashToken(string text = TinyTokenTexts.Slash) =>
            SlashToken("", text);

        public static SyntaxToken EqualEqualToken(string trivia, string text = TinyTokenTexts.EqualEqual) =>
            new TinyToken(TinyTokenKinds.EqualEqualToken, trivia, text);

        public static SyntaxToken EqualEqualToken(string text = TinyTokenTexts.EqualEqual) =>
            EqualEqualToken("", text);

        public static SyntaxToken NotEqualToken(string trivia, string text = TinyTokenTexts.NotEqual) =>
            new TinyToken(TinyTokenKinds.NotEqualToken, trivia, text);

        public static SyntaxToken NotEqualToken(string text = TinyTokenTexts.NotEqual) =>
            NotEqualToken("", text);

        public static SyntaxToken LessThanToken(string trivia, string text = TinyTokenTexts.LessThan) =>
            new TinyToken(TinyTokenKinds.LessThanToken, trivia, text);

        public static SyntaxToken LessThanToken(string text = TinyTokenTexts.LessThan) =>
            LessThanToken("", text);

        public static SyntaxToken LessThanOrEqualToken(string trivia, string text = TinyTokenTexts.LessThanEqual) =>
            new TinyToken(TinyTokenKinds.LessThanEqualToken, trivia, text);

        public static SyntaxToken LessThanOrEqualToken(string text = TinyTokenTexts.LessThanEqual) =>
            LessThanToken("", text);

        public static SyntaxToken GreaterThanToken(string trivia, string text = TinyTokenTexts.GreaterThan) =>
            new TinyToken(TinyTokenKinds.GreaterThanToken, trivia, text);

        public static SyntaxToken GreaterThanToken(string text = TinyTokenTexts.LessThan) =>
            GreaterThanToken("", text);

        public static SyntaxToken GreaterThanOrEqualToken(string trivia, string text = TinyTokenTexts.GreaterThanEqual) =>
            new TinyToken(TinyTokenKinds.GreaterThanEqualToken, trivia, text);

        public static SyntaxToken GreaterThanOrEqualToken(string text = TinyTokenTexts.GreaterThanEqual) =>
            GreaterThanToken("", text);

        public static SyntaxToken AndToken(string trivia, string text = TinyTokenTexts.And) =>
            new TinyToken(TinyTokenKinds.AndToken, trivia, text);

        public static SyntaxToken AndToken(string text = TinyTokenTexts.And) =>
            AndToken("", text);

        public static SyntaxToken OrToken(string trivia, string text = TinyTokenTexts.Or) =>
            new TinyToken(TinyTokenKinds.OrToken, trivia, text);

        public static SyntaxToken OrToken(string text = TinyTokenTexts.Or) =>
            OrToken("", text);

        public static SyntaxToken NotToken(string trivia, string text = TinyTokenTexts.Not) =>
            new TinyToken(TinyTokenKinds.NotToken, trivia, text);

        public static SyntaxToken NotToken(string text = TinyTokenTexts.Not) =>
            NotToken("", text);

        public static SyntaxToken EndOfTextToken(string trivia = "") =>
            new TinyToken(TinyTokenKinds.EndOfTextToken, trivia, "");

        public static TinyExpression Identifier(SyntaxToken token) =>
            new TinyIdentifier(token);

        public static TinyExpression Identifier(string name) =>
            Identifier(IdentifierToken(name));

        public static TinyExpression LiteralString(SyntaxToken token) =>
            new TinyLiteralString(token);

        public static TinyExpression LiteralString(string text) =>
            LiteralString(StringToken(text));

        public static TinyExpression LiteralNumber(SyntaxToken token) =>
            new TinyLiteralNumber(token);

        public static TinyExpression LiteralNumber(string text) =>
            LiteralNumber(NumberToken(text));

        public static TinyExpression Add(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.Add, left, op, right);

        public static TinyExpression Subtract(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.Subtract, left, op, right);

        public static TinyExpression Multiply(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.Multiply, left, op, right);

        public static TinyExpression Divide(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.Divide, left, op, right);

        public static TinyExpression Equal(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.Equal, left, op, right);

        public static TinyExpression NotEqual(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.NotEqual, left, op, right);

        public static TinyExpression LessThan(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.LessThan, left, op, right);

        public static TinyExpression LessThanOrEqual(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.LessThanOrEqual, left, op, right);

        public static TinyExpression GreaterThan(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.GreaterThan, left, op, right);

        public static TinyExpression GreaterThanOrEqual(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.GreaterThanOrEqual, left, op, right);

        public static TinyExpression And(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.And, left, op, right);

        public static TinyExpression Or(TinyExpression left, SyntaxToken op, TinyExpression right) =>
            new TinyBinary(TinyNodeKinds.Or, left, op, right);

        public static TinyExpression Not(SyntaxToken not, TinyExpression operand) =>
            new TinyPrefixUnary(TinyNodeKinds.Not, not, operand);

        public static TinyExpression Negate(SyntaxToken negation, TinyExpression operand) =>
            new TinyPrefixUnary(TinyNodeKinds.Negate, negation, operand);

        public static TinyExpression ParenthesizedExpression(SyntaxToken open, TinyExpression expression, SyntaxToken close) =>
            new TinyParentheses(open, expression, close);

        public static TinySkipped Skipped(IReadOnlyList<SyntaxElement> skipped) =>
            new TinySkipped(skipped);

        public static TinySkipped Skipped(params SyntaxElement[] skipped) =>
            Skipped((IReadOnlyList<SyntaxElement>)skipped);

        public static TinyRoot Root(TinyExpression expression, TinySkipped? skipped = null) =>
            new TinyRoot(expression, skipped ?? Skipped(EndOfTextToken("")));

        public static TinyExpression MissingExpression() =>
            new TinyIdentifier(
                new TinyToken(TinyTokenKinds.IdentifierToken, "", ""), 
                new Diagnostic("Expression expected"));

        public static SyntaxToken MissingCloseParen() =>
            new TinyToken(TinyTokenKinds.CloseParenToken, "", "", new Diagnostic("Missing )"));
    }
}
