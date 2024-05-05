namespace Parkour.Semantics;
using Symbols;

public class NamespaceDeclaration : MemberDeclaration
{
    public ImmutableList<Declaration> Declarations { get; }
    public NamespaceSymbol? NamespaceSymbol { get; }

    public NamespaceDeclaration(
        string name, 
        ImmutableList<Declaration> declarations,
        ISourceLocation? location,
        NamespaceSymbol? namespaceSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(declarations)
            | NotNullState(namespaceSymbol), 
            name, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            location,
            diagnostics)
    {
        this.Declarations = declarations;
        this.NamespaceSymbol = namespaceSymbol;    
    }

    public override Symbol? DeclaredSymbol => this.NamespaceSymbol;

    public bool IsGlobalNamespace => 
        this.Name == "";

    public override int ChildCount =>
        this.Declarations.Count;

    public override SemanticElement? GetChild(int index) =>
        this.Declarations[index];
}
