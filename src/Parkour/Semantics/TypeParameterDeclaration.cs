namespace Parkour.Semantics;
using Symbols;

public class TypeParameterDeclaration : Declaration
{
    public TypeParameterSymbol? TypeParameterSymbol { get; }

    public TypeParameterDeclaration(
        string name,
        ISourceLocation? location,
        TypeParameterSymbol? typeParameterSymbol,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullOrDiagnosticState(typeParameterSymbol, diagnostics),
            name,
            location,
            diagnostics)
    {
        this.TypeParameterSymbol = typeParameterSymbol;
    }

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}
