using System;
using Parkour.Analysis;

namespace Tiny
{
    public static class TokenKinds
    {
        public const string IdentifierToken = nameof(IdentifierToken);
        public const string NumberToken = nameof(NumberToken);
        public const string StringToken = nameof(StringToken);
        public const string OpenParenToken = nameof(OpenParenToken);
        public const string CloseParenToken = nameof(CloseParenToken);
        public const string PlusToken = nameof(PlusToken);
        public const string DashToken = nameof(DashToken);
        public const string AsteriskToken = nameof(AsteriskToken);
        public const string SlashToken = nameof(SlashToken);
        public const string EqualToken = nameof(EqualToken);
        public const string EqualEqualToken = nameof(EqualEqualToken);
        public const string NotEqualToken = nameof(NotEqualToken);
        public const string GreaterThanToken = nameof(GreaterThanToken);
        public const string GreaterThanEqualToken = nameof(GreaterThanEqualToken);
        public const string LessThanToken = nameof(LessThanToken);
        public const string LessThanEqualToken = nameof(LessThanEqualToken);
        public const string AndToken = nameof(AndToken);
        public const string OrToken = nameof(OrToken);
        public const string NotToken = nameof(NotToken);
        public const string LetToken = nameof(LetToken);
        public const string ColonToken = nameof(ColonToken);
        public const string CommaToken = nameof(CommaToken);
        public const string QuestionMarkToken = nameof(QuestionMarkToken);
        public const string UnknownToken = nameof(UnknownToken);
        public const string EndOfTextToken = nameof(EndOfTextToken);
    }

    public static class TokenTexts
    {
        public const string OpenParen = "(";
        public const string CloseParen = ")";
        public const string Plus = "+";
        public const string Dash = "-";
        public const string Asterisk = "*";
        public const string Slash = "/";
        public const string Equal = "=";
        public const string EqualEqual = "==";
        public const string NotEqual = "!=";
        public const string GreaterThan = ">";
        public const string GreaterThanEqual = ">=";
        public const string LessThan = "<";
        public const string LessThanEqual = "<=";
        public const string And = "and";
        public const string Or = "or";
        public const string Not = "not";
        public const string Let = "let";
        public const string Colon = ":";
        public const string Comma = ",";
        public const string QuestionMark = "?";
        public const string SingleQuote = "'";
        public const string DoubleQuote = "\"";
    }

    public static class NodeKinds
    {
        public const string Add = nameof(Add);
        public const string Subtract = nameof(Subtract);
        public const string Multiply = nameof(Multiply);
        public const string Divide = nameof(Divide);
        public const string Equal = nameof(Equal);
        public const string NotEqual = nameof(NotEqual);
        public const string LessThan = nameof(LessThan);
        public const string LessThanOrEqual = nameof(LessThanOrEqual);
        public const string GreaterThan = nameof(GreaterThan);
        public const string GreaterThanOrEqual = nameof(GreaterThanOrEqual);
        public const string And = nameof(And);
        public const string Or = nameof(Or);
        public const string Not = nameof(Not);
        public const string Negate = nameof(Negate);
        public const string ParenthesizedExpression = nameof(ParenthesizedExpression);
        public const string Skipped = nameof(Skipped);
        public const string Root = nameof(Root);
    }
}