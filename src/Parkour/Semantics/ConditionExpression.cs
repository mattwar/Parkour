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
              test.State | whenTrue.State | whenFalse.State,
              location,
              resultType,
              diagnostics)
    {
        this.Test = test;
        this.WhenTrue = whenTrue;
        this.WhenFalse = whenFalse;
    }
}

