namespace Parkour.Semantics;
using Symbols;

public sealed class VoidExpression : Expression
{
    public VoidExpression(
        ISourceLocation? location) 
        : base(
            ContainsState.None, 
            location,
            CommonSymbols.Void, 
            null) 
    { 
    }

    public override Symbol? ReferencedSymbol => CommonSymbols.Void;

    public static VoidExpression Default = new VoidExpression(null);
}

