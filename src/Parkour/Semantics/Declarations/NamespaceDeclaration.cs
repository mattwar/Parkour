namespace Parkour.Semantics;
using Symbols;
using System.Xml.Linq;

public class NamespaceDeclaration : MemberDeclaration
{
    public override NamespaceSymbol? Symbol { get; }

    public ImmutableList<Declaration> Declarations { get; }

    private NamespaceDeclaration(
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

    public NamespaceDeclaration(
        string name,
        ImmutableList<Declaration> declarations,
        ISourceLocation? location)
        : this(name, declarations, location, null, null)
    {
    }

    public override NamespaceDeclaration WithName(string name) =>
        name == this.Name ? this :
        new NamespaceDeclaration(
            name,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Declarations,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public NamespaceDeclaration WithSymbol(NamespaceSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new NamespaceDeclaration(
            this.Name,
            this.Declarations,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override NamespaceDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
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

    public NamespaceDeclaration WithDeclarations(ImmutableList<Declaration> declarations) =>
        declarations == this.Declarations ? this :
        new NamespaceDeclaration(
            this.Name,
            declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public bool IsGlobalNamespace => 
        this.Name == "";

    public override int ChildCount =>
        this.Declarations.Count;

    public override SemanticElement? GetChild(int index) =>
        this.Declarations[index];

    public override NamespaceDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var declarations = rewriter.Rewrite(this.Declarations);
        return this.WithDeclarations(declarations);
    }
}
