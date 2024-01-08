namespace Parkour.Expressions;
using Symbols;

public sealed class VoidExpression : Expression
{
    private VoidExpression() : base(ContainsState.None, CommonSymbols.Void, null) { }
    public static VoidExpression Instance = new VoidExpression();
}

