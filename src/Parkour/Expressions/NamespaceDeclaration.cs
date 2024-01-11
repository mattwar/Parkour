namespace Parkour.Expressions;
using Symbols;
using Syntax;

public class NamespaceDeclaration : MemberDeclaration
{
    public ImmutableList<Declaration> Declarations { get; }
    public NamespaceSymbol? Symbol { get; }

    public NamespaceDeclaration(
        string name, 
        ImmutableList<Declaration> declarations, 
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax,
        NamespaceSymbol? symbol)
        : base(
            CombineState(declarations), 
            name, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            diagnostics,
            syntax)
    {
        this.Declarations = declarations;
        this.Symbol = symbol;
    }
}
