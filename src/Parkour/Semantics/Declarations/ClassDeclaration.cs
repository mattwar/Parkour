namespace Parkour.Semantics;

using Symbols;

public sealed class ClassDeclaration : TypeDeclaration
{
    public override ClassSymbol? Symbol { get; }

    private ClassDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        ClassSymbol? symbol,
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

    public ClassDeclaration(
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

    public override ClassDeclaration WithName(string name) =>
        name == this.Name ? this : 
        new ClassDeclaration(
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

    public override ClassDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new ClassDeclaration(
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

    public ClassDeclaration WithSymbol(ClassSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new ClassDeclaration(
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

    public override ClassDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new ClassDeclaration(
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

    public override ClassDeclaration WithAccess(SymbolAccess access) =>
        access == this.Access ? this :
        new ClassDeclaration(
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

    public override ClassDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new ClassDeclaration(
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

    public override ClassDeclaration WithTypeParameters(ImmutableList<TypeParameterDeclaration> typeParameters) =>
        typeParameters == this.TypeParameters ? this :
        new ClassDeclaration(
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

    public override ClassDeclaration WithBaseTypes(ImmutableList<Expression> baseTypes) =>
        baseTypes == this.BaseTypes ? this : 
        new ClassDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            baseTypes,
            this.Declarations,
            this.Location,
            symbol: null,
            diagnostics: null
            );

    public override ClassDeclaration WithDeclarations(ImmutableList<Declaration> declarations) =>
        declarations == this.Declarations ? this :
        new ClassDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            declarations,
            this.Location,
            symbol: null,
            diagnostics: null
            );

    public override ClassDeclaration RewriteChildren(SemanticRewriter rewriter)
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

