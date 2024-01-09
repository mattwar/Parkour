namespace Parkour.Expressions;
using Symbols;
using Syntax;

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
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            state, 
            CommonSymbols.Void, 
            diagnostics,
            syntax)
    {
        this.Name = name;
        this.Access = access;
        this.Modifiers = modifiers;
    }
}