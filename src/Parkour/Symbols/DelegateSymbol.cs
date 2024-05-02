namespace Parkour.Symbols;

public class DelegateSymbol : TypeSymbol
{
    private Func<DelegateSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
    private ImmutableList<ParameterSymbol>? _parameters;

    public ImmutableList<ParameterSymbol> Parameters
    {
        get
        {
            if (_parameters == null && _fnParameters is { } fn)
            {
                _fnParameters = null;
                var tmp = fn(this);
                Interlocked.CompareExchange(ref _parameters, tmp, null);
            }

            return _parameters ?? ImmutableList<ParameterSymbol>.Empty;
        }
    }

    private Func<TypeSymbol>? _fnReturnType;
    private TypeSymbol? _returnType;

    public TypeSymbol ReturnType
    {
        get
        {
            if (_returnType == null && _fnReturnType is { } fn)
            {
                _fnReturnType = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _returnType, tmp, null);
            }

            return _returnType ?? SpecialSymbols.Unknown;
        }
    }

    private DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>>? fnParameters,
        Func<TypeSymbol>? fnReturnType,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        TypeSymbol? constructedFrom)
        : base(name, declaringSymbol, access, modifiers, fnTypeParameters, fnTypeArguments, fnBaseTypes, fnMembers, constructedFrom)
    {
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
    }

    private DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers)
        : this(name, declaringSymbol, access, modifiers, null, null, null, null, null, null, null)
    {
    }

    public DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            fnParameters,
            fnReturnType,
            null, null, null, null, null)
    {
    }

    public DelegateSymbol(
        string name,
        Symbol? declaringSymbol,
        Func<DelegateSymbol, ImmutableList<ParameterSymbol>> fnParameters,
        Func<TypeSymbol> fnReturnType)
        : this(
            name,
            declaringSymbol,
            SymbolAccess.Public,
            SymbolModifier.None,
            fnParameters,
            fnReturnType,
            null, null, null, null, null)
    {
    }

    internal protected override TypeSymbol Construct(ConstructionContext context)
    {
        var definition = this.ConstructedFrom ?? this;
        var subContext = context.CreateSubstitution(definition.TypeParameters);

        return new DelegateSymbol(
            this.Name,
            this.DeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => subContext.Substitute(this.Parameters),
            () => subContext.Substitute(this.ReturnType),
            me => ImmutableList<TypeParameterSymbol>.Empty,
            () => context.TypeArguments,
            () => subContext.Substitute(this.BaseTypes),
            me => subContext.Substitute(this.Members, me),
            definition);
    }

    internal protected override TypeSymbol Substitute(SubstitutionContext context, Symbol? declaringSymbol)
    {
        var newDeclaringSymbol =
            declaringSymbol ?? this.DeclaringSymbol;

        return new DelegateSymbol(
            this.Name,
            newDeclaringSymbol,
            this.Access,
            this.Modifiers,
            me => context.Substitute(this.Parameters),
            () => context.Substitute(this.ReturnType),
            me => this.TypeParameters,
            () => context.Substitute(this.TypeArguments),
            () => context.Substitute(this.BaseTypes),
            me => context.Substitute(this.Members),
            this.ConstructedFrom ?? (this.IsConstructable ? this : null));
    }

    public override int DeclarationCount =>
        this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        this.Parameters[index];

    public override int ReferenceCount =>
        this.DeclarationCount + 1;

    public override Symbol? GetReference(int index)
    {
        if (index <= this.DeclarationCount)
            return this.GetDeclaration(index);

        index -= this.DeclarationCount;

        if (index == 0)
            return this.ReturnType;

        return null;
    }
}
