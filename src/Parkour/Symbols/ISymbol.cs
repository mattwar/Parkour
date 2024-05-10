namespace Parkour;

public interface ISymbol
{
    /// <summary>
    /// The name of the symbol
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The full name of the symbol, including namespace, etc.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// The kind of the symbol.
    /// </summary>
    string Kind { get; }
}
