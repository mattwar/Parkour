namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Repeats the evaluation of expression until 
/// the loop is exited by branching to the break target
/// or other targets outside the loop.
/// </summary>
public sealed class LoopExpression : Expression
{
    public Expression Body { get; }
    public LabelSymbol? BreakTarget { get; }
    public LabelSymbol? ContinueTarget { get; }

    public LoopExpression(
        Expression body,
        ISourceLocation? location,
        TypeSymbol? resultType,
        LabelSymbol? breakTarget,
        LabelSymbol? continueTarget,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(body)
            | NotNullState(resultType)
            | NotNullState(breakTarget)
            | NotNullState(continueTarget), 
            location,
            resultType, 
            diagnostics)
    {
        this.Body = body;
        this.BreakTarget = breakTarget;
        this.ContinueTarget = continueTarget;
    }

    public override LoopExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new LoopExpression(
            this.Body,
            location,
            this.ResultType,
            this.BreakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public override LoopExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new LoopExpression(
            this.Body,
            this.Location,
            this.ResultType,
            this.BreakTarget,
            this.ContinueTarget,
            diagnostics
            );

    public override LoopExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new LoopExpression(
            this.Body,
            this.Location,
            resultType,
            this.BreakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public LoopExpression WithBody(Expression body) =>
        body == this.Body ? this :
        new LoopExpression(
            body,
            this.Location,
            this.ResultType,
            this.BreakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public LoopExpression WithBreakTarget(LabelSymbol? breakTarget) =>
        breakTarget == this.BreakTarget ? this :
        new LoopExpression(
            this.Body,
            this.Location,
            this.ResultType,
            breakTarget,
            this.ContinueTarget,
            this.Diagnostics
            );

    public LoopExpression WithContinueTarget(LabelSymbol? continueTarget) =>
        continueTarget == this.ContinueTarget ? this :
        new LoopExpression(
            this.Body,
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
            0 => this.Body,
            _ => null
        };

    public override LoopExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var body = rewriter.Rewrite(this.Body);
        return this.WithBody(body!);
    }
}