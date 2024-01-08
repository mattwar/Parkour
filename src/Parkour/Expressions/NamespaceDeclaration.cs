namespace Parkour.Expressions;
using Symbols;

public class NamespaceDeclaration : Declaration
{
    public ImmutableList<Declaration> Declarations { get; }

    public NamespaceDeclaration(string name, ImmutableList<Declaration> declarations, ImmutableList<Diagnostic>? diagnostics = null)
        : base(CombineState(declarations), name, SymbolAccess.Public, SymbolModifier.None, diagnostics)
    {
        this.Declarations = declarations;
    }
}
