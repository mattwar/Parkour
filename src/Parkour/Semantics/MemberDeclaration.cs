namespace Parkour.Semantics;
using Symbols;
using Syntax;

public abstract class MemberDeclaration : Declaration
{
    public SymbolAccess Access { get; }
    public SymbolModifier Modifiers { get; }

    private protected MemberDeclaration(
        ContainsState state,
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
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
}
