namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Calls the method or delagate identified by the expression
/// with the specified arguments.
/// </summary>
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

    public override CallExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new CallExpression(
            this.Expression,
            this.Arguments,
            location,
            this.CalledSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override CallExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new CallExpression(
            this.Expression,
            this.Arguments,
            this.Location,
            this.CalledSymbol,
            this.ResultType,
            diagnostics
            );

    public override CallExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new CallExpression(
            this.Expression,
            this.Arguments,
            this.Location,
            this.CalledSymbol,
            resultType,
            this.Diagnostics
            );

    public CallExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new CallExpression(
            expression,
            this.Arguments,
            this.Location,
            this.CalledSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public CallExpression WithArguments(ImmutableList<Expression> arguments) =>
        arguments == this.Arguments ? this :
        new CallExpression(
            this.Expression,
            arguments,
            this.Location,
            this.CalledSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public CallExpression WithCalledSymbol(Symbol? calledSymbol) =>
        calledSymbol == this.CalledSymbol ? this :
        new CallExpression(
            this.Expression,
            this.Arguments,
            this.Location,
            calledSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1 + this.Arguments.Count;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => this.Arguments[index - 1]
        };

    public override CallExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        var arguments = rewriter.Rewrite(this.Arguments);
        return this
            .WithExpression(expression!)
            .WithArguments(arguments);
    }
}

