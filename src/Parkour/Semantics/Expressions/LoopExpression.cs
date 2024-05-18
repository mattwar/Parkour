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

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Body,
            _ => null
        };
}

