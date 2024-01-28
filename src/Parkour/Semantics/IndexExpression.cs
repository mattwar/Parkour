namespace Parkour.Semantics;
using Symbols;

public sealed class IndexExpression : Expression
{
    public Expression Expression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public Symbol? IndexedSymbol { get; }

    public IndexExpression(
        Expression expression,
        ImmutableList<Expression> arguments,
        ISourceLocation? location,
        Symbol? indexedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(expression)
            | CombineState(arguments)
            | NotNullState(indexedSymbol)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Arguments = arguments.ToImmutableList();
        this.IndexedSymbol = indexedSymbol;
    }

    public override int ChildCount => 1 + this.Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => this.Arguments[index - 1]
        };
}

