namespace Parkour.Semantics;

using Symbols;

public sealed class StructDeclaration : TypeDeclaration
{
    public override StructSymbol? Symbol { get; }

    private StructDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
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

    public StructDeclaration(
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

    public override StructDeclaration WithName(string name) =>
        name == this.Name ? this :
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
        location == this.Location ? this :
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
        symbol == this.Symbol ? this :
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
        diagnostics == this.Diagnostics ? this :
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
        access == this.Access ? this :
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

    public override StructDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
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
        typeParameters == this.TypeParameters ? this :
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
        baseTypes == this.BaseTypes ? this :
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
        declarations == this.Declarations ? this :
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

    public override StructDeclaration RewriteChildren(SemanticRewriter rewriter)
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