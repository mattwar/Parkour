namespace Parkour.Expressions;
using Analysis;

public sealed class VoidExpression : Expression
{
    private VoidExpression() : base(ContainsState.None, SymbolModel.Void, null) { }
    public static VoidExpression Instance = new VoidExpression();
}

