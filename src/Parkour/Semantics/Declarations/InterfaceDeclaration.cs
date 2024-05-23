namespace Parkour.Semantics;
using Symbols;

public sealed class InterfaceDeclaration : TypeDeclaration
{
    public override InterfaceSymbol? Symbol { get; }

    private InterfaceDeclaration(
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

    public InterfaceDeclaration(
        string name,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location)
        : this(
              name, 
              SymbolAccess.Public, 
              SymbolModifier.None, 
              ImmutableList<TypeParameterDeclaration>.Empty, 
              baseTypes, 
              declarations, 
              location, 
              null, 
              null)
    {
    }

    public override InterfaceDeclaration WithName(string name) =>
        name == this.Name ? this :
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
        location == this.Location ? this :
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
        symbol == this.Symbol ? this :
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
        diagnostics == this.Diagnostics ? this :
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
        access == this.Access ? this :
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
        modifiers == this.Modifiers ? this :
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
        typeParameters == this.TypeParameters ? this :
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
        baseTypes == this.BaseTypes ? this :
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
        declarations == this.Declarations ? this :
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

    public override InterfaceDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var typeParams = rewriter.Rewrite(this.TypeParameters);
        var baseTypes = rewriter.Rewrite(this.BaseTypes);
        var declarations = rewriter.Rewrite(this.Declarations);
        return this
            .WithTypeParameters(typeParams)
            .WithBaseTypes(baseTypes)
            .WithDeclarations(declarations);
    }
}
