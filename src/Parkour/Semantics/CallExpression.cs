namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class CallExpression : Expression
{
    public Expression Expression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public Symbol? CalledSymbol { get; }

    public CallExpression(
        Expression expression,
        ImmutableList<Expression> arguments,
        ISourceLocation? location,
        Symbol? symbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              expression.State | arguments.Aggregate(ContainsState.None, (s, e) => e.State | s),
              location,
              resultType,
              diagnostics)
    {
        this.Expression = expression;
        this.Arguments = arguments.ToImmutableList();
        this.CalledSymbol = symbol;
    }
}

