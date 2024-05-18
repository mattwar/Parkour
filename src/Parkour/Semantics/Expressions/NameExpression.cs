namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References a named symbol or symbols in the current scope,
/// based on the lookup rules of the binder.
/// </summary>
public sealed class NameExpression : Expression
{
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public NameExpression(
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