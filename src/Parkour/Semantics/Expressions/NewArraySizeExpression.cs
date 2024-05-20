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

    public override NewArraySizeExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NewArraySizeExpression(
            this.ElementType,
            this.Sizes,
            location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override NewArraySizeExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NewArraySizeExpression(
            this.ElementType,
            this.Sizes,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override NewArraySizeExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new NewArraySizeExpression(
            this.ElementType,
            this.Sizes,
            this.Location,
            this.ElementTypeSymbol,
            resultType,
            this.Diagnostics
            );

    public NewArraySizeExpression WithElementType(Expression elementType) =>
        elementType == this.ElementType ? this :
        new NewArraySizeExpression(
            elementType,
            this.Sizes,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArraySizeExpression WithSizes(ImmutableList<Expression> sizes) =>
        sizes == this.Sizes ? this :
        new NewArraySizeExpression(
            this.ElementType,
            sizes,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArraySizeExpression WithElementTypeSymbol(TypeSymbol? elementTypeSymbol) =>
        elementTypeSymbol == this.ElementTypeSymbol ? this :
        new NewArraySizeExpression(
            this.ElementType,
            this.Sizes,
            this.Location,
            elementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1 + this.Sizes.Count;

    public override SemanticElement? GetChild(int index)
    {
        if (index == 0)
            return this.ElementType;
        index--;
        return (index < this.Sizes.Count) ? this.Sizes[index] : null;
    }

    public override NewArraySizeExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var elementType = rewriter.Rewrite(this.ElementType);
        var sizes = rewriter.Rewrite(this.Sizes);
        return this
            .WithElementType(elementType!)
            .WithSizes(sizes);
    }
}
