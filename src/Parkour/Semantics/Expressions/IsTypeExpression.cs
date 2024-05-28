namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Tests the type of the value produced by the expression.
/// </summary>
public class IsTypeExpression : Expression
{
    public Expression Expression { get; }
    public Expression Type { get; }
    public TypeSymbol? TypeSymbol { get; }

    private IsTypeExpression(
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

    public IsTypeExpression(
        Expression expression,
        Expression type,
        ISourceLocation? location)
        : this(expression, type, location, null, null, null)
    {
    }

    public override IsTypeExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new IsTypeExpression(
            this.Expression,
            this.Type,
            location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override IsTypeExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new IsTypeExpression(
            this.Expression,
            this.Type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override IsTypeExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new IsTypeExpression(
            this.Expression,
            this.Type,
            this.Location,
            this.TypeSymbol,
            resultType,
            this.Diagnostics
            );

    public IsTypeExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new IsTypeExpression(
            expression,
            this.Type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public IsTypeExpression WithType(Expression type) =>
        type == this.Type ? this :
        new IsTypeExpression(
            this.Expression,
            type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public IsTypeExpression WithTypeSymbol(TypeSymbol? symbol) =>
        symbol == this.TypeSymbol ? this :
        new IsTypeExpression(
            this.Expression,
            this.Type,
            this.Location,
            symbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            1 => this.Type,
            _ => null
        };

    public override IsTypeExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        var type = rewriter.Rewrite(this.Type);
        return this
            .WithExpression(expression!)
            .WithType(type!);
    }
}
