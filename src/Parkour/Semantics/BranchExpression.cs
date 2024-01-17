namespace Parkour.Semantics;
using Symbols;

public sealed class BranchExpression : Expression
{
    public string TargetName { get; }
    public LabelSymbol? TargetSymbol { get; }
    public Expression? Expression { get; }

    public BranchExpression(
        string targetName,
        Expression? expression,
        ISourceLocation? location,
        LabelSymbol? target,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              OptionalState(expression)
              | NotNullOrDiagnosticState(target, diagnostics)
              | NotNullState(resultType),
              location,
              resultType,
              diagnostics)
    {
        this.TargetName = targetName;
        this.TargetSymbol = target;
        this.Expression = expression;
    }

    public bool IsBreak => this.TargetName == LabelSymbol.BreakLabelName;
    public bool IsContinue => this.TargetName == LabelSymbol.ContinueLabelName;
    public bool IsReturn => this.TargetName == LabelSymbol.ReturnLabelName;
    public bool IsGoto => !IsBreak && !IsContinue && !IsReturn;

    public static BranchExpression CreateBreak(Expression? expression, ISourceLocation? location, LabelSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression(LabelSymbol.BreakLabelName, expression, location, target, target != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public static BranchExpression CreateContinue(ISourceLocation? location, LabelSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression(LabelSymbol.ContinueLabelName, null, location, target, target != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public static BranchExpression CreateReturn(Expression? expression, ISourceLocation? location, LabelSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression(LabelSymbol.ReturnLabelName, expression, location, target, target != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);
}

