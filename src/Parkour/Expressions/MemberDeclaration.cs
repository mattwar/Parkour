namespace Parkour.Expressions;
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
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            state,
            name,
            diagnostics,
            syntax)
    {
        Access = access;
        Modifiers = modifiers;
    }
}
