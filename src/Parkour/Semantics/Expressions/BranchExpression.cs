namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// Evaluates the expression (if specified)
/// and branches to the location identified by the label.
/// </summary>
public sealed class BranchExpression : Expression
{
    public string LabelName { get; }
    public LabelSymbol? LabelSymbol { get; }
    public Expression? Expression { get; }

    public BranchExpression(
        string labelName,
        Expression? expression,
        ISourceLocation? location,
        LabelSymbol? labelSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              State(expression)
              | NotNullOrDiagnosticState(labelSymbol, diagnostics)
              | NotNullState(resultType),
              location,
              resultType,
              diagnostics)
    {
        this.LabelName = labelName;
        this.LabelSymbol = labelSymbol;
        this.Expression = expression;
    }

    public bool IsBreak => this.LabelName == LabelSymbol.BreakLabelName;
    public bool IsContinue => this.LabelName == LabelSymbol.ContinueLabelName;
    public bool IsReturn => this.LabelName == LabelSymbol.ReturnLabelName;
    public bool IsGoto => !IsBreak && !IsContinue && !IsReturn;

    public static BranchExpression CreateBreak(Expression? expression, ISourceLocation? location, LabelSymbol? labelSymbol, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression(LabelSymbol.BreakLabelName, expression, location, labelSymbol, labelSymbol != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public static BranchExpression CreateContinue(ISourceLocation? location, LabelSymbol? labelSymbol, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression(LabelSymbol.ContinueLabelName, null, location, labelSymbol, labelSymbol != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public static BranchExpression CreateReturn(Expression? expression, ISourceLocation? location, LabelSymbol? labelSymbol, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression(LabelSymbol.ReturnLabelName, expression, location, labelSymbol, labelSymbol != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public override BranchExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new BranchExpression(
            this.LabelName,
            this.Expression,
            location,
            this.LabelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override BranchExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new BranchExpression(
            this.LabelName,
            this.Expression,
            this.Location,
            this.LabelSymbol,
            this.ResultType,
            diagnostics
            );

    public override BranchExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new BranchExpression(
            this.LabelName,
            this.Expression,
            this.Location,
            this.LabelSymbol,
            resultType,
            this.Diagnostics
            );

    public BranchExpression WithLabelName(string labelName) =>
        labelName == this.LabelName ? this :
        new BranchExpression(
            labelName,
            this.Expression,
            this.Location,
            this.LabelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public BranchExpression WithExpression(Expression? expression) =>
        expression == this.Expression ? this :
        new BranchExpression(
            this.LabelName,
            expression,
            this.Location,
            this.LabelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public BranchExpression WithLabelSymbol(LabelSymbol? labelSymbol) =>
        labelSymbol == this.LabelSymbol ? this :
        new BranchExpression(
            this.LabelName,
            this.Expression,
            this.Location,
            labelSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => null
        };

    public override BranchExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var expression = rewriter.Rewrite(this.Expression);
        return this.WithExpression(expression);
    }
}

