namespace Parkour.Symbols;

public class StructSymbol : TypeSymbol
{
    public StructSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        TypeSymbol? constructedFrom)
        : base(
            name, 
            declaringSymbol, 
            access, 
            modifiers, 
            fnTypeParameters,
            fnTypeArguments,
            fnBaseTypes,
            fnMembers,
            fnAttributes,
            constructedFrom)
    {
    }

    public override bool IsValueType => true;

    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new StructSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => ImmutableList<TypeParameterSymbol>.Empty,
            () => context.TypeArguments,
            () => subContext.Substitute(this.BaseTypes),
            me => subContext.Substitute(this.Members, me),
            me => this.Attributes.SelectSame(a => a.Substitute(subContext)),
            definition);
    }

    internal protected override TypeSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        var newDeclaringSymbol =
            declaringSymbol ?? this.DeclaringSymbol;

        return new StructSymbol(
            this.Name,
            newDeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => this.TypeParameters,
            () => context.Substitute(this.TypeArguments),
            () => context.Substitute(this.BaseTypes),
            me => context.Substitute(this.Members),
            me => this.Attributes.SelectSame(a => a.Substitute(context)),
            this.ConstructedFrom ?? (this.IsConstructable ? this : null));
    }
}
