namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References a named symbol or symbols in scope.
/// </summary>
public sealed class NameReferenceExpression : Expression
{
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public NameReferenceExpression(
        string name,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullOrDiagnosticState(referencedSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}