namespace Parkour.Semantics;
using Symbols;

public abstract class TypeDeclaration : MemberDeclaration
{
    public ImmutableList<TypeParameterDeclaration> TypeParameters { get; }
    public ImmutableList<Expression> BaseTypes { get; }
    public ImmutableList<Declaration> Declarations { get; }

    private protected TypeDeclaration(
        ContainsState state,
        string name,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterDeclaration> typeParameters,
        ImmutableList<Expression> baseTypes,
        ImmutableList<Declaration>? declarations,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
    : base(
        state
        | CombineState(typeParameters)
        | CombineState(declarations),
        name,
        access,
        modifiers,
        location,
        diagnostics)
    {
        this.TypeParameters = typeParameters;
        this.BaseTypes = baseTypes;
        this.Declarations = declarations ?? ImmutableList<Declaration>.Empty;
    }

    public abstract TypeDeclaration WithTypeParameters(ImmutableList<TypeParameterDeclaration> typeParameters);
    public abstract TypeDeclaration WithBaseTypes(ImmutableList<Expression> baseTypes);
    public abstract TypeDeclaration WithDeclarations(ImmutableList<Declaration> declarations);

    public override int ChildCount =>
        this.TypeParameters.Count
        + this.BaseTypes.Count
        + this.Declarations.Count;

    public override SemanticElement? GetChild(int index)
    {
        if (index < this.TypeParameters.Count)
            return this.TypeParameters[index];
        index -= this.TypeParameters.Count;
        if (index < this.BaseTypes.Count)
            return this.BaseTypes[index];
        index -= this.BaseTypes.Count;
        if (index < this.Declarations.Count)
            return this.Declarations[index];
        return null;
    }

    protected static ImmutableList<Declaration>? WithDefaultConstructor(
        ImmutableList<Declaration>? declarations, ISourceLocation? location)
    {
        declarations ??= ImmutableList<Declaration>.Empty;

        // include a default constructor if no instance constructor is specified
        if (declarations.Any(d => d is ConstructorDeclaration cd && (cd.Modifiers & SymbolModifier.Static) == 0))
            return declarations;

        return declarations.Add(SemanticFactory.Constructor(location));
    }
}
