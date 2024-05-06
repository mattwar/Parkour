namespace Parkour.Semantics;

using Symbols;

public class TypeParameterDeclaration : Declaration
{
    public override TypeParameterSymbol? Symbol { get; }

    public TypeParameterDeclaration(
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

    public override TypeParameterDeclaration WithName(string name) =>
        new TypeParameterDeclaration(
            name, 
            this.Location, 
            this.Symbol,
            this.Diagnostics
            );

    public override TypeParameterDeclaration WithLocation(ISourceLocation? location) =>
        new TypeParameterDeclaration(
            this.Name, 
            location, 
            this.Symbol,
            this.Diagnostics
            );

    public TypeParameterDeclaration WithSymbol(TypeParameterSymbol? symbol) =>
        new TypeParameterDeclaration(
            this.Name,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override TypeParameterDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new TypeParameterDeclaration(
            this.Name,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}
