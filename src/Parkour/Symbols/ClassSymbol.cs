namespace Parkour.Symbols;

public class ClassSymbol : TypeSymbol
{
    public override bool IsClass => true;

    public ClassSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>> fnTypeParameters,
        Func<ImmutableList<TypeSymbol>> fnTypeArguments,
        Func<ImmutableList<TypeSymbol>> fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>> fnMembers,
        TypeSymbol? constructedFrom)
        : base(name, declaringSymbol, access, modifiers, fnTypeParameters, fnTypeArguments, fnBaseTypes, fnMembers, constructedFrom)
    {
    }

    public ClassSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        ImmutableList<TypeParameterSymbol> typeParameters,
        ImmutableList<TypeSymbol> typeArguments,
        ImmutableList<TypeSymbol> baseTypes,
        ImmutableList<Symbol> members,
        TypeSymbol? constructedFrom)
        : this(
              name,
              declaringSymbol,
              access,
              modifiers,
              me => typeParameters,
              () => typeArguments,
              () => baseTypes,
              me => members,
              constructedFrom)
    {
    }

    public ClassSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            ImmutableList<TypeParameterSymbol>.Empty,
            ImmutableList<TypeSymbol>.Empty,
            ImmutableList<TypeSymbol>.Empty,
            ImmutableList<Symbol>.Empty,
            constructedFrom: null)
    {
    }

    public ClassSymbol(string name)
        : this(
            name,
            declaringSymbol: null,
            SymbolAccess.Public,
            SymbolModifier.None)
    {
    }

    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new ClassSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => ImmutableList<TypeParameterSymbol>.Empty,
            () => context.TypeArguments,
            () => subContext.Substitute(this.BaseTypes),
            me => subContext.Substitute(this.Members, me),
            definition);
    }

    internal protected override TypeSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        return new ClassSymbol(
            this.Name,
            declaringSymbol ?? this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => this.TypeParameters,
            () => context.Substitute(this.TypeArguments),
            () => context.Substitute(this.BaseTypes),
            me => context.Substitute(this.Members),
            this.ConstructedFrom);
    }
}