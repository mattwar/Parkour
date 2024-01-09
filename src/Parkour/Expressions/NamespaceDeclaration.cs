namespace Parkour.Expressions;
using Symbols;
using Syntax;

public class NamespaceDeclaration : Declaration
{
    public ImmutableList<Declaration> Declarations { get; }

    public NamespaceDeclaration(
        string name, 
        ImmutableList<Declaration> declarations, 
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            CombineState(declarations), 
            name, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            diagnostics,
            syntax)
    {
        this.Declarations = declarations;
    }
}
