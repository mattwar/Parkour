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
                    Constant(double.Parse(token.Text)),
                TinyTokenKinds.IdentifierToken =>
                    Reference(token.Text),
                _ => throw new InvalidOperationException($"Unhandled token kind: '{token.Kind}'")
            };
        }
        else
        {
            return element.Kind switch
            {
                TinyNodeKinds.Add =>
                    Add(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.Subtract =>
                    Subtract(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.Multiply =>
                    Multiply(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.Divide =>
                    Divide(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.Negate =>
                    Negate(Translate(element.GetChild(0)!)),
                TinyNodeKinds.Equal =>
                    Equal(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.NotEqual =>
                    NotEqual(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.LessThan =>
                    LessThan(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.LessThanOrEqual =>
                    LessThanOrEqual(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.GreaterThan =>
                    GreaterThan(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.GreaterThanOrEqual =>
                    GreaterThanOrEqual(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.And =>
                    And(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.Or =>
                    Or(Translate(element.GetChild(0)!), Translate(element.GetChild(2)!)),
                TinyNodeKinds.Not =>
                    Not(Translate(element.GetChild(0)!)),
                TinyNodeKinds.ParenthesizedExpression =>
                    Translate(element.GetChild(0)!),
                TinyNodeKinds.Root =>
                    Translate(element.GetChild(0)!),
                _ => throw new InvalidOperationException($"Unhandled node kind: '{element.Kind}'")
            };
        }
    }
}
