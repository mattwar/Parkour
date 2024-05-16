namespace Parkour.Semantics;
using Symbols;

public abstract class MemberDeclaration : Declaration
{
    public SymbolAccess Access { get; }
    public BitSet<SymbolModifier> Modifiers { get; }

    private protected MemberDeclaration(
        ContainsState state,
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            state,
            name,
            location,
            diagnostics)
    {
        Access = access;
        Modifiers = modifiers;
    }

    public abstract MemberDeclaration WithAccess(SymbolAccess access);
    public abstract MemberDeclaration WithModifiers(BitSet<SymbolModifier> modifiers);
}
