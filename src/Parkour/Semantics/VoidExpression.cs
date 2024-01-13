namespace Parkour.Semantics;
using Symbols;

public sealed class VoidExpression : Expression
{
    public VoidExpression(
        ISourceLocation? location) 
        : base(
            ContainsState.None, 
            location,
            SpecialSymbols.Void, 
            null) 
    { 
    }

    public override Symbol? ReferencedSymbol => SpecialSymbols.Void;

    public static VoidExpression Default = new VoidExpression(null);
}

