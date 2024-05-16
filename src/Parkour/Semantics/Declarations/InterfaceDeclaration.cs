namespace Parkour.Semantics;
using Symbols;

public sealed class InterfaceDeclaration : TypeDeclaration
{
    public override InterfaceSymbol? Symbol { get; }

    public InterfaceDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        InterfaceSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        NotNullOrDiagnosticState(symbol, diagnostics),
        name,
        access,
        modifiers,
        typeParameters,
        baseTypes,
        declarations,
        location,
        diagnostics)
    {
        this.Symbol = symbol;
    }

    public override InterfaceDeclaration WithName(string name) =>
        new InterfaceDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override InterfaceDeclaration WithLocation(ISourceLocation? location) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public InterfaceDeclaration WithSymbol(InterfaceSymbol? symbol) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override InterfaceDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override InterfaceDeclaration WithAccess(SymbolAccess access) =>
        new InterfaceDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override InterfaceDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override InterfaceDeclaration WithTypeParameters(ImmutableList<TypeParameterDeclaration> typeParameters) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            typeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override InterfaceDeclaration WithBaseTypes(ImmutableList<Expression> baseTypes) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            baseTypes,
            this.Declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override InterfaceDeclaration WithDeclarations(ImmutableList<Declaration> declarations) =>
        new InterfaceDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            declarations,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );
}
