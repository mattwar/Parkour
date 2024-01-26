namespace Parkour.Symbols;

public abstract class MemberSymbol : Symbol
{
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

    /// <summary>
    /// True if the member is consiered to be static.
    /// </summary>
    public bool IsStatic => (Modifiers & SymbolModifier.Static) != 0;

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

    private string? _fullName;
    public string FullName
    {
        get
        {
            if (_fullName == null)
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
                    fname = fname + "`" + this.Arity;
                }

                Interlocked.CompareExchange(ref _fullName, fname, null);
            }

            return _fullName;
        }
    }
}
