namespace Parkour.Expressions;
using Symbols;
using Syntax;

public class OperatorExpression : Expression
{
    public string Kind { get; }
    public override Symbol? ReferencedSymbol { get; }

    public OperatorExpression(
        string kind, 
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
        this.Kind = kind;
        this.ReferencedSymbol = referencedSymbol;
    }
}
