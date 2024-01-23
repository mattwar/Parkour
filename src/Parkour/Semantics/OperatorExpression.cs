namespace Parkour.Semantics;
using Symbols;

public class OperatorExpression : Expression
{
    public string Kind { get; }
    public override Symbol? ReferencedSymbol { get; }

    public OperatorExpression(
        string kind, 
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
        this.Kind = kind;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}
