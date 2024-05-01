namespace Parkour.Symbols;

/// <summary>
/// Operator functions that are unbound.
/// </summary>
public static class OperatorKind
{
    public const string Add = nameof(Add);
    public const string Subtract = nameof(Subtract);
    public const string Multiply = nameof(Multiply);
    public const string Divide = nameof(Divide);
    public const string Remainder = nameof(Remainder);
    public const string Negate = nameof(Negate);
    public const string UnaryPlus = nameof(UnaryPlus);

    public const string Increment = nameof(Increment);
    public const string Decrement = nameof(Decrement);

    public const string ShiftLeft = nameof(ShiftLeft);
    public const string ShiftRight = nameof(ShiftRight);

    public const string BitwiseAnd = nameof(BitwiseAnd);
    public const string BitwiseOr = nameof(BitwiseOr);
    public const string BitwiseXor = nameof(BitwiseXor);
    public const string BitwiseNot = nameof(BitwiseNot);

    public const string LogicalAnd = nameof(LogicalAnd);
    public const string LogicalOr = nameof(LogicalOr);
    public const string LogicalXor = nameof(LogicalXor);
    public const string LogicalNot = nameof(LogicalNot);
    public const string LogicalAndAlso = nameof(LogicalAndAlso);
    public const string LogicalOrElse = nameof(LogicalOrElse);

    public const string Equal = nameof(Equal);
    public const string NotEqual = nameof(NotEqual);
    public const string LessThan = nameof(LessThan);
    public const string LessThanOrEqual = nameof(LessThanOrEqual);
    public const string GreaterThan = nameof(GreaterThan);
    public const string GreaterThanOrEqual = nameof(GreaterThanOrEqual);

    public const string True = nameof(True);
    public const string False = nameof(False);
}
