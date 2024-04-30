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
            State(expression) 
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

    public override int ChildCount => 1 + this.Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => this.Arguments[index - 1]
        };
}

