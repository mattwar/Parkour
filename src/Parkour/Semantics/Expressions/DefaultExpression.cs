namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Produces the default value for the type,
/// which is null for reference types and zero-initialized for value types.
/// </summary>
public sealed class DefaultExpression : Expression
{
    public Expression? Type { get; }

    public DefaultExpression(
        Expression? type,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Type = type;
    }

    public override DefaultExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new DefaultExpression(
            this.Type,
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override DefaultExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new DefaultExpression(
            this.Type,
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override DefaultExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new DefaultExpression(
            this.Type,
            this.Location,
            resultType,
            this.Diagnostics
            );

    public DefaultExpression WithType(Expression type) =>
        type == this.Type ? this :
        new DefaultExpression(
            type,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Type,
            _ => null
        };

    public override DefaultExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var type = rewriter.Rewrite(this.Type);
        return this.WithType(type!);
    }
}
