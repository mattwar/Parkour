namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Converts referenced types to an arrays of that type.
/// </summary>
public class ArrayExpression : AdjustedReferenceExpression
{
    public override Expression ElementType { get; }
    public override Symbol? ReferencedSymbol { get; }

    public ArrayExpression(
        Expression elementType,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | NotNullOrDiagnosticState(referencedSymbol, diagnostics),
            location,
            resultType,
            diagnostics)
    {
        this.ElementType = elementType;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ElementType,
            _ => null
        };
}