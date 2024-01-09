namespace Parkour.Expressions;
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
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
              test.State | whenTrue.State | whenFalse.State,
              resultType,
              diagnostics,
              syntax)
    {
        this.Test = test;
        this.WhenTrue = whenTrue;
        this.WhenFalse = whenFalse;
    }
}

