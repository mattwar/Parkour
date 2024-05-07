namespace Parkour.Semantics;

using Symbols;

public class TypeOfExpression : Expression
{
    public Expression Type { get; }

    public TypeOfExpression(
        Expression type,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(type)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index == 0 ? this.Type : null;
}
