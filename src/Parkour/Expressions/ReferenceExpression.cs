namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class ReferenceExpression : Expression
{
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ReferenceExpression(
        string name,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            ContainsState.None,
            resultType,
            diagnostics,
            syntax)
    {
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }
}

