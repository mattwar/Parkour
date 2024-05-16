namespace Parkour.Semantics;
using Symbols;

public class NamespaceDeclaration : MemberDeclaration
{
    public override NamespaceSymbol? Symbol { get; }

    public ImmutableList<Declaration> Declarations { get; }

    public NamespaceDeclaration(
        string name, 
        ImmutableList<Declaration> declarations,
        ISourceLocation? location,
        NamespaceSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(declarations)
            | NotNullState(symbol), 
            name, 
            SymbolAccess.Public, 
            SymbolModifier.None, 
            location,
            diagnostics)
    {
        this.Declarations = declarations;
        this.Symbol = symbol;    
    }

    public override NamespaceDeclaration WithName(string name) =>
        new NamespaceDeclaration(
            name,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithLocation(ISourceLocation? location) =>
        new NamespaceDeclaration(
            this.Name,
            this.Declarations,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public NamespaceDeclaration WithSymbol(NamespaceSymbol? symbol) =>
        new NamespaceDeclaration(
            this.Name,
            this.Declarations,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new NamespaceDeclaration(
            this.Name,
            this.Declarations,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override NamespaceDeclaration WithAccess(SymbolAccess access) =>
        this;

    public override NamespaceDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        this;

    public bool IsGlobalNamespace => 
        this.Name == "";

    public override int ChildCount =>
        this.Declarations.Count;

    public override SemanticElement? GetChild(int index) =>
        this.Declarations[index];
}
