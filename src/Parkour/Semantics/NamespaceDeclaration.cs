namespace Parkour.Semantics;
using Symbols;
using Syntax;

public class NamespaceDeclaration : MemberDeclaration
{
    public ImmutableList<Declaration> Declarations { get; }
    public NamespaceSymbol? Symbol { get; }

    public NamespaceDeclaration(
        string name, 
        ImmutableList<Declaration> declarations,
        ISourceLocation? location,
        NamespaceSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(declarations), 
            name, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            location,
            diagnostics)
    {
        this.Declarations = declarations;
        this.Symbol = symbol;
    }
}
