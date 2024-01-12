using Parkour;
using Parkour.Syntax;

namespace Tiny;

public abstract class TinyExpression : SyntaxList
{
    public TinyExpression(string kind, IReadOnlyList<SyntaxElement> elements, Diagnostic? diagnostic = null)
        : base(kind, elements, diagnostic)
    {
    }
}

public abstract class TinyLiteral : TinyExpression
{
    public TinyLiteral(string kind, SyntaxToken token, Diagnostic? diagnostic = null)
        : base(kind, [token], diagnostic)
    {
    }

    public SyntaxToken Token => (SyntaxToken)GetChild(0)!;
}

public sealed class TinyLiteralNumber : TinyLiteral
{
    public TinyLiteralNumber(SyntaxToken token, Diagnostic? diagnostic = null)
        : base(TinyNodeKinds.LiteralNumber, token, diagnostic)
    {
    }
}

public sealed class TinyLiteralString : TinyLiteral
{
    public TinyLiteralString(SyntaxToken token, Diagnostic? diagnostic = null)
        : base(TinyNodeKinds.LiteralString, token, diagnostic)
    {
    }
}

public sealed class TinyIdentifier : TinyExpression
{
    public TinyIdentifier(SyntaxToken token, Diagnostic? diagnostic = null)
        : base(TinyNodeKinds.Identifier, [token], diagnostic)
    {
    }

    public SyntaxToken Token => (SyntaxToken)GetChild(0)!;
}

public sealed class TinyBinary : TinyExpression
{
    public TinyBinary(string kind, TinyExpression left, SyntaxToken @operator, TinyExpression right, Diagnostic? diagnostic = null)
        : base(kind, [left, @operator, right], diagnostic)
    {
    }

    public TinyExpression Left => (TinyExpression)GetChild(0)!;
    public SyntaxToken Operator => (SyntaxToken)GetChild(1)!;
    public TinyExpression Right => (TinyExpression)GetChild(2)!;
}

public class TinyPrefixUnary : TinyExpression
{
    public TinyPrefixUnary(string kind, SyntaxToken @operator, TinyExpression operand, Diagnostic? diagnostic = null)
        : base(kind, [@operator, operand], diagnostic)
    {
    }

    public SyntaxToken Operator => (SyntaxToken)GetChild(0)!;
    public TinyExpression Operand => (TinyExpression)GetChild(1)!;
}

public class TinyParentheses : TinyExpression
{
    public TinyParentheses(SyntaxToken openParen, TinyExpression expression, SyntaxToken closeParen, Diagnostic? diagnostic = null)
        : base(TinyNodeKinds.ParenthesizedExpression, [openParen, expression, closeParen], diagnostic)
    {
    }

    public SyntaxToken OpenParen => (SyntaxToken)GetChild(0)!;
    public TinyExpression Expression => (TinyExpression)GetChild(1)!;
    public SyntaxToken CloseParen => (SyntaxToken)GetChild(2)!;
}

public class TinySkipped : SyntaxList
{
    public TinySkipped(IReadOnlyList<SyntaxElement> skipped, Diagnostic? diagnostic = null)
        : base(TinyNodeKinds.Skipped, skipped, diagnostic)
    {
    }
}

public class TinyRoot : TinyExpression
{
    public TinyRoot(TinyExpression expression, TinySkipped skipped, Diagnostic? diagnostic = null)
        : base(TinyNodeKinds.Root, [expression, skipped], diagnostic)
    {
    }

    public TinyExpression Expression => (TinyExpression)GetChild(0)!;
    public TinySkipped Skipped => (TinySkipped)GetChild(1)!;
}
