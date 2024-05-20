namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Creates a new array single dimensional array initialized with the values of the expressions.
/// </summary>
public class NewArrayInitExpression : Expression
{
    public Expression? ElementType { get; }
    public ImmutableList<Expression> Expressions { get; }
    public TypeSymbol? ElementTypeSymbol { get; }

    public NewArrayInitExpression(
        Expression? elementType,
        ImmutableList<Expression> expressions,
        ISourceLocation? location,
        TypeSymbol? elementTypeSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(elementType)
            | CombineState(expressions)
            | NotNullOrDiagnosticState(elementTypeSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        ElementType = elementType;
        Expressions = expressions;
        ElementTypeSymbol = elementTypeSymbol;
    }

    public override NewArrayInitExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NewArrayInitExpression(
            this.ElementType,
            this.Expressions,
            location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override NewArrayInitExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new NewArrayInitExpression(
            this.ElementType,
            this.Expressions,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override NewArrayInitExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new NewArrayInitExpression(
            this.ElementType,
            this.Expressions,
            this.Location,
            this.ElementTypeSymbol,
            resultType,
            this.Diagnostics
            );

    public NewArrayInitExpression WithElementType(Expression elementType) =>
        elementType == this.ElementType ? this :
        new NewArrayInitExpression(
            elementType,
            this.Expressions,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArrayInitExpression WithExpressions(ImmutableList<Expression> expressions) =>
        expressions == this.Expressions ? this :
        new NewArrayInitExpression(
            this.ElementType,
            expressions,
            this.Location,
            this.ElementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public NewArrayInitExpression WithElementTypeSymbol(TypeSymbol? elementTypeSymbol) =>
        elementTypeSymbol == this.ElementTypeSymbol ? this :
        new NewArrayInitExpression(
            this.ElementType,
            this.Expressions,
            this.Location,
            elementTypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        1 + Expressions.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.ElementType,
            _ => this.Expressions[index - 1]
        };

    public override NewArrayInitExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var elementType = rewriter.Rewrite(this.ElementType);
        var expressions = rewriter.Rewrite(this.Expressions);
        return this
            .WithElementType(elementType!)
            .WithExpressions(expressions);
    }
}
