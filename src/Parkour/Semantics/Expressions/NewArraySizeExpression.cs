namespace Parkour.Semantics;

using Symbols;

public class NewArraySizeExpression : Expression
{
    public Expression? ElementType { get; }
    public Expression Size { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArraySizeExpression(
        Expression? elementType,
        Expression size,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | State(size)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        ElementType = elementType;
        Size = size;
        ElementTypeSymbol = elementTypeSymbol;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => ElementType,
            1 => Size,
            _ => null
        };
}
