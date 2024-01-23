using System.Reflection;

namespace Parkour.Symbols;

public class MethodSymbol : MemberSymbol
{
    public MemberSymbol? DeclaringSymbol { get; }
    public override MemberSymbol? Container => DeclaringSymbol;
    public override SymbolAccess Access { get; }
    public override SymbolModifier Modifiers { get; }

    private Func<ImmutableList<TypeParameterSymbol>>? _fnTypeParameters;
    private ImmutableList<TypeParameterSymbol>? _typeParameters;
    private Func<ImmutableList<TypeSymbol>>? _fnTypeArguments;
    private ImmutableList<TypeSymbol>? _typeArguments;
    private Func<MethodSymbol>? _fnDefinition;
    private MethodSymbol? _definition;
    private Func<MethodSymbol, ImmutableList<ParameterSymbol>>? _fnParameters;
    private ImmutableList<ParameterSymbol>? _parameters;
    private Func<TypeSymbol>? _fnReturnType;
    private TypeSymbol? _returnType;

    /// <summary>
    /// <see cref="TypeParameters"/> for generic method definitions.
    /// </summary>
    public ImmutableList<TypeParameterSymbol> TypeParameters
    {
        get
        {
            if (_typeParameters == null && _fnTypeParameters is { } fn)
            {
                _fnTypeParameters = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _typeParameters, tmp, null);
            }

            return _typeParameters ?? ImmutableList<TypeParameterSymbol>.Empty;
        }
    }

    /// <summary>
    /// Type arguments for constructed generic methods
    /// </summary>
    public ImmutableList<TypeSymbol> TypeArguments
    {
        get
        {
            if (_typeArguments == null && _fnTypeArguments is { } fn)
            {
                _fnTypeArguments = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _typeArguments, tmp, null);
            }

            return _typeArguments ?? ImmutableList<TypeSymbol>.Empty;
        }
    }

    /// <summary>
    /// The generic definition for this constructed method.
    /// </summary>
    public MethodSymbol? Definition 
    { 
        get
        {
            if (_definition == null && _fnDefinition is { } fn)
            {
                _fnDefinition = null;
                var tmp = fn();
                Interlocked.CompareExchange(ref _definition, tmp, null);
            }

            return _definition;
        }
    }

    /// <summary>
    /// True if the method is generic (definition or constructed)
    /// </summary>
    public bool IsGeneric => IsDefinition || IsConstructed;

    /// <summary>
    /// True if the method is a generic method definition.
    /// </summary>
    public bool IsDefinition => this.TypeParameters.Count > 0;

    /// <summary>
    /// True if the method is a constructed generic method.
    /// </summary>
    public bool IsConstructed => this.TypeArguments.Count > 0;

    /// <summary>
    /// The parameters of this method.
    /// </summary>
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

            return _parameters!;
        }
    }

    /// <summary>
    /// The return type of this method.
    /// </summary>
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

            return _returnType!;
        }
    }

    public MethodBase? RuntimeMethod { get; }

    public MethodSymbol(
        string name,
        MemberSymbol? declaringSymbol, 
        SymbolAccess access, 
        SymbolModifier modifiers, 
        Func<ImmutableList<TypeParameterSymbol>>? fnTypeParameters,
        Func<ImmutableList<TypeSymbol>>? fnTypeArguments,
        Func<MethodSymbol, ImmutableList<ParameterSymbol>>? fnParameters, 
        Func<TypeSymbol>? fnReturnType, 
        Func<MethodSymbol>? fnGenericDefinition,
        MethodBase? runtimeMethod)
        : base(name)
    {
        DeclaringSymbol = declaringSymbol;
        Access = access;
        Modifiers = modifiers;
        _fnTypeParameters = fnTypeParameters;
        _fnTypeArguments = fnTypeArguments;
        _fnParameters = fnParameters;
        _fnReturnType = fnReturnType;
        _fnDefinition = fnGenericDefinition;
        RuntimeMethod = runtimeMethod;
    }

    public MethodSymbol(
        string name,
        MemberSymbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifier,
        ImmutableList<TypeParameterSymbol> typeParameters,
        ImmutableList<TypeSymbol> typeArguments,
        ImmutableList<ParameterSymbol> parameters,
        TypeSymbol returnType,
        MethodSymbol? genericDefinition,
        MethodBase? runtimeMethod)
        : this(
              name, 
              declaringSymbol, 
              access, 
              modifier, 
              () => typeParameters, 
              () => typeArguments,
              me => parameters, 
              () => returnType, 
              genericDefinition != null ? () => genericDefinition : null,
              runtimeMethod)
    {
    }

    public override int DeclarationCount =>
        this.TypeParameters.Count + this.Parameters.Count;

    public override Symbol? GetDeclaration(int index) =>
        index < this.TypeParameters.Count
            ? this.TypeParameters[index]
            : this.Parameters[index = this.TypeParameters.Count];
}
