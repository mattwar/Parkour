using Parkour;
using Parkour.Syntax;

namespace Tiny;

public abstract record TinyExpression(string Kind, Diagnostic? Diagnostic)
    : SyntaxNode(Diagnostic)
{
    public override string Kind { get; } = Kind;
}

public abstract record TinyLiteral(string Kind, SyntaxToken Token, Diagnostic? Diagnostic) 
    : TinyExpression(Kind, Diagnostic);

public sealed record TinyLiteralNumber(SyntaxToken Token, Diagnostic? Diagnostic = null)
    : TinyLiteral(TinyNodeKinds.LiteralNumber, Token, Diagnostic);

public sealed record TinyLiteralString(SyntaxToken Token, Diagnostic? Diagnostic = null) 
    : TinyLiteral(TinyNodeKinds.LiteralString, Token, Diagnostic);

public sealed record TinyIdentifier(SyntaxToken Token, Diagnostic? Diagnostic = null)
    : TinyExpression(TinyNodeKinds.Identifier, Diagnostic);

public sealed record TinyBinary(string Kind, TinyExpression Left, SyntaxToken Operator, TinyExpression Right, Diagnostic? Diagnostic = null) 
    : TinyExpression(Kind, Diagnostic);

public sealed record TinyPrefixUnary(string Kind, SyntaxToken Operator, TinyExpression Operand, Diagnostic? Diagnostic = null) 
    : TinyExpression(Kind, Diagnostic);

public sealed record TinyParentheses(SyntaxToken OpenParen, TinyExpression Expression, SyntaxToken CloseParen, Diagnostic? Diagnostic = null)
    : TinyExpression(TinyNodeKinds.ParenthesizedExpression, Diagnostic);

public sealed record TinySkipped(IReadOnlyList<SyntaxElement?> Elements, Diagnostic? Diagnostic = null) 
    : SyntaxList(Elements, Diagnostic);

public sealed record TinyRoot(TinyExpression Expression, TinySkipped Skipped, Diagnostic? Diagnostic = null) 
    : TinyExpression(TinyNodeKinds.Root, Diagnostic);
