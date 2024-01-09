namespace Parkour.Expressions;
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
        TargetSymbol? target,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
              expression != null ? expression.State : ContainsState.None,
              expression != null ? expression.ResultType : CommonSymbols.Void,
              diagnostics,
              syntax)
    {
        this.TargetName = targetName;
        this.Target = target;
        this.Expression = expression;
    }

    public bool IsBreak => this.TargetName == "break";
    public bool IsContinue => this.TargetName == "continue";
    public bool IsReturn => this.TargetName == "return";
    public bool IsGoto => !IsBreak && !IsContinue && !IsReturn;

    public static BranchExpression CreateBreak(Expression? expression, TargetSymbol? target, ImmutableList<Diagnostic>? diagnostics, SyntaxElement? syntax) =>
        new BranchExpression("break", expression, target, diagnostics, syntax);

    public static BranchExpression CreateContinue(TargetSymbol? target, ImmutableList<Diagnostic>? diagnostics, SyntaxElement? syntax) =>
        new BranchExpression("continue", null, target, diagnostics, syntax);

    public static BranchExpression CreateReturn(Expression? expression, TargetSymbol? target, ImmutableList<Diagnostic>? diagnostics, SyntaxElement? syntax) =>
        new BranchExpression("return", expression, target, diagnostics, syntax);
}

