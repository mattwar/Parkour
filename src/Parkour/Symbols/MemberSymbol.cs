namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class MemberSymbol : Symbol
{
    private string DebugText => $"{GetType().Name}: {FullName}";

    /// <summary>
    /// The accessibility of the symbol.
    /// </summary>
    public Access Access { get; }

    /// <summary>
    /// Declaration modifiers for the symbol.
    /// </summary>
    public BitSet<Modifier> Modifiers { get; }

    /// <summary>
    /// The symbol that declares this symbol.
    /// </summary>
    public Symbol? DeclaringSymbol { get; }

    /// <summary>
    /// True if the member is the definition, and not one that has type parameters substituted.
    /// </summary>
    public bool IsDefinition => this.Definition == null;

    /// <summary>
    /// The defintion of this symbol without substitution of type parameters.
    /// </summary>
    public MemberSymbol? Definition { get; }

    public bool IsPublic => Access is RuntimeAccess.Public;
    public bool IsPrivate => Access is RuntimeAccess.Private;
    public bool IsProtected => Access is RuntimeAccess.Protected;
    public bool IsProtectedAndInternal => Access is RuntimeAccess.ProtectedAndInternal;
    public bool IsProtectedOrInternal => Access is RuntimeAccess.ProtectedOrInternal;
    public bool IsInternal => Access is RuntimeAccess.Internal;

    /// <summary>
    /// True if the symbol is considered to be static.
    /// </summary>
    public bool IsStatic => this.Modifiers.Contains(Modifier.Static);

    /// <summary>
    /// True if the symbol is considered to be abstract.
    /// </summary>
    public bool IsAbstract => this.Modifiers.Contains(Modifier.Abstract);

    /// <summary>
    /// True if the symbol is considered to be virtual.
    /// </summary>
    public bool IsVirtual => this.Modifiers.Contains(Modifier.Virtual);

    /// <summary>
    /// True if the symbol is considered to be an override.
    /// </summary>
    public bool IsOverride => this.Modifiers.Contains(Modifier.Override);

    /// <summary>
    /// True if the symbol is considered to be sealed.
    /// </summary>
    public bool IsSealed => this.Modifiers.Contains(Modifier.Sealed);

    /// <summary>
    /// True if the symbol is considered to be hidden.
    /// </summary>
    public bool IsHideBySig => this.Modifiers.Contains(Modifier.HideBySig);

    /// <summary>
    /// True if the symbol is considered to be special.
    /// </summary>
    public bool IsSpecial => this.Modifiers.Contains(Modifier.Special);

    /// <summary>
    /// True if the symbol is considered to be read only.
    /// </summary>
    public bool IsReadOnly => this.Modifiers.Contains(Modifier.ReadOnly);

    /// <summary>
    /// True if the symbol is constant.
    /// </summary>
    public bool IsConstant => this.Modifiers.Contains(Modifier.Constant);

    public MemberSymbol(
        string name, 
        Symbol? declaringSymbol,
        Access access,
        BitSet<Modifier> modifiers,
        MemberSymbol? definition)
        : base(name)
    {
        this.DeclaringSymbol = declaringSymbol;
        this.Access = access;
        this.Modifiers = modifiers;
        this.Definition = definition;
    }

    /// <summary>
    /// The namespace this symbol is declared within.
    /// </summary>
    public string Namespace => 
        this.DeclaringSymbol switch
        {
           NamespaceSymbol ns => ns.FullName,
           MemberSymbol ms => ms.Namespace,
           _ => ""
        };

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

    protected virtual string GetFullName()
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
