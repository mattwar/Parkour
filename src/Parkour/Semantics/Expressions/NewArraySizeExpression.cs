namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Creates a new single dimensional array 
/// </summary>
public class NewArraySizeExpression : Expression
{
    public Expression? ElementType { get; }
    public ImmutableList<Expression> Sizes { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArraySizeExpression(
        Expression? elementType,
        ImmutableList<Expression> sizes,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | CombineState(sizes)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        ElementType = elementType;
        Sizes = sizes;
        ElementTypeSymbol = elementTypeSymbol;
    }

    public override int ChildCount => 1 + this.Sizes.Count;

    public override SemanticElement? GetChild(int index)
    {
        if (index == 0)
            return this.ElementType;
        index--;
        return (index < this.Sizes.Count) ? this.Sizes[index] : null;
    }
}
