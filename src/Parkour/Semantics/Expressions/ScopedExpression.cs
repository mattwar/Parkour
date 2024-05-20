namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// An expression that evaluates the scope expression,
/// then evaluates the expression with the scope expression's members in scope
/// returning the scope expression's final value.
/// </summary>
public class ScopedExpression : Expression
{
    public Expression Scope { get; }
    public Expression Expression { get; }

    public ScopedExpression(
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

    public override ScopedExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ScopedExpression(
            this.Scope,
            this.Expression,
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override ScopedExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ScopedExpression(
            this.Scope,
            this.Expression,
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override ScopedExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ScopedExpression(
            this.Scope,
            this.Expression,
            this.Location,
            resultType,
            this.Diagnostics
            );

    public ScopedExpression WithScope(Expression scope) =>
        scope == this.Scope ? this :
        new ScopedExpression(
            scope,
            this.Expression,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public ScopedExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new ScopedExpression(
            this.Scope,
            expression,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Scope,
            1 => this.Expression,
            _ => null
        };

    public override ScopedExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var scope = rewriter.Rewrite(this.Scope);
        var expression = rewriter.Rewrite(this.Expression);
        return this
            .WithScope(scope!)
            .WithExpression(expression!);
    }
}
