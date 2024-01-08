namespace Parkour.Expressions;
using Symbols;

public sealed class BlockExpression : Expression
{
    public ImmutableList<Expression> Expressions { get; }

    public BlockExpression(
        ImmutableList<Expression> expressions,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(
              CombineState(expressions),
              expressions.Count > 0 ? expressions[^1].ResultType : CommonSymbols.Void,
              diagnostics)
    {
        this.Expressions = expressions.ToImmutableList();
    }
}

