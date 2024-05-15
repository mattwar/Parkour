namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Converts the expression to the specified type if it is an instance of that type or null if it is not.
/// </summary>
public class AsTypeExpression : Expression
{
    public Expression Expression { get; }
    public Expression Type { get; }
    public TypeSymbol? TypeSymbol { get; }

    public AsTypeExpression(
        Expression expression,
        Expression type,
        ISourceLocation? location,
        TypeSymbol? typeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | State(type)
            | NotNullOrDiagnosticState(typeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Type = type;
        this.TypeSymbol = typeSymbol;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.Type,
            _ => null
        };
}
