namespace Parkour;

/// <summary>
/// Represents a unique language element identified during semantic analysis.
/// Typically used for resolved name reference, either declared in source or from loaded metadata,
/// such as types, methods, properties and fields.
/// </summary>
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
