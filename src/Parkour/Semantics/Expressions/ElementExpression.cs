namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Return the indexed element from the array or indexable instance.
/// </summary>
public sealed class ElementExpression : Expression
{
    public Expression Expression { get; }
    public ImmutableList<Expression> Arguments { get; }
    public IndexerSymbol? IndexerSymbol { get; }

    private ElementExpression(
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

    public ElementExpression(
        Expression expression,
        ImmutableList<Expression> arguments,
        ISourceLocation? location)
        : this(expression, arguments, location, null, null, null)
    {
    }

    public override ElementExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ElementExpression(
            this.Expression,
            this.Arguments,
            location,
            this.IndexerSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override ElementExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ElementExpression(
            this.Expression,
            this.Arguments,
            this.Location,
            this.IndexerSymbol,
            this.ResultType,
            diagnostics
            );

    public override ElementExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ElementExpression(
            this.Expression,
            this.Arguments,
            this.Location,
            this.IndexerSymbol,
            resultType,
            this.Diagnostics
            );

    public ElementExpression WithExpression(Expression expression) =>
        expression == this.Expression ? this :
        new ElementExpression(
            expression,
            this.Arguments,
            this.Location,
            this.IndexerSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ElementExpression WithArguments(ImmutableList<Expression> arguments) =>
        arguments == this.Arguments ? this :
        new ElementExpression(
            this.Expression,
            arguments,
            this.Location,
            this.IndexerSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public ElementExpression WithIndexerSymbol(IndexerSymbol? symbol) =>
        symbol == this.IndexerSymbol ? this :
        new ElementExpression(
            this.Expression,
            this.Arguments,
            this.Location,
            symbol,
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

    public override ElementExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        var arguments = rewriter.Rewrite(this.Arguments);
        return this
            .WithExpression(expression!)
            .WithArguments(arguments);
    }
}