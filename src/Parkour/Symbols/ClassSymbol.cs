namespace Parkour.Symbols;

public class ClassSymbol : TypeSymbol
{
    public override bool IsClass => true;

    public ClassSymbol(
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

    public ClassSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers)
        : this(
              name, 
              declaringSymbol, 
              access, 
              modifiers, 
              null, 
              null, 
              null, 
              null,
              null,
              null)
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
            fnTypeParameters: null,
            () => context.TypeArguments,
            this.BaseTypes.Count > 0 ? () => subContext.Substitute(this.BaseTypes) : null,
            this.Members.Count > 0 ? me => subContext.Substitute(this.Members, me) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(subContext)) : null,
            definition
            );
    }

    internal protected override TypeSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        var newDeclaringSymbol =
            declaringSymbol ?? this.DeclaringSymbol;

        return new ClassSymbol(
            this.Name,
            newDeclaringSymbol,
            this.Access,
            this.Modifiers,
            this.TypeParameters.Count > 0 ? me => this.TypeParameters : null,
            this.TypeArguments.Count > 0 ? () => context.Substitute(this.TypeArguments) : null,
            this.BaseTypes.Count > 0 ? () => context.Substitute(this.BaseTypes) : null,
            this.Members.Count > 0 ? me => context.Substitute(this.Members) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.ConstructedFrom ?? (this.IsConstructable ? this : null)
            );
    }
}