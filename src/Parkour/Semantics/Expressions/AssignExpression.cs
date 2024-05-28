using Parkour.Symbols;

namespace Parkour.Semantics;

/// <summary>
/// Assigns the result of the source expression to the location
/// specified by the target expression.
/// </summary>
public sealed class AssignExpression : Expression
{
    public Expression Target { get; }
    public Expression Source { get; }

    private AssignExpression(
        Expression target,
        Expression source,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(target)
            | State(source), 
            location,
            resultType,
            diagnostics)
    {
        this.Target = target;
        this.Source = source;
    }

    public AssignExpression(
        Expression target,
        Expression source,
        ISourceLocation? location)
        : this(target, source, location, null, null)
    {
    }

    public override AssignExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new AssignExpression(
            this.Target,
            this.Source,
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override AssignExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new AssignExpression(
            this.Target,
            this.Source,
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override AssignExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new AssignExpression(
            this.Target,
            this.Source,
            this.Location,
            resultType,
            this.Diagnostics
            );

    public AssignExpression WithTarget(Expression target) =>
        target == this.Target ? this :
        new AssignExpression(
            target,
            this.Source,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public AssignExpression WithSource(Expression source) =>
        source == this.Source ? this :
        new AssignExpression(
            this.Target,
            source,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Target,
            1 => this.Source,
            _ => null
        };

    public override AssignExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var target = rewriter.Rewrite(this.Target);
        var source = rewriter.Rewrite(this.Source);
        return this
            .WithTarget(target!)
            .WithSource(source!);
    }
}
