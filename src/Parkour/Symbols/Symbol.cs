namespace Parkour.Symbols;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Symbol
    : ISymbol
{
    private string DebugText => $"{GetType().Name}: {Name}";

    public string Name { get; }

    protected Symbol(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The number of symbols declared by this symbol
    /// </summary>
    public virtual int DeclarationCount => 0;

    /// <summary>
    /// Gets the nth symbol declared by this symbol.
    /// </summary>
    public virtual Symbol? GetDeclaration(int index) => null;
}
