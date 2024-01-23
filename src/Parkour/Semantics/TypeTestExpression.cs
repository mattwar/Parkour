namespace Parkour.Semantics;
using Symbols;

public class TypeTestExpression : Expression
{
    public Expression Expression { get; }
    public Expression? TypeExpression { get; }

    public TypeTestExpression(
        Expression expression,
        Expression? typeExpression,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic> diagnostics)
        : base(
            expression.State
            | OptionalState(typeExpression)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.TypeExpression = typeExpression;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.TypeExpression,
            _ => null
        };
}
