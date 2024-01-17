namespace Parkour.Semantics;
using Symbols;

public sealed class CallExpression : Expression
{
    public Expression Expression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public Symbol? CalledSymbol { get; }

    public CallExpression(
        Expression expression,
        ImmutableList<Expression> arguments,
        ISourceLocation? location,
        Symbol? calledSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            expression.State 
            | CombineState(arguments)
            | NotNullState(calledSymbol)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Expression = expression;
        this.Arguments = arguments.ToImmutableList();
        this.CalledSymbol = calledSymbol;
    }
}

