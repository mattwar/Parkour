namespace Parkour.Symbols;

public class InterfaceSymbol : TypeSymbol
{
    public override bool IsInterface => true;

    /// <summary>
    /// The definition without substitution of type parameters.
    /// </summary>
    public new InterfaceSymbol? Definition => base.Definition as InterfaceSymbol;

    public InterfaceSymbol(
        string name,
        Symbol? declaringSymbol,
        Access access,
        BitSet<Modifier> modifiers,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        InterfaceSymbol? definition = null)
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
            definition)
    {
    }

    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.Definition ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new InterfaceSymbol(
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

        return new InterfaceSymbol(
            this.Name,
            newDeclaringSymbol,
            this.Access,
            this.Modifiers,
            this.TypeParameters.Count > 0 ? me => this.TypeParameters : null,
            this.TypeArguments.Count > 0 ? () => context.Substitute(this.TypeArguments) : null,
            this.BaseTypes.Count > 0 ? () => context.Substitute(this.BaseTypes) : null,
            this.Members.Count > 0 ? me => context.Substitute(this.Members) : null,
            this.Attributes.Count > 0 ? me => this.Attributes.SelectSame(a => a.Substitute(context)) : null,
            this.Definition ?? this
            );
    }
}
