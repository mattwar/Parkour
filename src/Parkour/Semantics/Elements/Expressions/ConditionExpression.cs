namespace Parkour.Semantics;
using Symbols;
using Syntax;

/// <summary>
/// If the Test expression results in true the WhenTrue expression is evaluated,
/// otherwise the WhenFalse expression is evaluated.
/// </summary>
public sealed class ConditionExpression : Expression
{
    public Expression Test { get; }
    public Expression WhenTrue { get; }
    public Expression WhenFalse { get; }

    private ConditionExpression(
        Expression test,
        Expression whenTrue,
        Expression whenFalse,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(test)
            | State(whenTrue)
            | State(whenFalse)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Test = test;
        this.WhenTrue = whenTrue;
        this.WhenFalse = whenFalse;
    }

    public ConditionExpression(
        Expression test,
        Expression whenTrue,
        Expression whenFalse,
        ISourceLocation? location)
        : this(test, whenTrue, whenFalse, location, null, null)
    {
    }

    public override ConditionExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ConditionExpression(
            this.Test,
            this.WhenTrue,
            this.WhenFalse,
            location,
            this.ResultType,
            this.Diagnostics
            );

    public override ConditionExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ConditionExpression(
            this.Test,
            this.WhenTrue,
            this.WhenFalse,
            this.Location,
            this.ResultType,
            diagnostics
            );

    public override ConditionExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new ConditionExpression(
            this.Test,
            this.WhenTrue,
            this.WhenFalse,
            this.Location,
            resultType,
            this.Diagnostics
            );

    public ConditionExpression WithTest(Expression test) =>
        test == this.Test ? this :
        new ConditionExpression(
            test,
            this.WhenTrue,
            this.WhenFalse,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public ConditionExpression WithWhenTrue(Expression whenTrue) =>
        whenTrue == this.WhenTrue ? this :
        new ConditionExpression(
            this.Test,
            whenTrue,
            this.WhenFalse,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public ConditionExpression WithWhenFalse(Expression whenFalse) =>
        whenFalse == this.WhenFalse ? this :
        new ConditionExpression(
            this.Test,
            this.WhenTrue,
            whenFalse,
            this.Location,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 3;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Test,
            1 => this.WhenTrue,
            2 => this.WhenFalse,
            _ => null
        };

    public override ConditionExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var test = rewriter.Rewrite(this.Test);
        var whenTrue = rewriter.Rewrite(this.WhenTrue);
        var whenFalse = rewriter.Rewrite(this.WhenFalse);
        return this
            .WithTest(test!)
            .WithWhenTrue(whenTrue!)
            .WithWhenFalse(whenFalse!);
    }
}

