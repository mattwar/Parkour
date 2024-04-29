namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class MemberSymbol : Symbol
{
    private string DebugText => $"{GetType().Name}: {FullName}";

    /// <summary>
    /// The accessibility of the symbol.
    /// </summary>
    public SymbolAccess Access { get; }

    /// <summary>
    /// Declaration modifiers for the symbol.
    /// </summary>
    public SymbolModifier Modifiers { get; }

    /// <summary>
    /// The symbol that declares this symbol.
    /// </summary>
    public Symbol? DeclaringSymbol { get; }

    public bool IsPublic => Access == SymbolAccess.Public;
    public bool IsPrivate => Access == SymbolAccess.Private;
    public bool IsProtected => Access == SymbolAccess.Protected;
    public bool IsProtectedAndInternal => Access == SymbolAccess.ProtectedAndInternal;
    public bool IsProtectedOrInternal => Access == SymbolAccess.ProtectedOrInternal;
    public bool IsInternal => Access == SymbolAccess.Internal;

    /// <summary>
    /// True if the symbol is considered to be static.
    /// </summary>
    public bool IsStatic => (Modifiers & SymbolModifier.Static) != 0;

    /// <summary>
    /// True if the symbol is considered to be abstract.
    /// </summary>
    public bool IsAbstract => (Modifiers & SymbolModifier.Abstract) != 0;

    /// <summary>
    /// True if the symbol is considered to be virtual.
    /// </summary>
    public bool IsVirtual => (Modifiers & SymbolModifier.Virtual) != 0;

    /// <summary>
    /// True if the symbol is considered to be an override.
    /// </summary>
    public bool IsOverride => (Modifiers & SymbolModifier.Override) != 0;

    /// <summary>
    /// True if the symbol is considered to be sealed.
    /// </summary>
    public bool IsSealed => (Modifiers & SymbolModifier.Sealed) != 0;

    /// <summary>
    /// True if the symbol is considered to be hidden.
    /// </summary>
    public bool IsHideBySig => (Modifiers & SymbolModifier.HideBySig) != 0;

    /// <summary>
    /// True if the symbol is considered to be special.
    /// </summary>
    public bool IsSpecial => (Modifiers & SymbolModifier.Special) != 0;

    /// <summary>
    /// True if the symbol is considered to be read only.
    /// </summary>
    public bool IsReadOnly => (this.Modifiers & SymbolModifier.ReadOnly) != 0;

    public MemberSymbol(
        string name, 
        Symbol? declaringSymbol,
        SymbolAccess access,
        SymbolModifier modifiers)
        : base(name)
    {
        this.DeclaringSymbol = declaringSymbol;
        this.Access = access;
        this.Modifiers = modifiers;
    }

    /// <summary>
    /// The namespace this symbol is declared within.
    /// </summary>
    public string Namespace => 
        this.DeclaringSymbol is MemberSymbol ms
            ? ms.Namespace 
            : "";

    /// <summary>
    /// The type this symbol is declared within.
    /// </summary>
    public virtual TypeSymbol? DeclaringType =>
        this.DeclaringSymbol is TypeSymbol typeSymbol ? typeSymbol
        : this.DeclaringSymbol is MemberSymbol memberSymbol ? memberSymbol.DeclaringType
        : null;

    private string? _fullName;
    
    public override string FullName
    {
        get
        {
            if (_fullName == null)
            {
                var tmp = GetFullName();
                Interlocked.CompareExchange(ref _fullName, tmp, null);
            }

            return _fullName;
        }
    }

    protected string GetFullName()
    {
        string? fname = null;

        if (this.DeclaringSymbol is MemberSymbol ms)
        {
            if (ms.FullName.Length > 0)
            {
                fname = ms.FullName + "." + this.Name;
            }
            else
            {
                fname = this.Name;
            }
        }
        else if (this.Namespace.Length > 0)
        {
            fname = this.Namespace + "." + this.Name;
        }
        else
        {
            fname = this.Name;
        }

        if (this.Arity > 0)
        {
            var typeArgs =
                this is TypeSymbol type ? type.TypeArguments
                : this is MethodSymbol method ? method.TypeArguments
                : ImmutableList<TypeSymbol>.Empty;

            if (typeArgs.Count > 0)
            {
                var typeArgList = string.Join(", ", typeArgs.Select(ta => ta.FullName));
                fname += $"[{typeArgList}]";
            }
            else
            {
                fname = fname + "`" + this.Arity;
            }
        }

        return fname;
    }
}
