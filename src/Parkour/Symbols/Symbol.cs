namespace Parkour.Symbols;

public abstract class Symbol
{
    public string Name { get; }
    public virtual ImmutableList<Symbol> Members => ImmutableList<Symbol>.Empty;

    protected Symbol(string name)
    {
        Name = name;
    }
}