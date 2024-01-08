namespace Parkour.Expressions;
using Analysis;
using Symbols;

public abstract class Declaration : Expression
{
    public string Name { get; }
    public SymbolAccess Access { get; }
    public SymbolModifier Modifiers { get; }

    private protected Declaration(
        ContainsState state,
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<Diagnostic>? diagnostics)
        : base(state, CommonSymbols.Void, diagnostics)
    {
        this.Name = name;
        this.Access = access;
        this.Modifiers = modifiers;
    }
}