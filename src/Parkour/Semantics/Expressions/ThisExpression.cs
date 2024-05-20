namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References the current instance within a code body.
/// </summary>
public sealed class ThisExpression : Expression
{
    public ThisExpression(
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
    }

    public override ThisExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ThisExpression(
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override ThisExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ThisExpression(
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override ThisExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ThisExpression(
            this.Location,
            resultType,
            this.Diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
    public override ThisExpression RewriteChildren(SemanticRewriter rewriter) => this;
}