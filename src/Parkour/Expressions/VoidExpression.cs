namespace Parkour.Expressions;
using Symbols;

public sealed class VoidExpression : Expression
{
    private VoidExpression() 
        : base(
            ContainsState.None, 
            CommonSymbols.Void, 
            null,
            null) 
    { 
    }

    public static VoidExpression Instance = new VoidExpression();
}

