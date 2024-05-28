namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Repeats the evaluation of expression until 
/// the loop is exited by branching to the break target
/// or other targets outside the loop.
/// </summary>
public sealed class LoopExpression : Expression
{
    public Expression Expression { get; }
    public LabelSymbol? BreakTarget { get; }
    public LabelSymbol? ContinueTarget { get; }

    private LoopExpression(
        Expression expression,
        ISourceLocation? location,
        TypeSymbol? resultType,
        LabelSymbol? breakTarget,
        LabelSymbol? continueTarget,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | NotNullState(resultType)
            | NotNullState(breakTarget)
            | NotNullState(continueTarget), 
            location,
            resultType, 
            diagnostics)
    {
        this.Expression = expression;
        this.BreakTarget = breakTarget;
        this.ContinueTarget = continueTarget;
    }

    public LoopExpression(
        Expression expression,
        ISourceLocation? location)
        : this(expression, location, null, null, null, null)
    {
    }

    public override LoopExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new LoopExpression(
            this.Expression,
            location,
            this.ResultType,
            this.BreakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public override LoopExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new LoopExpression(
            this.Expression,
            this.Location,
            this.ResultType,
            this.BreakTarget,
            this.ContinueTarget,
            diagnostics
            );

    public override LoopExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new LoopExpression(
            this.Expression,
            this.Location,
            resultType,
            this.BreakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public LoopExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new LoopExpression(
            expression,
            this.Location,
            this.ResultType,
            this.BreakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public LoopExpression WithBreakTarget(LabelSymbol? breakTarget) =>
        breakTarget == this.BreakTarget ? this :
        new LoopExpression(
            this.Expression,
            this.Location,
            this.ResultType,
            breakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public LoopExpression WithContinueTarget(LabelSymbol? continueTarget) =>
        continueTarget == this.ContinueTarget ? this :
        new LoopExpression(
            this.Expression,
            this.Location,
            this.ResultType,
            this.BreakTarget,
            continueTarget,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => null
        };

    public override LoopExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var body = rewriter.Rewrite(this.Expression);
        return this.WithExpression(body!);
    }
}