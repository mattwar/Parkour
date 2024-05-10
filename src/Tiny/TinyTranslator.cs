using Parkour;
using Parkour.Syntax;
using Parkour.Semantics;
using static Parkour.Semantics.SemanticFactory;

namespace Tiny;

/// <summary>
/// Translates tiny syntax into expressions
/// </summary>
public class TinyTranslator
{
    public TinyTranslator()
    {
    }

    public Expression Translate(SyntaxElement element)
    {
        if (element is SyntaxToken token)
        {
            return token.Kind switch
            {
                TinyTokenKinds.NumberToken =>
                    Constant(double.Parse(token.Text), token),
                TinyTokenKinds.IdentifierToken =>
                    Name(token.Text, token),
                _ => throw new InvalidOperationException($"Unhandled token kind: '{token.Kind}'")
            };
        }
        else
        {
            return element.Kind switch
            {
                TinyNodeKinds.Add =>
                    Add(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.Subtract =>
                    Subtract(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.Multiply =>
                    Multiply(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.Divide =>
                    Divide(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.Negate =>
                    Negate(Translate(element.GetChild(0)!), element.GetChild(1)),
                TinyNodeKinds.Equal =>
                    Equal(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.NotEqual =>
                    NotEqual(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.LessThan =>
                    LessThan(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.LessThanOrEqual =>
                    LessThanOrEqual(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.GreaterThan =>
                    GreaterThan(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.GreaterThanOrEqual =>
                    GreaterThanOrEqual(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.And =>
                    BitwiseAnd(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.Or =>
                    BitwiseOr(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!), element.GetChild(1)),
                TinyNodeKinds.Not =>
                    BitwiseNot(Translate(element.GetChild(0)!), element.GetChild(1)),
                TinyNodeKinds.ParenthesizedExpression =>
                    Translate(element.GetChild(0)!),
                TinyNodeKinds.Root =>
                    Translate(element.GetChild(0)!),
                TinyNodeKinds.LiteralNumber =>
                    Translate(element.GetChild(0)!),
                TinyNodeKinds.LiteralString =>
                    Translate(element.GetChild(0)!),
                _ => throw new InvalidOperationException($"Unhandled node kind: '{element.Kind}'")
            };
        }
    }
}
