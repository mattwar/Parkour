namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class BlockExpression : Expression
{
    public ImmutableList<Expression> Expressions { get; }

    public BlockExpression(
        ImmutableList<Expression> expressions,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
              CombineState(expressions),
              expressions.Count > 0 ? expressions[^1].ResultType : CommonSymbols.Void,
              diagnostics,
              syntax)
    {
        this.Expressions = expressions.ToImmutableList();
    }
}

