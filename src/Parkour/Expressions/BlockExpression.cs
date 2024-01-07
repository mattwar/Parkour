namespace Parkour.Expressions;
using Analysis;

public sealed class BlockExpression : Expression
{
    public ImmutableList<Expression> Expressions { get; }

    public BlockExpression(
        ImmutableList<Expression> expressions,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(
              CombineState(expressions),
              expressions.Count > 0 ? expressions[^1].ResultType : SymbolModel.Void,
              diagnostics)
    {
        this.Expressions = expressions.ToImmutableList();
    }
}

