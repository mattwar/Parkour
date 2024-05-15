namespace Parkour.Semantics;

using Symbols;

public class TypeOfExpression : Expression
{
    public Expression Type { get; }
    public TypeSymbol? TypeSymbol { get; }

    public TypeOfExpression(
        Expression type,
        ISourceLocation? location,
        TypeSymbol? typeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(type)
            | NotNullOrDiagnosticState(typeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
        this.TypeSymbol = typeSymbol;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index == 0 ? this.Type : null;
}
