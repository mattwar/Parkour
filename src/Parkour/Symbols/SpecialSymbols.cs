namespace Parkour.Symbols;

public static class SpecialSymbols
{
    /// <summary>
    /// The result type of an expression with control flow that does not actually return,
    /// (like branch or throw), but otherwise the same as Void.
    /// </summary>
    public static readonly TypeSymbol DoesNotReturn = new ClassSymbol(nameof(DoesNotReturn));

    /// <summary>
    /// The result type of the namespace portion of a type expression.
    /// </summary>
    public static readonly TypeSymbol Namespace = new ClassSymbol(nameof(Namespace));

    /// <summary>
    /// The result type of a constant expression with null value that is not yet inferred from context.
    /// </summary>
    public static readonly TypeSymbol Null = new ClassSymbol(nameof(Null));

    /// <summary>
    /// The result type of an expression when it is not known (unbound or not found).
    /// </summary>
    public static readonly TypeSymbol Unknown = new ClassSymbol(nameof(Unknown));
}
