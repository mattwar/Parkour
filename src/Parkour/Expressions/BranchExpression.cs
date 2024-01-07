namespace Parkour.Expressions;
using Symbols;
using Analysis;

public sealed class BranchExpression : Expression
{
    public string TargetName { get; }
    public TargetSymbol? Target { get; }
    public Expression? Expression { get; }

    public BranchExpression(
        string targetName,
        Expression? expression,
        TargetSymbol? target,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(
              expression != null ? expression.State : ContainsState.None,
              expression != null ? expression.ResultType : SymbolModel.Void,
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

    public static BranchExpression CreateBreak(Expression? expression = null) =>
        new BranchExpression("break", expression, null);

    public static BranchExpression CreateContinue() =>
        new BranchExpression("continue", null, null);

    public static BranchExpression CreateReturn(Expression? expression = null) =>
        new BranchExpression("return", expression, null);
}

