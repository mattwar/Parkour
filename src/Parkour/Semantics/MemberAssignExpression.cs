namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// An expression that evaluates the scope expression,
/// then evaluates the expression with the scope expression's members in scope
/// returning the scope expression's final value.
/// </summary>
public class WithExpression : Expression
{
    public Expression Scope { get; }
    public Expression Expression { get; }

    public WithExpression(
        Expression scope,
        Expression expression,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(scope)
            | State(expression)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Scope = scope;
        this.Expression = expression;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Scope,
            1 => this.Expression,
            _ => null
        };
}
