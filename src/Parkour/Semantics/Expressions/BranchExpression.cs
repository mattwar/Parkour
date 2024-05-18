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
        LabelSymbol? target,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              State(expression)
              | NotNullOrDiagnosticState(target, diagnostics)
              | NotNullState(resultType),
              location,
              resultType,
              diagnostics)
    {
        this.LabelName = labelName;
        this.LabelSymbol = target;
        this.Expression = expression;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Expression,
            _ => null
        };

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
}

