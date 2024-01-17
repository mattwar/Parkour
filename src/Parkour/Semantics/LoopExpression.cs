namespace Parkour.Semantics;
using Symbols;

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
            body.State
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
}

