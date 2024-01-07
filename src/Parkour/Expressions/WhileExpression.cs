namespace Parkour.Expressions;
using Symbols;

public sealed class WhileExpression : Expression
{
    public Expression Test { get; }
    public Expression Body { get; }
    public TargetSymbol? BreakTarget { get; }
    public TargetSymbol? ContinueTarget { get; }

    public WhileExpression(
        Expression test,
        Expression body,
        TypeSymbol? resultType,
        TargetSymbol? breakTarget,
        TargetSymbol? continueTarget,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(test.State | body.State, resultType, diagnostics)
    {
        this.Test = test;
        this.Body = body;
        this.BreakTarget = breakTarget;
        this.ContinueTarget = continueTarget;
    }
}

