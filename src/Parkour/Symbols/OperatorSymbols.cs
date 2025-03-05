using System.Runtime.CompilerServices;

namespace Parkour.Symbols;
using Symbols;

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
            BinaryOp(OperatorKind.Add, symbols.Int32),
            BinaryOp(OperatorKind.Add, symbols.UInt32),
            BinaryOp(OperatorKind.Add, symbols.Int64),
            BinaryOp(OperatorKind.Add, symbols.UInt64),
            BinaryOp(OperatorKind.Add, symbols.Single),
            BinaryOp(OperatorKind.Add, symbols.Double),
            BinaryOp(OperatorKind.Add, symbols.Decimal),

            BinaryOp(OperatorKind.Subtract, symbols.Int32),
            BinaryOp(OperatorKind.Subtract, symbols.UInt32),
            BinaryOp(OperatorKind.Subtract, symbols.Int64),
            BinaryOp(OperatorKind.Subtract, symbols.UInt64),
            BinaryOp(OperatorKind.Subtract, symbols.Single),
            BinaryOp(OperatorKind.Subtract, symbols.Double),
            BinaryOp(OperatorKind.Subtract, symbols.Decimal),

            BinaryOp(OperatorKind.Multiply, symbols.Int32),
            BinaryOp(OperatorKind.Multiply, symbols.UInt32),
            BinaryOp(OperatorKind.Multiply, symbols.Int64),
            BinaryOp(OperatorKind.Multiply, symbols.UInt64),
            BinaryOp(OperatorKind.Multiply, symbols.Single),
            BinaryOp(OperatorKind.Multiply, symbols.Double),
            BinaryOp(OperatorKind.Multiply, symbols.Decimal),

            BinaryOp(OperatorKind.Divide, symbols.Int32),
            BinaryOp(OperatorKind.Divide, symbols.UInt32),
            BinaryOp(OperatorKind.Divide, symbols.Int64),
            BinaryOp(OperatorKind.Divide, symbols.UInt64),
            BinaryOp(OperatorKind.Divide, symbols.Single),
            BinaryOp(OperatorKind.Divide, symbols.Double),
            BinaryOp(OperatorKind.Divide, symbols.Decimal),

            BinaryOp(OperatorKind.Remainder, symbols.Int32),
            BinaryOp(OperatorKind.Remainder, symbols.UInt32),
            BinaryOp(OperatorKind.Remainder, symbols.Int64),
            BinaryOp(OperatorKind.Remainder, symbols.UInt64),
            BinaryOp(OperatorKind.Remainder, symbols.Single),
            BinaryOp(OperatorKind.Remainder, symbols.Double),
            BinaryOp(OperatorKind.Remainder, symbols.Decimal),

            UnaryOp(OperatorKind.Negate, symbols.Int32),
            UnaryOp(OperatorKind.Negate, symbols.Int64),
            UnaryOp(OperatorKind.Negate, symbols.Single),
            UnaryOp(OperatorKind.Negate, symbols.Double),
            UnaryOp(OperatorKind.Negate, symbols.Decimal),

            UnaryOp(OperatorKind.Increment, symbols.Int32),
            UnaryOp(OperatorKind.Increment, symbols.UInt32),
            UnaryOp(OperatorKind.Increment, symbols.Int64),
            UnaryOp(OperatorKind.Increment, symbols.UInt64),

            UnaryOp(OperatorKind.Decrement, symbols.Int32),
            UnaryOp(OperatorKind.Decrement, symbols.UInt32),
            UnaryOp(OperatorKind.Decrement, symbols.Int64),
            UnaryOp(OperatorKind.Decrement, symbols.UInt64),

            BinaryOp(OperatorKind.ShiftLeft, symbols.Int32, symbols.Int32, symbols.Int32),
            BinaryOp(OperatorKind.ShiftLeft, symbols.UInt32, symbols.Int32, symbols.UInt32),
            BinaryOp(OperatorKind.ShiftLeft, symbols.Int64, symbols.Int32, symbols.Int64),
            BinaryOp(OperatorKind.ShiftLeft, symbols.UInt64, symbols.Int32, symbols.UInt64),

            BinaryOp(OperatorKind.ShiftRight, symbols.Int32, symbols.Int32, symbols.Int32),
            BinaryOp(OperatorKind.ShiftRight, symbols.UInt32, symbols.Int32, symbols.UInt32),
            BinaryOp(OperatorKind.ShiftRight, symbols.Int64, symbols.Int32, symbols.Int64),
            BinaryOp(OperatorKind.ShiftRight, symbols.UInt64, symbols.Int32, symbols.UInt64),

            BinaryOp(OperatorKind.BitwiseAnd, symbols.Int32),
            BinaryOp(OperatorKind.BitwiseAnd, symbols.UInt32),
            BinaryOp(OperatorKind.BitwiseAnd, symbols.Int64),
            BinaryOp(OperatorKind.BitwiseAnd, symbols.UInt64),

            BinaryOp(OperatorKind.BitwiseOr, symbols.Int32),
            BinaryOp(OperatorKind.BitwiseOr, symbols.UInt32),
            BinaryOp(OperatorKind.BitwiseOr, symbols.Int64),
            BinaryOp(OperatorKind.BitwiseOr, symbols.UInt64),

            BinaryOp(OperatorKind.BitwiseXor, symbols.Int32),
            BinaryOp(OperatorKind.BitwiseXor, symbols.UInt32),
            BinaryOp(OperatorKind.BitwiseXor, symbols.Int64),
            BinaryOp(OperatorKind.BitwiseXor, symbols.UInt64),

            UnaryOp(OperatorKind.BitwiseNot, symbols.Int32),
            UnaryOp(OperatorKind.BitwiseNot, symbols.UInt32),
            UnaryOp(OperatorKind.BitwiseNot, symbols.Int64),
            UnaryOp(OperatorKind.BitwiseNot, symbols.UInt64),

            BinaryOp(OperatorKind.LogicalAnd, symbols.Boolean),
            BinaryOp(OperatorKind.LogicalOr, symbols.Boolean),
            BinaryOp(OperatorKind.LogicalXor, symbols.Boolean),
            UnaryOp(OperatorKind.LogicalNot, symbols.Boolean),

            BinaryOp(OperatorKind.Equal, symbols.Boolean, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.Int32, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.UInt32, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.Int64, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.UInt64, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.Single, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.Double, symbols.Boolean),
            BinaryOp(OperatorKind.Equal, symbols.Decimal, symbols.Boolean),

            BinaryOp(OperatorKind.NotEqual, symbols.Boolean, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.Int32, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.UInt32, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.Int64, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.UInt64, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.Single, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.Double, symbols.Boolean),
            BinaryOp(OperatorKind.NotEqual, symbols.Decimal, symbols.Boolean),

            BinaryOp(OperatorKind.LessThan, symbols.Int32, symbols.Boolean),
            BinaryOp(OperatorKind.LessThan, symbols.Int64, symbols.Boolean),
            BinaryOp(OperatorKind.LessThan, symbols.UInt64, symbols.Boolean),
            BinaryOp(OperatorKind.LessThan, symbols.Single, symbols.Boolean),
            BinaryOp(OperatorKind.LessThan, symbols.Double, symbols.Boolean),
            BinaryOp(OperatorKind.LessThan, symbols.Decimal, symbols.Boolean),

            BinaryOp(OperatorKind.LessThanOrEqual, symbols.Int32, symbols.Boolean),
            BinaryOp(OperatorKind.LessThanOrEqual, symbols.Int64, symbols.Boolean),
            BinaryOp(OperatorKind.LessThanOrEqual, symbols.UInt64, symbols.Boolean),
            BinaryOp(OperatorKind.LessThanOrEqual, symbols.Single, symbols.Boolean),
            BinaryOp(OperatorKind.LessThanOrEqual, symbols.Double, symbols.Boolean),
            BinaryOp(OperatorKind.LessThanOrEqual, symbols.Decimal, symbols.Boolean),

            BinaryOp(OperatorKind.GreaterThan, symbols.Int32, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThan, symbols.Int64, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThan, symbols.UInt64, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThan, symbols.Single, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThan, symbols.Double, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThan, symbols.Decimal, symbols.Boolean),

            BinaryOp(OperatorKind.GreaterThanOrEqual, symbols.Int32, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThanOrEqual, symbols.Int64, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThanOrEqual, symbols.UInt64, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThanOrEqual, symbols.Single, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThanOrEqual, symbols.Double, symbols.Boolean),
            BinaryOp(OperatorKind.GreaterThanOrEqual, symbols.Decimal, symbols.Boolean),
        ];
    }

    /// <summary>
    /// The default set of operators used by <see cref="StandardBinder"/>.
    /// </summary>
    private static ImmutableList<OperatorSymbol> CreateDefault(SymbolTable symbols, ImmutableList<OperatorSymbol> intrinsice)
    {
        var stringConcat = symbols.String.FindMethod("Concat", [symbols.String, symbols.String]);

        return intrinsice.AddRange([
            BinaryOp(OperatorKind.LogicalAndAlso, symbols.Boolean),
            BinaryOp(OperatorKind.LogicalOrElse, symbols.Boolean),
            MethodOp(OperatorKind.Add, stringConcat!)
        ]);
    }

    public static OperatorSymbol MethodOp(string kind, MethodSymbol checkedMethod, MethodSymbol? uncheckedMethod = null) =>
        new OperatorSymbol(
            kind,
            me => checkedMethod.Parameters,
            () => checkedMethod.ReturnType,
            checkedMethod,
            uncheckedMethod);

    public static OperatorSymbol UnaryOp(string kind, TypeSymbol operandType, TypeSymbol resultType) =>
        new OperatorSymbol(
            kind,
            me => ImmutableList.Create(new ParameterSymbol("operand", me, operandType)),
            () => resultType
            );

    public static OperatorSymbol UnaryOp(string kind, TypeSymbol operand) =>
        UnaryOp(kind, operand, operand);

    public static OperatorSymbol BinaryOp(string kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType) =>
        new OperatorSymbol(
            kind,
            me => [
                new ParameterSymbol("left", me, leftType),
                new ParameterSymbol("right", me, rightType)
                ],
            () => resultType
            );

    public static OperatorSymbol BinaryOp(string kind, TypeSymbol argsType, TypeSymbol resultType) =>
        BinaryOp(kind, argsType, argsType, resultType);

    public static OperatorSymbol BinaryOp(string kind, TypeSymbol type) =>
        BinaryOp(kind, type, type, type);
}
