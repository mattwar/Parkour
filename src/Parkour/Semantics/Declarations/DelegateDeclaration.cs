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
}
