namespace Parkour.Semantics;

using Symbols;

public class TypeParameterDeclaration : Declaration
{
    public override TypeParameterSymbol? Symbol { get; }

    private TypeParameterDeclaration(
        string name,
        ISourceLocation? location,
        TypeParameterSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullOrDiagnosticState(symbol, diagnostics),
            name,
            location,
            diagnostics)
    {
        this.Symbol = symbol;
    }

    public TypeParameterDeclaration(
        string name,
        ISourceLocation? location)
        : this(name, location, null, null)
    {
    }

    public override TypeParameterDeclaration WithName(string name) =>
        name == this.Name ? this :
        new TypeParameterDeclaration(
            name, 
            this.Location, 
            this.Symbol,
            this.Diagnostics
            );

    public override TypeParameterDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new TypeParameterDeclaration(
            this.Name, 
            location, 
            this.Symbol,
            this.Diagnostics
            );

    public TypeParameterDeclaration WithSymbol(TypeParameterSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new TypeParameterDeclaration(
            this.Name,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override TypeParameterDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new TypeParameterDeclaration(
            this.Name,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
    public override SemanticElement RewriteChildren(SemanticRewriter rewriter) => this;
}
