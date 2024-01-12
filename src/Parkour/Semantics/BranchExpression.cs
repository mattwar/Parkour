namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class BranchExpression : Expression
{
    public string TargetName { get; }
    public TargetSymbol? Target { get; }
    public Expression? Expression { get; }

    public BranchExpression(
        string targetName,
        Expression? expression,
        ISourceLocation? location,
        TargetSymbol? target,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
              expression != null ? expression.State : ContainsState.None,
              location,
              expression != null ? expression.ResultType : CommonSymbols.Void,
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

    public static BranchExpression CreateBreak(Expression? expression, ISourceLocation? location, TargetSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression("break", expression, location, target, diagnostics);

    public static BranchExpression CreateContinue(ISourceLocation? location, TargetSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression("continue", null, location, target, diagnostics);

    public static BranchExpression CreateReturn(Expression? expression, ISourceLocation? location, TargetSymbol? target, ImmutableList<Diagnostic>? diagnostics) =>
        new BranchExpression("return", expression, location, target, diagnostics);
}

