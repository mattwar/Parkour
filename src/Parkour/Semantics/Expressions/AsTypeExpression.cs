namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Converts the expression to the specified type if it is an instance of that type 
/// or null if it is not.
/// </summary>
public class AsTypeExpression : Expression
{
    public Expression Expression { get; }
    public Expression Type { get; }
    public TypeSymbol? TypeSymbol { get; }

    private AsTypeExpression(
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

    public AsTypeExpression(
        Expression expression,
        Expression type,
        ISourceLocation? location)
        : this(expression, type, location, null, null, null)
    {
    }

    public override AsTypeExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new AsTypeExpression(
            this.Expression,
            this.Type,
            location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override AsTypeExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new AsTypeExpression(
            this.Expression,
            this.Type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            diagnostics
            );

    public override AsTypeExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new AsTypeExpression(
            this.Expression,
            this.Type,
            this.Location,
            this.TypeSymbol,
            resultType,
            this.Diagnostics
            );

    public AsTypeExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new AsTypeExpression(
            expression,
            this.Type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public AsTypeExpression WithType(Expression type) =>
        type == this.Type ? this :
        new AsTypeExpression(
            this.Expression,
            type,
            this.Location,
            this.TypeSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public AsTypeExpression WithTypeSymbol(TypeSymbol? symbol) =>
        symbol == this.TypeSymbol ? this :
        new AsTypeExpression(
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

    public override AsTypeExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        var type = rewriter.Rewrite(this.Type);
        return this
            .WithExpression(expression!)
            .WithType(type!);
    }
}
