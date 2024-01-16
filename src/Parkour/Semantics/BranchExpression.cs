namespace Parkour.Semantics;
using Symbols;

public sealed class BranchExpression : Expression
{
    public string TargetName { get; }
    public LabelSymbol? Target { get; }
    public Expression? Expression { get; }

    public BranchExpression(
        string targetName,
        Expression? expression,
        ISourceLocation? location,
        LabelSymbol? target,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              (expression != null ? expression.State : ContainsState.None),
              location,
              resultType,
              diagnostics)
    {
        this.TargetName = targetName;
        this.Target = target;
        this.Expression = expression;
    }

    public bool IsBreak => this.TargetName == "break";
    public bool IsContinue => this.TargetName == "continue";
    public bool IsReturn => this.TargetName == "return";
    public bool IsGoto => !IsBreak && !IsContinue && !IsReturn;

    public static BranchExpression CreateBreak(Expression? expression, ISourceLocation? location, LabelSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression("break", expression, location, target, target != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public static BranchExpression CreateContinue(ISourceLocation? location, LabelSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression("continue", null, location, target, target != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);

    public static BranchExpression CreateReturn(Expression? expression, ISourceLocation? location, LabelSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression("return", expression, location, target, target != null ? SpecialSymbols.DoesNotReturn : null, diagnostics);
}

