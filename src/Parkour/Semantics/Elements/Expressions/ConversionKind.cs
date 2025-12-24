namespace Parkour.Semantics;

public enum ConversionKind
{
    /// <summary>
    /// No conversion possible between the two types
    /// </summary>
    None,

    /// <summary>
    /// The two types are the same and do not need conversion.
    /// </summary>
    SameType,

    /// <summary>
    /// Conversion to Unknown type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Source type is DoesNotReturn, so it can techically be assigned to any target type.
    /// </summary>
    DoesNotReturn,

    /// <summary>
    /// Value type conversion to object.
    /// </summary>
    Boxing,

    /// <summary>
    /// Numeric narrowing: long -> int
    /// </summary>
    Narrowing,

    /// <summary>
    /// Numeric widening: int -> long
    /// </summary>
    Widening,

    /// <summary>
    /// conversion to base type
    /// </summary>
    BaseType,

    /// <summary>
    /// Conversion via a custom implicit conversion operator
    /// </summary>
    CustomImplicit,

    /// <summary>
    /// Conversion via a custom explicit conversion operator
    /// </summary>
    CustomExplicit,
}

