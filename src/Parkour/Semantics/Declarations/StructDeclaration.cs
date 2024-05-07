namespace Parkour.Semantics;

using Symbols;

public sealed class StructDeclaration : TypeDeclaration
{
    public override StructSymbol? Symbol { get; }

    public StructDeclaration(
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        StructSymbol? symbol,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        NotNullOrDiagnosticState(symbol, diagnostics),
        name,
        access,
        modifiers,
        typeParameters,
        baseTypes,
        WithDefaultConstructor(declarations, location),
        location,
        diagnostics)
    {
        this.Symbol = symbol;
    }

    public override StructDeclaration WithName(string name) =>
        new StructDeclaration(
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

    public override StructDeclaration WithLocation(ISourceLocation? location) =>
        new StructDeclaration(
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

    public StructDeclaration WithSymbol(StructSymbol? symbol) =>
        new StructDeclaration(
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

    public override StructDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        new StructDeclaration(
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

    public override StructDeclaration WithAccess(SymbolAccess access) =>
        new StructDeclaration(
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

    public override StructDeclaration WithModifiers(SymbolModifier modifiers) =>
        new StructDeclaration(
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

    public override StructDeclaration WithTypeParameters(ImmutableList<TypeParameterDeclaration> typeParameters) =>
        new StructDeclaration(
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

    public override StructDeclaration WithBaseTypes(ImmutableList<Expression> baseTypes) =>
        new StructDeclaration(
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

    public override StructDeclaration WithDeclarations(ImmutableList<Declaration> declarations) =>
        new StructDeclaration(
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