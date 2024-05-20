namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Represents zero or more expressions.
/// The result of the block is the result of the final expression.
/// The block is considered void if it has no expressions or the last expression is void.
/// </summary>
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

    public override BlockExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new BlockExpression(
            this.Expressions,
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override BlockExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new BlockExpression(
            this.Expressions,
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override BlockExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new BlockExpression(
            this.Expressions,
            this.Location,
            resultType,
            this.Diagnostics
            );

    public BlockExpression WithExpressions(ImmutableList<Expression> expressions) =>
        expressions == this.Expressions ? this :
        new BlockExpression(
            expressions,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount =>
        this.Expressions.Count;

    public override SemanticElement? GetChild(int index) =>
        this.Expressions[index];

    public override BlockExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expressions = rewriter.Rewrite(this.Expressions);
        return this.WithExpressions(expressions);
    }
}

