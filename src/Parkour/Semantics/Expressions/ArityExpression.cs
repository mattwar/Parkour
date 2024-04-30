namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Filters referenced symbols to only those matching the arity.
/// </summary>
public class ArityExpression : AdjustedReferenceExpression
{
    public override Expression Expression { get; }
    public int Arity { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ArityExpression(
        Expression expression,
        int arity,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | NotNullOrDiagnosticState(referencedSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Arity = arity;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => null
        };
}