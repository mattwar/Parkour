namespace Parkour.Semantics;
using Symbols;

public sealed class BlockExpression : Expression
{
    public ImmutableList<Expression> Expressions { get; }

    public BlockExpression(
        ImmutableList<Expression> expressions,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              CombineState(expressions),
              location,
              resultType,
              diagnostics)
    {
        this.Expressions = expressions.ToImmutableList();
    }

    public override int ChildCount => 
        this.Expressions.Count;

    public override SemanticElement? GetChild(int index) =>
        this.Expressions[index];
}

