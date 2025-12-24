namespace Parkour;

/// <summary>
/// A closed-hierarchy of runtime operators understood by IL emit.
/// </summary>
public class RuntimeOperator : Operator
{
    /// <summary>
    /// Private constructor to close hierarchy.
    /// </summary>
    private RuntimeOperator() { }

    public sealed class Add : RuntimeOperator { private Add() { } public static readonly Add Instance = new(); }
    public sealed class Subtract : RuntimeOperator { private Subtract() { }  public static readonly Subtract Instance = new(); }
    public sealed class Multiply : RuntimeOperator { private Multiply() { } public static readonly Multiply Instance = new(); }
    public sealed class Divide : RuntimeOperator { private Divide() { } public static readonly Divide Instance = new(); }
    public sealed class Remainder : RuntimeOperator { private Remainder() { } public static readonly Remainder Instance = new(); }
    public sealed class Negate : RuntimeOperator { private Negate() { } public static readonly Negate Instance = new(); }
    public sealed class UnaryPlus : RuntimeOperator { private UnaryPlus() { } public static readonly UnaryPlus Instance = new(); }
    public sealed class Increment : RuntimeOperator { private Increment() { } public static readonly Increment Instance = new(); }
    public sealed class Decrement : RuntimeOperator { private Decrement() { } public static readonly Decrement Instance = new(); }
    public sealed class ShiftLeft : RuntimeOperator { private ShiftLeft() { } public static readonly ShiftLeft Instance = new(); }
    public sealed class ShiftRight : RuntimeOperator { private ShiftRight() { } public static readonly ShiftRight Instance = new(); }
    public sealed class BitwiseAnd : RuntimeOperator { private BitwiseAnd() { } public static readonly BitwiseAnd Instance = new(); }
    public sealed class BitwiseOr : RuntimeOperator { private BitwiseOr() { } public static readonly BitwiseOr Instance = new(); }
    public sealed class BitwiseXor : RuntimeOperator { private BitwiseXor() { } public static readonly BitwiseXor Instance = new(); }
    public sealed class BitwiseNot : RuntimeOperator { private BitwiseNot() { } public static readonly BitwiseNot Instance = new(); }
    public sealed class LogicalAnd : RuntimeOperator { private LogicalAnd() { } public static readonly LogicalAnd Instance = new(); }
    public sealed class LogicalOr : RuntimeOperator { private LogicalOr() { } public static readonly LogicalOr Instance = new(); }
    public sealed class LogicalXor : RuntimeOperator { private LogicalXor() { } public static readonly LogicalXor Instance = new(); }
    public sealed class LogicalNot : RuntimeOperator { private LogicalNot() { } public static readonly LogicalNot Instance = new(); }
    public sealed class LogicalAndAlso : RuntimeOperator { private LogicalAndAlso() { } public static readonly LogicalAndAlso Instance = new(); }
    public sealed class LogicalOrElse : RuntimeOperator { private LogicalOrElse() { } public static readonly LogicalOrElse Instance = new(); }
    public sealed class Equal : RuntimeOperator { private Equal() { } public static readonly Equal Instance = new(); }
    public sealed class NotEqual : RuntimeOperator { private NotEqual() { } public static readonly NotEqual Instance = new(); }
    public sealed class LessThan : RuntimeOperator { private LessThan() { } public static readonly LessThan Instance = new(); }
    public sealed class LessThanOrEqual : RuntimeOperator { private LessThanOrEqual() { } public static readonly LessThanOrEqual Instance = new(); }
    public sealed class GreaterThan : RuntimeOperator { private GreaterThan() { } public static readonly GreaterThan Instance = new(); }
    public sealed class GreaterThanOrEqual : RuntimeOperator { private GreaterThanOrEqual() { } public static readonly GreaterThanOrEqual Instance = new(); }
    public sealed class True : RuntimeOperator { private True() { } public static readonly True Instance = new(); }
    public sealed class False : RuntimeOperator { private False() { } public static readonly False Instance = new(); }
}

/// <summary>
/// An extension that provides easy access to runtime operators as static properties on the <see cref="Operator"/> class.
/// </summary>
public static class RuntimeOperatorExtensions
{
    extension(Operator)
    {
        public static RuntimeOperator.Add Add => RuntimeOperator.Add.Instance;
        public static RuntimeOperator.Subtract Subtract => RuntimeOperator.Subtract.Instance;
        public static RuntimeOperator.Multiply Multiply => RuntimeOperator.Multiply.Instance;
        public static RuntimeOperator.Divide Divide => RuntimeOperator.Divide.Instance;
        public static RuntimeOperator.Remainder Remainder => RuntimeOperator.Remainder.Instance;
        public static RuntimeOperator.Negate Negate => RuntimeOperator.Negate.Instance;
        public static RuntimeOperator.UnaryPlus UnaryPlus => RuntimeOperator.UnaryPlus.Instance;
        public static RuntimeOperator.Increment Increment => RuntimeOperator.Increment.Instance;
        public static RuntimeOperator.Decrement Decrement => RuntimeOperator.Decrement.Instance;
        public static RuntimeOperator.ShiftLeft ShiftLeft => RuntimeOperator.ShiftLeft.Instance;
        public static RuntimeOperator.ShiftRight ShiftRight => RuntimeOperator.ShiftRight.Instance;
        public static RuntimeOperator.BitwiseAnd BitwiseAnd => RuntimeOperator.BitwiseAnd.Instance;
        public static RuntimeOperator.BitwiseOr BitwiseOr => RuntimeOperator.BitwiseOr.Instance;
        public static RuntimeOperator.BitwiseXor BitwiseXor => RuntimeOperator.BitwiseXor.Instance;
        public static RuntimeOperator.BitwiseNot BitwiseNot => RuntimeOperator.BitwiseNot.Instance;
        public static RuntimeOperator.LogicalAnd LogicalAnd => RuntimeOperator.LogicalAnd.Instance;
        public static RuntimeOperator.LogicalOr LogicalOr => RuntimeOperator.LogicalOr.Instance;
        public static RuntimeOperator.LogicalXor LogicalXor => RuntimeOperator.LogicalXor.Instance;
        public static RuntimeOperator.LogicalNot LogicalNot => RuntimeOperator.LogicalNot.Instance;
        public static RuntimeOperator.LogicalAndAlso LogicalAndAlso => RuntimeOperator.LogicalAndAlso.Instance;
        public static RuntimeOperator.LogicalOrElse LogicalOrElse => RuntimeOperator.LogicalOrElse.Instance;
        public static RuntimeOperator.Equal Equal => RuntimeOperator.Equal.Instance;
        public static RuntimeOperator.NotEqual NotEqual => RuntimeOperator.NotEqual.Instance;
        public static RuntimeOperator.LessThan LessThan => RuntimeOperator.LessThan.Instance;
        public static RuntimeOperator.LessThanOrEqual LessThanOrEqual => RuntimeOperator.LessThanOrEqual.Instance;
        public static RuntimeOperator.GreaterThan GreaterThan => RuntimeOperator.GreaterThan.Instance;
        public static RuntimeOperator.GreaterThanOrEqual GreaterThanOrEqual => RuntimeOperator.GreaterThanOrEqual.Instance;
        public static RuntimeOperator.True True => RuntimeOperator.True.Instance;
        public static RuntimeOperator.False False => RuntimeOperator.False.Instance;
    }
}