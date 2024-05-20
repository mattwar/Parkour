namespace Parkour.Semantics;

using Symbols;

public sealed class DelegateDeclaration : TypeDeclaration
{
    public override DelegateSymbol? Symbol { get; }

    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression ReturnType { get; }

    public DelegateDeclaration(
        string name,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration> declarations,
        ImmutableList<ParameterDeclaration> parameters,
        Expression returnType,
        ISourceLocation? location,
        DelegateSymbol? symbol,
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
        this.Parameters = parameters;
        this.ReturnType = returnType;
        this.Symbol = symbol;
    }

    public override DelegateDeclaration WithName(string name) =>
        name == this.Name ? this :
        new DelegateDeclaration(
            name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override DelegateDeclaration WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            location,
            this.Symbol,
            this.Diagnostics
            );

    public DelegateDeclaration WithSymbol(DelegateSymbol? symbol) =>
        symbol == this.Symbol ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            symbol,
            this.Diagnostics
            );

    public override DelegateDeclaration WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            diagnostics
            );

    public override DelegateDeclaration WithAccess(SymbolAccess access) =>
        access == this.Access ? this :
        new DelegateDeclaration(
            this.Name,
            access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override DelegateDeclaration WithModifiers(BitSet<SymbolModifier> modifiers) =>
        modifiers == this.Modifiers ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override DelegateDeclaration WithTypeParameters(ImmutableList<TypeParameterDeclaration> typeParameters) =>
        typeParameters == this.TypeParameters ? this : 
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            typeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override DelegateDeclaration WithBaseTypes(ImmutableList<Expression> baseTypes) =>
        baseTypes == this.BaseTypes ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            baseTypes,
            this.Declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override DelegateDeclaration WithDeclarations(ImmutableList<Declaration> declarations) =>
        declarations == this.Declarations ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            declarations,
            this.Parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public DelegateDeclaration WithParameters(ImmutableList<ParameterDeclaration> parameters) =>
        parameters == this.Parameters ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            parameters,
            this.ReturnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public DelegateDeclaration WithReturnType(Expression returnType) =>
        returnType == this.ReturnType ? this :
        new DelegateDeclaration(
            this.Name,
            this.Access,
            this.Modifiers,
            this.TypeParameters,
            this.BaseTypes,
            this.Declarations,
            this.Parameters,
            returnType,
            this.Location,
            this.Symbol,
            this.Diagnostics
            );

    public override int ChildCount =>
        base.ChildCount + this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index)
    {
        if (index <= base.ChildCount)
            return base.GetChild(index);
        index -= base.ChildCount;
        if (index < this.Parameters.Count)
            return this.Parameters[index];
        index -= this.Parameters.Count;
        return index == 0 ? this.ReturnType : null;
    }

    public override DelegateDeclaration RewriteChildren(SemanticRewriter rewriter)
    {
        var baseTypes = rewriter.Rewrite(this.BaseTypes);
        var typeParameters = rewriter.Rewrite(this.TypeParameters);
        var parameters = rewriter.Rewrite(this.Parameters);
        var returnType = rewriter.Rewrite(this.ReturnType);
        var declarations = rewriter.Rewrite(this.Declarations);
        return this
            .WithBaseTypes(baseTypes)
            .WithTypeParameters(typeParameters)
            .WithParameters(parameters)
            .WithReturnType(returnType!)
            .WithDeclarations(declarations);
    }
}
