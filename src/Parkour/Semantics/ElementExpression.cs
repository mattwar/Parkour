namespace Parkour.Semantics;
using Symbols;

public sealed class ElementExpression : Expression
{
    public Expression Expression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public IndexerSymbol? IndexerSymbol { get; }

    public ElementExpression(
        Expression expression,
        ImmutableList<Expression> arguments,
        ISourceLocation? location,
        IndexerSymbol? indexerSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | CombineState(arguments)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Arguments = arguments.ToImmutableList();
        this.IndexerSymbol = indexerSymbol;
    }

    public override int ChildCount => 1 + this.Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => this.Arguments[index - 1]
        };
}

