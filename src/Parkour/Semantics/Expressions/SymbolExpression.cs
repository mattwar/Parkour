namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References a symbol via the current symbol table.
/// </summary>
public sealed class SymbolExpression : Expression
{
    public string FullName { get; }
    public override Symbol? ReferencedSymbol { get; }

    public SymbolExpression(
        string fullName,
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
        this.FullName = fullName;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}
