namespace Parkour.Symbols;

public static class SpecialSymbols
{
    /// <summary>
    /// The type is unknown.
    /// </summary>
    public static readonly TypeSymbol Unknown = new TypeSymbol("Unknown");

    /// <summary>
    /// The type of a namespace symbol
    /// </summary>
    public static readonly TypeSymbol Namespace = new TypeSymbol("Namespace");

    /// <summary>
    /// The type of a null literal, not assigned.
    /// </summary>
    public static readonly TypeSymbol Null = new TypeSymbol("Null");

    /// <summary>
    /// Any type
    /// </summary>
    public static readonly TypeSymbol Any = new TypeSymbol("Any");

    /// <summary>
    /// Void (unit) type.
    /// </summary>
    public static readonly TypeSymbol Void = new TypeSymbol("Void");

    /// <summary>
    /// Similar to None and Void, but indicates the expression does not actually return.
    /// </summary>
    public static readonly TypeSymbol DoesNotReturn = new TypeSymbol("DoesNotReturn");
}
