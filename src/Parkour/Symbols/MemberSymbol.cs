namespace Parkour.Symbols;

public abstract class MemberSymbol : Symbol
{
    public abstract MemberSymbol? Container { get; }
    public virtual SymbolAccess Access => SymbolAccess.Public;
    public virtual SymbolModifier Modifiers => SymbolModifier.None;

    public bool IsStatic => (Modifiers & SymbolModifier.Static) != 0;

    public MemberSymbol(string name)
        : base(name)
    {
    }

    public string Namespace => this.Container != null ? this.Container.Namespace : "";

    private string? _fullName;
    public string FullName
    {
        get
        {
            if (_fullName == null)
            {
                var fn =
                    this.Container != null ? this.Container.FullName + "." + this.Name
                    : this.Namespace.Length > 0 ? this.Namespace + "." + Name
                    : Name;
                Interlocked.CompareExchange(ref _fullName, fn, null);
            }

            return _fullName;
        }
    }
}
