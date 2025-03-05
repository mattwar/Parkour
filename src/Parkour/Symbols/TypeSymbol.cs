
namespace Parkour.Symbols;

public abstract class TypeSymbol : ContainerSymbol
{
    /// <summary>
    /// The type parameters for this generic type definition.
    /// </summary>
    public ImmutableList<TypeParameterSymbol> TypeParameters =>
        _lazyTypeParameters?.Value ?? ImmutableList<TypeParameterSymbol>.Empty;
    private readonly Lazy<ImmutableList<TypeParameterSymbol>>? _lazyTypeParameters;

    /// <summary>
    /// The type arguments for this constructed generic type.
    /// </summary>
    public ImmutableList<TypeSymbol> TypeArguments =>
        _lazyTypeArguments?.Value ?? ImmutableList<TypeSymbol>.Empty;
    private readonly Lazy<ImmutableList<TypeSymbol>>? _lazyTypeArguments;

    /// <summary>
    /// The base type and interfaces of this type.
    /// </summary>
    public ImmutableList<TypeSymbol> BaseTypes =>
        _lazyBaseTypes?.Value ?? ImmutableList<TypeSymbol>.Empty;
    private readonly Lazy<ImmutableList<TypeSymbol>>? _lazyBaseTypes;

    /// <summary>
    /// The members of this type.
    /// </summary>
    public override ImmutableList<Symbol> Members =>
        _lazyMembers?.Value ?? ImmutableList<Symbol>.Empty;
    private readonly Lazy<ImmutableList<Symbol>>? _lazyMembers;

    /// <summary>
    /// Custom attributes for this type
    /// </summary>
    public override ImmutableList<AttributeInfo> Attributes =>
        _lazyAttributes?.Value ?? ImmutableList<AttributeInfo>.Empty;
    private readonly Lazy<ImmutableList<AttributeInfo>>? _lazyAttributes;
    
    protected TypeSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol, ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<ImmutableList<TypeSymbol>>? fnBaseTypes,
        Func<TypeSymbol, ImmutableList<Symbol>>? fnMembers,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes,
        TypeSymbol? definition)
        : base(name, declaringSymbol, access, modifiers, definition)
    {
        _lazyTypeParameters = fnTypeParameters != null
            ? new Lazy<ImmutableList<TypeParameterSymbol>>(() => fnTypeParameters(this))
            : null;
        _lazyTypeArguments = fnTypeArguments != null
            ? new Lazy<ImmutableList<TypeSymbol>>(fnTypeArguments)
            : null;
        _lazyBaseTypes = fnBaseTypes != null
            ? new Lazy<ImmutableList<TypeSymbol>>(fnBaseTypes)
            : null;
        _lazyAttributes = fnAttributes != null
            ? new Lazy<ImmutableList<AttributeInfo>>(() => fnAttributes(this))
            : null;
        _lazyMembers = fnMembers != null
            ? new Lazy<ImmutableList<Symbol>>(() => fnMembers(this))
            : null;
    }

    protected TypeSymbol(
        string name,
        Symbol? declaringSymbol,
        SymbolAccess access,
        BitSet<SymbolModifier> modifiers,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes)
        : this(
            name,
            declaringSymbol,
            access,
            modifiers,
            null,
            null,
            null,
            null,
            fnAttributes,
            null)
    {
    }

    protected TypeSymbol(
        string name,
        Func<TypeSymbol, ImmutableList<AttributeInfo>>? fnAttributes)
        : this(
              name,
              null,
              SymbolAccess.Public,
              SymbolModifier.None,
              fnAttributes)
    {
    }

    protected TypeSymbol(string name)
        : this(
            name,
            null,
            SymbolAccess.Public,
            SymbolModifier.None,
            null)
    {
    }

    /// <summary>
    /// True if this type is a generic type definition or a constructed generic type.
    /// </summary>
    public bool IsGeneric => 
        this.TypeParameters.Count > 0
        || this.TypeArguments.Count > 0;

    /// <summary>
    /// True if this type is a constructed generic type.
    /// </summary>
    public bool IsConstructed => 
        IsGeneric && this.TypeArguments.Count > 0;

    /// <summary>
    /// The definition of the type without substituted type parameters.
    /// </summary>
    public new TypeSymbol? Definition => 
        base.Definition as TypeSymbol;

    /// <summary>
    /// True if the type is an interface
    /// </summary>
    public virtual bool IsInterface => false;

    /// <summary>
    /// True if the type is a value type.
    /// </summary>
    public virtual bool IsValueType => false;

    /// <summary>
    /// True if the type is an array.
    /// </summary>
    public virtual bool IsArray => false;

    /// <summary>
    /// True if the type is a class
    /// </summary>
    public virtual bool IsClass => false;

    /// <summary>
    /// True if the type is constructable
    /// </summary>
    public override bool IsConstructable =>
        this.IsGeneric;

    public override int Arity =>
        IsConstructed
            ? this.TypeArguments.Count
            : this.TypeParameters.Count;

    public override int DeclaredSymbolCount =>
        this.TypeParameters.Count + this.Members.Count;

    public override Symbol? GetDeclaredSymbol(int index) =>
        index < this.TypeParameters.Count
            ? this.TypeParameters[index]
            : this.Members[index - this.TypeParameters.Count];

    public override int ReferencedSymbolCount =>
        this.TypeParameters.Count + this.TypeArguments.Count + this.Members.Count;

    public override Symbol? GetReferencedSymbol(int index)
    {
        if (index < this.TypeParameters.Count)
            return this.TypeParameters[index];
        
        index -= this.TypeParameters.Count;

        if (index < this.TypeArguments.Count)
            return this.TypeArguments[index];

        index -= this.TypeArguments.Count;

        if (index < this.Members.Count)
            return this.Members[index];

        return null;
    }

    public bool IsSubTypeOf(TypeSymbol baseType)
    {
        foreach (var bt in this.BaseTypes)
        {
            if (bt == baseType 
                || bt.IsSubTypeOf(baseType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the first matching nested type.
    /// </summary>
    public TypeSymbol? FindType(string name, int arity = 0, Func<TypeSymbol, bool>? predicate = null) =>
        this.GetFirstMember<TypeSymbol>(name, t => t.Arity == arity && (predicate == null || predicate(t)));

    /// <summary>
    /// Returns the first matching constructor.
    /// </summary>
    public ConstructorSymbol? FindConstructor(ImmutableList<TypeSymbol> parameterTypes, Func<ConstructorSymbol, bool>? predicate = null) =>
        this.GetFirstMember<ConstructorSymbol>(c => ParametersMatch(c.Parameters, parameterTypes) && (predicate == null || predicate(c)));

    /// <summary>
    /// Returns the first matching method.
    /// </summary>
    public MethodSymbol? FindMethod(string? name, ImmutableList<TypeSymbol> parameterTypes, Func<MethodSymbol, bool>? predicate = null) =>
        this.GetFirstMember<MethodSymbol>(name, m => m.Arity == 0 && ParametersMatch(m.Parameters, parameterTypes) && (predicate == null || predicate(m)));

    /// <summary>
    /// Returns the first matching method.
    /// </summary>
    public MethodSymbol? FindMethod(string? name, ImmutableList<TypeSymbol> typeArguments, ImmutableList<TypeSymbol> parameterTypes, Func<MethodSymbol, bool>? predicate = null) =>
        this.GetFirstMember<MethodSymbol>(name, m => m.Arity == typeArguments.Count && TypesMatch(m.TypeArguments, typeArguments) && ParametersMatch(m.Parameters, parameterTypes) && (predicate == null || predicate(m)));

    /// <summary>
    /// Returns the first matching property.
    /// </summary>
    public PropertySymbol? FindProperty(string name, Func<PropertySymbol, bool>? predicate = null) =>
        this.GetFirstMember(name, predicate);

    /// <summary>
    /// Returns the first matching field.
    /// </summary>
    public FieldSymbol? FindField(string name, Func<FieldSymbol, bool>? predicate = null) =>
        this.GetFirstMember(name, predicate);

    /// <summary>
    /// Returns the first matching property or field.
    /// </summary>
    public MemberSymbol? FindPropertyOrField(string name, Func<MemberSymbol, bool>? predicate = null) =>
        this.GetFirstMember<MemberSymbol>(name, m => (m is PropertySymbol || m is FieldSymbol) && (predicate == null || predicate(m)));

    /// <summary>
    /// Returns the first matching indexer.
    /// </summary>
    public IndexerSymbol? FindIndexer(ImmutableList<TypeSymbol> parameterTypes, Func<IndexerSymbol, bool>? predicate = null) =>
        this.GetFirstMember<IndexerSymbol>(i => ParametersMatch(i.GetMethod!.Parameters, parameterTypes) && (predicate == null || predicate(i)));

    private static bool TypesMatch(ImmutableList<TypeSymbol> types1, ImmutableList<TypeSymbol> types2)
    {
        if (types1.Count != types2.Count)
            return false;

        var typeComparer = TypeEqualityComparer.Instance;
        for (int i = 0; i < types1.Count; i++)
        {
            if (!typeComparer.Equals(types1[i], types2[i]))
                return false;
        }

        return true;
    }

    private static bool ParametersMatch(ImmutableList<ParameterSymbol> parameters, ImmutableList<TypeSymbol> parameterTypes)
    {
        if (parameters.Count != parameterTypes.Count)
            return false;

        var typeComparer = TypeEqualityComparer.Instance;
        for (int i = 0; i < parameters.Count; i++)
        {
            if (!typeComparer.Equals(parameters[i].Type, parameterTypes[i]))
                return false;
        }

        return true;
    }

    private static readonly ObjectPool<HashSet<TypeSymbol>> _hashSetPool =
        new ObjectPool<HashSet<TypeSymbol>>(() => new HashSet<TypeSymbol>(), hs => hs.Clear());

    /// <summary>
    /// Returns true if this type is assignable to the specified type.
    /// </summary>
    public bool IsAssignableTo(TypeSymbol type)
    {
        var visited = _hashSetPool.AllocateFromPool();
        var result = Check(this);
        _hashSetPool.ReturnToPool(visited);
        return result;

        bool Check(TypeSymbol checkType)
        {
            if (visited.Add(checkType))
            {
                if (TypeEqualityComparer.Instance.Equals(checkType, type))
                    return true;

                // check immediate base types
                foreach (var bt in checkType.BaseTypes)
                {
                    if (TypeEqualityComparer.Instance.Equals(bt, type))
                        return true;
                }

                // check base types of base types
                foreach (var bt in checkType.BaseTypes)
                {
                    if (Check(bt))
                        return true;
                }
            }

            return false;
        }
    }
}