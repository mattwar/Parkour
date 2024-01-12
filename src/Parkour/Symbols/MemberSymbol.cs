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
                string? fname = null;

                if (this.Container != null)
                {
                    if (this.Container.FullName.Length > 0)
                    {
                        fname = this.Container.FullName + "." + this.Name;
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

                Interlocked.CompareExchange(ref _fullName, fname, null);
            }

            return _fullName;
        }
    }
}
