namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Creates a new array initialized with the values.
/// </summary>
public class NewArrayExpression : Expression
{
    public Expression? ElementType { get; }
    public ImmutableList<Expression> Sizes { get; }
    public ImmutableList<Expression> Values { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArrayExpression(
        Expression? elementType,
        ImmutableList<Expression> sizes,
        ImmutableList<Expression> values,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | CombineState(values)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.ElementType = elementType;
        this.Sizes = sizes;
        this.Values = values;
        this.ElementTypeSymbol = elementTypeSymbol;
    }

    public NewArrayExpression(
        Expression? elementType,
        ImmutableList<Expression> sizes,
        ImmutableList<Expression> values,
        ISourceLocation? location)
        : this(elementType, sizes, values, location, null, null, null)
    {
    }

    public override NewArrayExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NewArrayExpression(
            this.ElementType,
            this.Sizes,
            this.Values,
            location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override NewArrayExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NewArrayExpression(
            this.ElementType,
            this.Sizes,
            this.Values,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override NewArrayExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new NewArrayExpression(
            this.ElementType,
            this.Sizes,
            this.Values,
            this.Location,
            this.ElementTypeSymbol,
            resultType,
            this.Diagnostics
            );

    public NewArrayExpression WithElementType(Expression elementType) =>
        elementType == this.ElementType ? this :
        new NewArrayExpression(
            elementType,
            this.Sizes,
            this.Values,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArrayExpression WithSizes(ImmutableList<Expression> sizes) =>
        sizes == this.Sizes ? this :
        new NewArrayExpression(
            this.ElementType,
            sizes,
            this.Values,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArrayExpression WithValues(ImmutableList<Expression> values) =>
        values == this.Values ? this :
        new NewArrayExpression(
            this.ElementType,
            this.Sizes,
            values,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArrayExpression WithElementTypeSymbol(TypeSymbol? elementTypeSymbol) =>
        elementTypeSymbol == this.ElementTypeSymbol ? this :
        new NewArrayExpression(
            this.ElementType,
            this.Sizes,
            this.Values,
            this.Location,
            elementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        1 + Values.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ElementType,
            _ => this.Values[index - 1]
        };

    public override NewArrayExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var elementType = rewriter.Rewrite(this.ElementType);
        var expressions = rewriter.Rewrite(this.Values);
        return this
            .WithElementType(elementType!)
            .WithValues(expressions);
    }
}
