namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class ConditionExpression : Expression
{
    public Expression Test { get; }
    public Expression WhenTrue { get; }
    public Expression WhenFalse { get; }

    public ConditionExpression(
        Expression test,
        Expression whenTrue,
        Expression whenFalse,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            test.State 
            | whenTrue.State 
            | whenFalse.State
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Test = test;
        this.WhenTrue = whenTrue;
        this.WhenFalse = whenFalse;
    }

    public override int ChildCount => 3;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Test,
            1 => this.WhenTrue,
            2 => this.WhenFalse,
            _ => null
        };
}

