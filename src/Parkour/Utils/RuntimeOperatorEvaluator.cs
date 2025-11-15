using System.Numerics; 

namespace Parkour;

using Symbols;

/// <summary>
/// Executes common operators against values.
/// </summary>
public abstract class RuntimeOperatorEvaluator
{
    /// <summary>
    /// Invokes the unary operator with the operand value.
    /// </summary>
    public abstract object? Evaluate(RuntimeOperator op, object? value, bool isChecked = true);

    /// <summary>
    /// Invokes the binary operator with the the left & right argument values.
    /// </summary>
    public abstract object? Evaluate(RuntimeOperator op, object? left, object? right, bool isChecked = true);

    /// <summary>
    /// Converts the value to the type.
    /// </summary>
    public abstract object? Convert(Type type, object? value, bool isChecked = true);
}

/// <summary>
/// Implemention of <see cref="RuntimeOperatorEvaluator"/> using dotnet language rules.
/// </summary>
public class StandardRuntimeOperatorEvaluator : RuntimeOperatorEvaluator
{
    private StandardRuntimeOperatorEvaluator()
    {
    }

    public static readonly StandardRuntimeOperatorEvaluator Instance = 
        new StandardRuntimeOperatorEvaluator();

    public override object? Evaluate(RuntimeOperator op, object? value, bool isChecked = true)
    {
        if (value == null)
            return null;

        var ops = GetOperators(value);

        switch (op)
        {
            case RuntimeOperator.Negate:
                return isChecked ? ops.Negate(value) : ops.NegateUnchecked(value);
            case RuntimeOperator.Increment:
                return isChecked ? ops.Increment(value) : ops.IncrementUnchecked(value);
            case RuntimeOperator.Decrement:
                return isChecked ? ops.Decrement(value) : ops.DecrementUnchecked(value);
            case RuntimeOperator.BitwiseNot:
                return ops.BitwiseNot(value);
            case RuntimeOperator.LogicalNot:
                return ops.LogicalNot(value);
            case RuntimeOperator.UnaryPlus:
                return value;
            case RuntimeOperator.True:
                return ops.True(value);
            case RuntimeOperator.False:
                return ops.False(value);
            default:
                throw new InvalidOperationException($"Unhandled unary operator kind '{op.GetType().Name}'");
        }
    }

    public override object? Evaluate(RuntimeOperator op, object? left, object? right, bool isChecked = true)
    {
        if (left == null || right == null) return null;

        if (op is RuntimeOperator.ShiftLeft)
        {
            var ops = GetOperators(left);
            return isChecked ? ops.ShiftLeft(left, right) : ops.ShiftLeftUnchecked(left, right);
        }
        else if (op is RuntimeOperator.ShiftRight)
        {
            var ops = GetOperators(left);
            return isChecked ? ops.ShiftRight(left, right) : ops.ShiftRightUnchecked(left, right);
        }
        else
        {
            var ops = GetOperators(left, right);

            switch (op)
            {
                case RuntimeOperator.Add:
                    return isChecked ? ops.Add(left, right) : ops.AddUnchecked(left, right);
                case RuntimeOperator.Subtract:
                    return isChecked ? ops.Subtract(left, right) : ops.SubtractUnchecked(left, right);
                case RuntimeOperator.Multiply:
                    return isChecked ? ops.Multiply(left, right) : ops.MultiplyUnchecked(left, right);
                case RuntimeOperator.Divide:
                    return isChecked ? ops.Divide(left, right) : ops.DivideUnchecked(left, right);
                case RuntimeOperator.Remainder:
                    return isChecked ? ops.Remainder(left, right) : ops.RemainderUnchecked(left, right);
                case RuntimeOperator.BitwiseAnd:
                    return ops.BitwiseAnd(left, right);
                case RuntimeOperator.BitwiseOr:
                    return ops.BitwiseOr(left, right);
                case RuntimeOperator.BitwiseXor:
                    return ops.BitwiseXor(left, right);
                case RuntimeOperator.Equal:
                    return ops.Equal(left, right);
                case RuntimeOperator.NotEqual:
                    return ops.NotEqual(left, right);
                case RuntimeOperator.LessThan:
                    return ops.LessThan(left, right);
                case RuntimeOperator.LessThanOrEqual:
                    return ops.LessThanOrEqual(left, right);
                case RuntimeOperator.GreaterThan:
                    return ops.GreaterThan(left, right);
                case RuntimeOperator.GreaterThanOrEqual:
                    return ops.GreaterThanOrEqual(left, right);
                case RuntimeOperator.LogicalAnd:
                case RuntimeOperator.LogicalAndAlso:
                    return ops.LogicalAnd(left, right);
                case RuntimeOperator.LogicalOr:
                case RuntimeOperator.LogicalOrElse:
                    return ops.LogicalOr(left, right);
                default:
                    throw new InvalidOperationException($"Unhandled binary operator kind '{op.GetType().Name}'.");
            }
        }
    }

    public override object? Convert(Type type, object? value, bool isChecked = true)
    {
        if (value == null && !type.IsValueType)
            return null;

        if (value == null)
            return GetOperators(type).DefaultValue;

        if (value.GetType().IsAssignableTo(type))
            return value;

        // try change type.. may throw
        return System.Convert.ChangeType(value, type);
    }

    interface IOperators
    {
        Type Type { get; }
        object? DefaultValue { get; }
        bool IsNumber { get; }
        bool IsInteger { get; }
        bool IsSigned { get; }
        int MaxNumericBitSize { get; }
        int MaxIntegerBitSize { get; }

        object Add(object left, object right);
        object AddUnchecked(object left, object right);
        object Subtract(object left, object right);
        object SubtractUnchecked(object left, object right);
        object Multiply(object left, object right);
        object MultiplyUnchecked(object left, object right);
        object Divide(object left, object right);
        object DivideUnchecked(object left, object right);
        object Remainder(object left, object right);
        object RemainderUnchecked(object left, object right);
        object Negate(object value);
        object NegateUnchecked(object value);
        object Increment(object value);
        object IncrementUnchecked(object value);
        object Decrement(object value);
        object DecrementUnchecked(object value);
        object BitwiseAnd(object left, object right);
        object BitwiseOr(object left, object right);
        object BitwiseXor(object left, object right);
        object BitwiseNot(object value);
        object ShiftLeft(object left, object right);
        object ShiftLeftUnchecked(object left, object right);
        object ShiftRight(object left, object right);
        object ShiftRightUnchecked(object left, object right);
        object Equal(object left, object right);
        object NotEqual(object left, object right);
        object LessThan(object left, object right);
        object LessThanOrEqual(object left, object right);
        object GreaterThan(object left, object right);
        object GreaterThanOrEqual(object left, object right);
        object LogicalAnd(object left, object right);
        object LogicalOr(object left, object right);
        object LogicalNot(object value);
        object True(object value);
        object False(object value);
    }

    private static IOperators GetOperators(Type type)
    {
        if (!_typeToOperatorsMap.TryGetValue(type, out var operators))
        {
            var tmp = CreateOperators(type);
            operators = ImmutableInterlocked.GetOrAdd(ref _typeToOperatorsMap, type, tmp);
        }

        return operators;
    }

    private static ImmutableDictionary<Type, IOperators> _typeToOperatorsMap =
        new Dictionary<Type, IOperators>() {
            { typeof(Boolean), new BoolOperators() },
            { typeof(Byte), new IntegerOperators<Byte>() },
            { typeof(SByte), new IntegerOperators<SByte>() },
            { typeof(Int16), new IntegerOperators<Int16>() },
            { typeof(UInt16), new IntegerOperators<UInt16>() },
            { typeof(Int32), new IntegerOperators<Int32>() },
            { typeof(UInt32), new IntegerOperators<UInt32>() },
            { typeof(Int64), new IntegerOperators<Int64>() },
            { typeof(UInt64), new IntegerOperators<UInt64>() },
            { typeof(Single), new FloatingPointOperators<Single>() },
            { typeof(Double), new FloatingPointOperators<Double>() },
            { typeof(Decimal), new NumberOperators<Decimal>() },
            { typeof(String), new StringOperators() }
        }
        .ToImmutableDictionary();

    private static IOperators CreateOperators(Type type)
    {
        var interfaces = type.GetInterfaces();

        if (HasInterface(typeof(IBinaryInteger<>)))
        {
            return (IOperators)Activator.CreateInstance(typeof(IntegerOperators<>).MakeGenericType(type))!;
        }
        else if (HasInterface(typeof(IFloatingPoint<>)))
        {
            return (IOperators)Activator.CreateInstance(typeof(FloatingPointOperators<>).MakeGenericType(type))!;
        }
        else if (HasInterface(typeof(INumber<>)))
        {
            return (IOperators)Activator.CreateInstance(typeof(NumberOperators<>).MakeGenericType(type))!;
        }
        else if (HasInterface(typeof(IComparable<>)))
        {
            return (IOperators)Activator.CreateInstance(typeof(ComparableOperators<>).MakeGenericType(type))!;
        }
        else if (HasInterface(typeof(IEquatable<>)))
        {
            return (IOperators)Activator.CreateInstance(typeof(EquatableOperators<>).MakeGenericType(type))!;
        }
        else
        {
            return (IOperators)Activator.CreateInstance(typeof(TypedOperators<>).MakeGenericType(type))!;
        }

        bool HasInterface(Type iface)
        {
            return interfaces.Any(f =>
                f == iface
                || iface.IsTypeDefinition && f.IsConstructedGenericType && f.GetGenericTypeDefinition() == iface);
        }
    }

    /// <summary>
    /// Gets the operators appropriate for the value.
    /// </summary>
    private static IOperators GetOperators(object value) =>
        GetOperators(value.GetType());

    /// <summary>
    /// Gets the <see cref="IOperators"/> for the best common type between the values.
    /// </summary>
    private static IOperators GetOperators(object left, object right)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        var leftOT = GetOperators(leftType);
        var rightOT = GetOperators(rightType);

        if (leftType == rightType)
            return leftOT;

        if (leftOT.IsInteger && rightOT.IsInteger)
        {
            if (leftOT.IsSigned == rightOT.IsSigned)
            {
                return leftOT.MaxIntegerBitSize >= rightOT.MaxIntegerBitSize
                    ? leftOT
                    : rightOT;
            }
            else if (leftOT.IsSigned && leftOT.MaxIntegerBitSize > rightOT.MaxIntegerBitSize)
            {
                return leftOT;
            }
            else if (rightOT.IsSigned && rightOT.MaxIntegerBitSize > leftOT.MaxIntegerBitSize)
            {
                return rightOT;
            }
        }
        else if (!leftOT.IsInteger && !rightOT.IsInteger)
        {
            return leftOT.MaxNumericBitSize >= rightOT.MaxNumericBitSize
                ? leftOT
                : rightOT;
        }
        else if (!leftOT.IsInteger && leftOT.MaxIntegerBitSize >= rightOT.MaxIntegerBitSize)
        {
            return leftOT;
        }
        else if (!rightOT.IsInteger && rightOT.MaxIntegerBitSize >= leftOT.MaxIntegerBitSize)
        {
            return rightOT;
        }

        throw new InvalidOperationException($"No compatible operators for types: '{leftType.FullName}' and '{rightType.FullName}'");
    }

    private class TypedOperators<T>
        : IOperators
    {
        private static Exception GetException(string op) =>
            new InvalidOperationException($"The {op} operator is not supported for type '{typeof(T).FullName}'.");

        public virtual T DefaultValue => default!;
        public virtual bool IsNumber => false;
        public virtual bool IsInteger => false;
        public virtual bool IsSigned => false;

        public virtual int MaxNumericBitSize =>
            System.Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Byte => 8,
                TypeCode.SByte => 8,
                TypeCode.UInt16 => 16,
                TypeCode.Int16 => 16,
                TypeCode.UInt32 => 32,
                TypeCode.Int32 => 32,
                TypeCode.UInt64 => 64,
                TypeCode.Int64 => 64,
                TypeCode.Single => 32,
                TypeCode.Double => 64,
                TypeCode.Decimal => 128,
                _ => 0
            };

        public virtual int MaxIntegerBitSize =>
            System.Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Byte => 8,
                TypeCode.SByte => 8,
                TypeCode.UInt16 => 16,
                TypeCode.Int16 => 16,
                TypeCode.UInt32 => 32,
                TypeCode.Int32 => 32,
                TypeCode.UInt64 => 64,
                TypeCode.Int64 => 64,
                TypeCode.Single => 24,
                TypeCode.Double => 53,
                TypeCode.Decimal => 96,
                _ => 0
            };

        public virtual T Add(T left, T right) =>
            throw GetException("add");

        public virtual T AddUnchecked(T left, T right) =>
            throw GetException("add");

        public virtual T Subtract(T left, T right) =>
            throw GetException("subtract");

        public virtual T SubtractUnchecked(T left, T right) =>
            throw GetException("subtract");

        public virtual T Multiply(T left, T right) =>
            throw GetException("multiply");

        public virtual T MultiplyUnchecked(T left, T right) =>
            throw GetException("multiply");

        public virtual T Divide(T left, T right) =>
            throw GetException("divide");

        public virtual T DivideUnchecked(T left, T right) =>
            throw GetException("divide");

        public virtual T Remainder(T left, T right) =>
            throw GetException("remainder");

        public virtual T RemainderUnchecked(T left, T right) =>
            throw GetException("remainder");

        public virtual T Negate(T value) =>
            throw GetException("negate");

        public virtual T NegateUnchecked(T value) =>
            throw GetException("negate");

        public virtual T Increment(T value) =>
            throw GetException("increment");

        public virtual T IncrementUnchecked(T value) =>
            throw GetException("increment");

        public virtual T Decrement(T value) =>
            throw GetException("decrement");

        public virtual T DecrementUnchecked(T value) =>
            throw GetException("decrement");

        public virtual T BitwiseAnd(T left, T right) =>
            throw GetException("bitwise and");

        public virtual T BitwiseOr(T left, T right) =>
            throw GetException("bitwise or");

        public virtual T BitwiseXor(T left, T right) =>
            throw GetException("bitwise xor");

        public virtual T BitwiseNot(T value) =>
            throw GetException("bitwise not");

        public virtual T ShiftLeft(T left, int right) =>
            throw GetException("shift left");

        public virtual T ShiftLeftUnchecked(T left, int right) =>
            throw GetException("shift left");

        public virtual T ShiftRight(T left, int right) =>
            throw GetException("shift right");

        public virtual T ShiftRightUnchecked(T left, int right) =>
            throw GetException("shift right");

        public virtual bool Equal(T left, T right) =>
            throw GetException("equal");

        public virtual bool NotEqual(T left, T right) =>
            throw GetException("not equal");

        public virtual bool LessThan(T left, T right) =>
            throw GetException("less than");

        public virtual bool LessThanOrEqual(T left, T right) =>
            throw GetException("less than or equal");

        public virtual bool GreaterThan(T left, T right) =>
            throw GetException("greather than");

        public virtual bool GreaterThanOrEqual(T left, T right) =>
            throw GetException("greater than or equal");

        public virtual bool LogicalAnd(T left, T right) =>
            throw GetException("logical and");

        public virtual bool LogicalOr(T left, T right) =>
            throw GetException("logical or");

        public virtual bool LogicalNot(T value) =>
            throw GetException("logical not");

        public virtual bool True(T value) =>
            throw GetException("true");

        public virtual bool False(T value) =>
            throw GetException("false");

        #region IOperators

        Type IOperators.Type => typeof(T);
        object? IOperators.DefaultValue => this.DefaultValue!;

        private static TValue GetValue<TValue>(object value)
        {
            if (value.GetType() == typeof(TValue))
                return (TValue)value;
            return (TValue)System.Convert.ChangeType(value, typeof(TValue));
        }

        private static T GetValue(object value) =>
            GetValue<T>(value);

        object IOperators.Add(object left, object right) =>
            this.Add(GetValue(left), GetValue(right))!;

        object IOperators.AddUnchecked(object left, object right) =>
            this.AddUnchecked(GetValue(left), GetValue(right))!;

        object IOperators.Subtract(object left, object right) =>
            this.Subtract(GetValue(left), GetValue(right))!;

        object IOperators.SubtractUnchecked(object left, object right) =>
            this.SubtractUnchecked(GetValue(left), GetValue(right))!;

        object IOperators.Multiply(object left, object right) =>
            this.Multiply(GetValue(left), GetValue(right))!;

        object IOperators.MultiplyUnchecked(object left, object right) =>
            this.MultiplyUnchecked(GetValue(left), GetValue(right))!;

        object IOperators.Divide(object left, object right) =>
            this.Divide(GetValue(left), GetValue(right))!;

        object IOperators.DivideUnchecked(object left, object right) =>
            this.DivideUnchecked(GetValue(left), GetValue(right))!;

        object IOperators.Remainder(object left, object right) =>
            this.Remainder(GetValue(left), GetValue(right))!;

        object IOperators.RemainderUnchecked(object left, object right) =>
            this.RemainderUnchecked(GetValue(left), GetValue(right))!;

        object IOperators.Negate(object operand) =>
            this.Negate(GetValue(operand))!;

        object IOperators.NegateUnchecked(object operand) =>
            this.NegateUnchecked(GetValue(operand))!;

        object IOperators.Increment(object operand) =>
            this.Increment(GetValue(operand))!;

        object IOperators.IncrementUnchecked(object operand) =>
            this.IncrementUnchecked(GetValue(operand))!;

        object IOperators.Decrement(object operand) =>
            this.Decrement(GetValue(operand))!;

        object IOperators.DecrementUnchecked(object operand) =>
            this.DecrementUnchecked(GetValue(operand))!;

        object IOperators.BitwiseAnd(object left, object right) =>
            this.BitwiseAnd(GetValue(left), GetValue(right))!;

        object IOperators.BitwiseOr(object left, object right) =>
            this.BitwiseOr(GetValue(left), GetValue(right))!;

        object IOperators.BitwiseXor(object left, object right) =>
            this.BitwiseXor(GetValue(left), GetValue(right))!;

        object IOperators.BitwiseNot(object value) =>
            this.BitwiseNot(GetValue(value))!;

        object IOperators.ShiftLeft(object left, object right) =>
            this.ShiftLeft(GetValue(left), GetValue<int>(right))!;

        object IOperators.ShiftLeftUnchecked(object left, object right) =>
            this.ShiftLeftUnchecked(GetValue(left), GetValue<int>(right))!;

        object IOperators.ShiftRight(object left, object right) =>
            this.ShiftRight(GetValue(left), GetValue<int>(right))!;

        object IOperators.ShiftRightUnchecked(object left, object right) =>
            this.ShiftRightUnchecked(GetValue(left), GetValue<int>(right))!;

        object IOperators.Equal(object left, object right) =>
            this.Equal(GetValue(left), GetValue(right))!;

        object IOperators.NotEqual(object left, object right) =>
            this.NotEqual(GetValue(left), GetValue(right))!;

        object IOperators.LessThan(object left, object right) =>
            this.LessThan(GetValue(left), GetValue(right))!;

        object IOperators.LessThanOrEqual(object left, object right) =>
            this.LessThanOrEqual(GetValue(left), GetValue(right))!;

        object IOperators.GreaterThan(object left, object right) =>
            this.GreaterThan(GetValue(left), GetValue(right))!;

        object IOperators.GreaterThanOrEqual(object left, object right) =>
            this.GreaterThanOrEqual(GetValue(left), GetValue(right))!;

        object IOperators.LogicalAnd(object left, object right) =>
            this.LogicalAnd(GetValue(left), GetValue(right))!;

        object IOperators.LogicalOr(object left, object right) =>
            this.LogicalOr(GetValue(left), GetValue(right))!;

        object IOperators.LogicalNot(object value) =>
            this.LogicalNot(GetValue(value))!;

        object IOperators.True(object value) =>
            this.True(GetValue(value));

        object IOperators.False(object value) =>
            this.False(GetValue(value));

        #endregion
    }

    private class EquatableOperators<T>
        : TypedOperators<T>
        where T : IEquatable<T>
    {
        public override bool Equal(T left, T right) =>
            left.Equals(right);

        public override bool NotEqual(T left, T right) =>
            !left.Equals(right);
    }

    private class ComparableOperators<T>
        : TypedOperators<T>
        where T : IComparable<T>
    {
        public override bool Equal(T left, T right) =>
            left.CompareTo(right) == 0;

        public override bool NotEqual(T left, T right) =>
            left.CompareTo(right) != 0;

        public override bool LessThan(T left, T right) =>
            left.CompareTo(right) < 0;

        public override bool LessThanOrEqual(T left, T right) =>
            left.CompareTo(right) <= 0;

        public override bool GreaterThan(T left, T right) =>
            left.CompareTo(right) > 0;

        public override bool GreaterThanOrEqual(T left, T right) =>
            left.CompareTo(right) >= 0;
    }

    private class BoolOperators
        : TypedOperators<bool>
    {
        public override int MaxIntegerBitSize => 1;
        public override int MaxNumericBitSize => 1;

        public override bool BitwiseAnd(bool left, bool right) =>
            left & right;

        public override bool BitwiseOr(bool left, bool right) =>
            left | right;

        public override bool BitwiseXor(bool left, bool right) =>
            left ^ right;

        public override bool BitwiseNot(bool value) =>
            !value;

        public override bool LogicalAnd(bool left, bool right) =>
            left && right;

        public override bool LogicalOr(bool left, bool right) =>
            left || right;

        public override bool LogicalNot(bool value) =>
            !value;

        public override bool True(bool value) =>
            value;

        public override bool False(bool value) =>
            !value;
    }

    private class NumberOperators<T>
        : TypedOperators<T>
        where T : INumber<T>
    {
        public override bool IsNumber => true;
        public override bool IsSigned => true;

        public override T Add(T left, T right) =>
            left + right;

        public override T AddUnchecked(T left, T right) =>
            unchecked(left + right);

        public override T Subtract(T left, T right) =>
            left - right;

        public override T SubtractUnchecked(T left, T right) =>
            unchecked(left - right);

        public override T Multiply(T left, T right) =>
            left * right;

        public override T MultiplyUnchecked(T left, T right) =>
            unchecked(left * right);

        public override T Divide(T left, T right) =>
            left / right;

        public override T DivideUnchecked(T left, T right) =>
            unchecked(left / right);

        public override T Remainder(T left, T right) =>
            left % right;

        public override T RemainderUnchecked(T left, T right) =>
            unchecked(left % right);

        public override T Negate(T value) =>
            -value;

        public override T Increment(T value) =>
            value + T.One;

        public override T IncrementUnchecked(T value) =>
            unchecked(value + T.One);

        public override T Decrement(T value) =>
            value - T.One;

        public override T DecrementUnchecked(T value) =>
            unchecked(value - T.One);

        public override bool Equal(T left, T right) =>
            left == right;

        public override bool NotEqual(T left, T right) =>
            left != right;

        public override bool LessThan(T left, T right) =>
            left < right;

        public override bool LessThanOrEqual(T left, T right) =>
            left <= right;

        public override bool GreaterThan(T left, T right) =>
            left > right;

        public override bool GreaterThanOrEqual(T left, T right) =>
            left >= right;

        public override bool LogicalAnd(T left, T right) =>
            left != T.Zero && right != T.Zero;

        public override bool LogicalOr(T left, T right) =>
            left != T.Zero || right != T.Zero;

        public override bool LogicalNot(T value) =>
            value == T.Zero;

        public override bool True(T value) =>
            value != T.Zero;

        public override bool False(T value) =>
            value == T.Zero;
    }

    private class FloatingPointOperators<T>
        : NumberOperators<T>
        where T : IFloatingPoint<T>
    {
    }

    private class IntegerOperators<T>
        : NumberOperators<T>
        where T : IBinaryInteger<T>
    {
        public override bool IsNumber => true;
        public override bool IsInteger => true;
        public override bool IsSigned => T.IsNegative(-T.One);

        public override T BitwiseAnd(T left, T right) =>
            left & right;

        public override T BitwiseOr(T left, T right) =>
            left | right;

        public override T BitwiseXor(T left, T right) =>
            left ^ right;

        public override T BitwiseNot(T value) =>
            ~value;

        public override T ShiftLeft(T left, int right) =>
            left << right;

        public override T ShiftLeftUnchecked(T left, int right) =>
            unchecked(left << right);

        public override T ShiftRight(T left, int right) =>
            left >> right;

        public override T ShiftRightUnchecked(T left, int right) =>
            unchecked(left >> right);
    }

    private class StringOperators : ComparableOperators<string>
    {
        public override string Add(string left, string right)
        {
            return left + right;
        }

        public override string AddUnchecked(string left, string right)
        {
            return left + right;
        }
    }
}