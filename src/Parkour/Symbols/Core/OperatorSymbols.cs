using System.Runtime.CompilerServices;

namespace Parkour.Symbols;

public sealed class OperatorSymbols
{
    /// <summary>
    /// The set of operators that are intrinsic to the runtime.
    /// </summary>
    public ImmutableList<OperatorSymbol> Intrinsic { get; }

    /// <summary>
    /// The default set of operators used by <see cref="StandardBinder"/>
    /// </summary>
    public ImmutableList<OperatorSymbol> Default { get; }

    private OperatorSymbols(SymbolTable symbols)
    {
        this.Intrinsic = CreateIntrinsic(symbols);
        this.Default = CreateDefault(symbols, this.Intrinsic);
    }

    private static readonly ConditionalWeakTable<SymbolTable, OperatorSymbols> _map =
        new ConditionalWeakTable<SymbolTable, OperatorSymbols>();

    public static OperatorSymbols From(SymbolTable symbols)
    {
        if (!_map.TryGetValue(symbols, out var intrinsics))
        {
            intrinsics = _map.GetValue(symbols, s => new OperatorSymbols(symbols));
        }

        return intrinsics;
    }

    private static ImmutableList<OperatorSymbol> CreateIntrinsic(SymbolTable symbols)
    {
        // operator intrinsics
        return [
            BinaryOp(Operator.Add, symbols.Int32),
            BinaryOp(Operator.Add, symbols.UInt32),
            BinaryOp(Operator.Add, symbols.Int64),
            BinaryOp(Operator.Add, symbols.UInt64),
            BinaryOp(Operator.Add, symbols.Single),
            BinaryOp(Operator.Add, symbols.Double),
            BinaryOp(Operator.Add, symbols.Decimal),

            BinaryOp(Operator.Subtract, symbols.Int32),
            BinaryOp(Operator.Subtract, symbols.UInt32),
            BinaryOp(Operator.Subtract, symbols.Int64),
            BinaryOp(Operator.Subtract, symbols.UInt64),
            BinaryOp(Operator.Subtract, symbols.Single),
            BinaryOp(Operator.Subtract, symbols.Double),
            BinaryOp(Operator.Subtract, symbols.Decimal),

            BinaryOp(Operator.Multiply, symbols.Int32),
            BinaryOp(Operator.Multiply, symbols.UInt32),
            BinaryOp(Operator.Multiply, symbols.Int64),
            BinaryOp(Operator.Multiply, symbols.UInt64),
            BinaryOp(Operator.Multiply, symbols.Single),
            BinaryOp(Operator.Multiply, symbols.Double),
            BinaryOp(Operator.Multiply, symbols.Decimal),

            BinaryOp(Operator.Divide, symbols.Int32),
            BinaryOp(Operator.Divide, symbols.UInt32),
            BinaryOp(Operator.Divide, symbols.Int64),
            BinaryOp(Operator.Divide, symbols.UInt64),
            BinaryOp(Operator.Divide, symbols.Single),
            BinaryOp(Operator.Divide, symbols.Double),
            BinaryOp(Operator.Divide, symbols.Decimal),

            BinaryOp(Operator.Remainder, symbols.Int32),
            BinaryOp(Operator.Remainder, symbols.UInt32),
            BinaryOp(Operator.Remainder, symbols.Int64),
            BinaryOp(Operator.Remainder, symbols.UInt64),
            BinaryOp(Operator.Remainder, symbols.Single),
            BinaryOp(Operator.Remainder, symbols.Double),
            BinaryOp(Operator.Remainder, symbols.Decimal),

            UnaryOp(Operator.Negate, symbols.Int32),
            UnaryOp(Operator.Negate, symbols.Int64),
            UnaryOp(Operator.Negate, symbols.Single),
            UnaryOp(Operator.Negate, symbols.Double),
            UnaryOp(Operator.Negate, symbols.Decimal),

            UnaryOp(Operator.Increment, symbols.Int32),
            UnaryOp(Operator.Increment, symbols.UInt32),
            UnaryOp(Operator.Increment, symbols.Int64),
            UnaryOp(Operator.Increment, symbols.UInt64),

            UnaryOp(Operator.Decrement, symbols.Int32),
            UnaryOp(Operator.Decrement, symbols.UInt32),
            UnaryOp(Operator.Decrement, symbols.Int64),
            UnaryOp(Operator.Decrement, symbols.UInt64),

            BinaryOp(Operator.ShiftLeft, symbols.Int32, symbols.Int32, symbols.Int32),
            BinaryOp(Operator.ShiftLeft, symbols.UInt32, symbols.Int32, symbols.UInt32),
            BinaryOp(Operator.ShiftLeft, symbols.Int64, symbols.Int32, symbols.Int64),
            BinaryOp(Operator.ShiftLeft, symbols.UInt64, symbols.Int32, symbols.UInt64),

            BinaryOp(Operator.ShiftRight, symbols.Int32, symbols.Int32, symbols.Int32),
            BinaryOp(Operator.ShiftRight, symbols.UInt32, symbols.Int32, symbols.UInt32),
            BinaryOp(Operator.ShiftRight, symbols.Int64, symbols.Int32, symbols.Int64),
            BinaryOp(Operator.ShiftRight, symbols.UInt64, symbols.Int32, symbols.UInt64),

            BinaryOp(Operator.BitwiseAnd, symbols.Int32),
            BinaryOp(Operator.BitwiseAnd, symbols.UInt32),
            BinaryOp(Operator.BitwiseAnd, symbols.Int64),
            BinaryOp(Operator.BitwiseAnd, symbols.UInt64),

            BinaryOp(Operator.BitwiseOr, symbols.Int32),
            BinaryOp(Operator.BitwiseOr, symbols.UInt32),
            BinaryOp(Operator.BitwiseOr, symbols.Int64),
            BinaryOp(Operator.BitwiseOr, symbols.UInt64),

            BinaryOp(Operator.BitwiseXor, symbols.Int32),
            BinaryOp(Operator.BitwiseXor, symbols.UInt32),
            BinaryOp(Operator.BitwiseXor, symbols.Int64),
            BinaryOp(Operator.BitwiseXor, symbols.UInt64),

            UnaryOp(Operator.BitwiseNot, symbols.Int32),
            UnaryOp(Operator.BitwiseNot, symbols.UInt32),
            UnaryOp(Operator.BitwiseNot, symbols.Int64),
            UnaryOp(Operator.BitwiseNot, symbols.UInt64),

            BinaryOp(Operator.LogicalAnd, symbols.Boolean),
            BinaryOp(Operator.LogicalOr, symbols.Boolean),
            BinaryOp(Operator.LogicalXor, symbols.Boolean),
            UnaryOp(Operator.LogicalNot, symbols.Boolean),

            BinaryOp(Operator.Equal, symbols.Boolean, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.Int32, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.UInt32, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.Int64, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.UInt64, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.Single, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.Double, symbols.Boolean),
            BinaryOp(Operator.Equal, symbols.Decimal, symbols.Boolean),

            BinaryOp(Operator.NotEqual, symbols.Boolean, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.Int32, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.UInt32, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.Int64, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.UInt64, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.Single, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.Double, symbols.Boolean),
            BinaryOp(Operator.NotEqual, symbols.Decimal, symbols.Boolean),

            BinaryOp(Operator.LessThan, symbols.Int32, symbols.Boolean),
            BinaryOp(Operator.LessThan, symbols.Int64, symbols.Boolean),
            BinaryOp(Operator.LessThan, symbols.UInt64, symbols.Boolean),
            BinaryOp(Operator.LessThan, symbols.Single, symbols.Boolean),
            BinaryOp(Operator.LessThan, symbols.Double, symbols.Boolean),
            BinaryOp(Operator.LessThan, symbols.Decimal, symbols.Boolean),

            BinaryOp(Operator.LessThanOrEqual, symbols.Int32, symbols.Boolean),
            BinaryOp(Operator.LessThanOrEqual, symbols.Int64, symbols.Boolean),
            BinaryOp(Operator.LessThanOrEqual, symbols.UInt64, symbols.Boolean),
            BinaryOp(Operator.LessThanOrEqual, symbols.Single, symbols.Boolean),
            BinaryOp(Operator.LessThanOrEqual, symbols.Double, symbols.Boolean),
            BinaryOp(Operator.LessThanOrEqual, symbols.Decimal, symbols.Boolean),

            BinaryOp(Operator.GreaterThan, symbols.Int32, symbols.Boolean),
            BinaryOp(Operator.GreaterThan, symbols.Int64, symbols.Boolean),
            BinaryOp(Operator.GreaterThan, symbols.UInt64, symbols.Boolean),
            BinaryOp(Operator.GreaterThan, symbols.Single, symbols.Boolean),
            BinaryOp(Operator.GreaterThan, symbols.Double, symbols.Boolean),
            BinaryOp(Operator.GreaterThan, symbols.Decimal, symbols.Boolean),

            BinaryOp(Operator.GreaterThanOrEqual, symbols.Int32, symbols.Boolean),
            BinaryOp(Operator.GreaterThanOrEqual, symbols.Int64, symbols.Boolean),
            BinaryOp(Operator.GreaterThanOrEqual, symbols.UInt64, symbols.Boolean),
            BinaryOp(Operator.GreaterThanOrEqual, symbols.Single, symbols.Boolean),
            BinaryOp(Operator.GreaterThanOrEqual, symbols.Double, symbols.Boolean),
            BinaryOp(Operator.GreaterThanOrEqual, symbols.Decimal, symbols.Boolean),
        ];
    }

    /// <summary>
    /// The default set of operators used by <see cref="StandardBinder"/>.
    /// </summary>
    private static ImmutableList<OperatorSymbol> CreateDefault(SymbolTable symbols, ImmutableList<OperatorSymbol> intrinsice)
    {
        var stringConcat = symbols.String.FindMethod("Concat", [symbols.String, symbols.String]);

        return intrinsice.AddRange([
            BinaryOp(Operator.LogicalAndAlso, symbols.Boolean),
            BinaryOp(Operator.LogicalOrElse, symbols.Boolean),
            MethodOp(Operator.Add, stringConcat!)
        ]);
    }

    public static OperatorSymbol MethodOp(Operator op, MethodSymbol checkedMethod, MethodSymbol? uncheckedMethod = null) =>
        new OperatorSymbol(
            op,
            me => checkedMethod.Parameters,
            () => checkedMethod.ReturnType,
            checkedMethod,
            uncheckedMethod);

    public static OperatorSymbol UnaryOp(Operator op, TypeSymbol operandType, TypeSymbol resultType) =>
        new OperatorSymbol(
            op,
            me => ImmutableList.Create(new ParameterSymbol("operand", me, operandType)),
            () => resultType
            );

    public static OperatorSymbol UnaryOp(Operator op, TypeSymbol operand) =>
        UnaryOp(op, operand, operand);

    public static OperatorSymbol BinaryOp(Operator op, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType) =>
        new OperatorSymbol(
            op,
            me => [
                new ParameterSymbol("left", me, leftType),
                new ParameterSymbol("right", me, rightType)
                ],
            () => resultType
            );

    public static OperatorSymbol BinaryOp(Operator op, TypeSymbol argsType, TypeSymbol resultType) =>
        BinaryOp(op, argsType, argsType, resultType);

    public static OperatorSymbol BinaryOp(Operator op, TypeSymbol type) =>
        BinaryOp(op, type, type, type);
}
