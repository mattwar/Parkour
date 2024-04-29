namespace Parkour.Symbols;

public static class SpecialSymbols
{
    /// <summary>
    /// The type is unknown.
    /// </summary>
    public static readonly TypeSymbol Unknown = new ClassSymbol("Unknown");

    /// <summary>
    /// The type of a namespace symbol
    /// </summary>
    public static readonly TypeSymbol Namespace = new ClassSymbol("Namespace");

    /// <summary>
    /// The type of a null literal, not assigned.
    /// </summary>
    public static readonly TypeSymbol Null = new ClassSymbol("Null");

    /// <summary>
    /// Any type
    /// </summary>
    public static readonly TypeSymbol Any = new ClassSymbol("Any");

    /// <summary>
    /// Void (unit) type.
    /// </summary>
    public static readonly TypeSymbol Void = new ClassSymbol("Void");

    /// <summary>
    /// Similar to None and Void, but indicates the expression does not actually return.
    /// </summary>
    public static readonly TypeSymbol DoesNotReturn = new ClassSymbol("DoesNotReturn");
}
