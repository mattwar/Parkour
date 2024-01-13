namespace Parkour.Binding;
using Symbols;
using System.Runtime.CompilerServices;

/// <summary>
/// Operator functions that are unbound.
/// </summary>
public static class OperatorKinds
{
    public const string Add = nameof(Add);
    public const string Subtract = nameof(Subtract);
    public const string Multiply = nameof(Multiply);
    public const string Divide = nameof(Divide);
    public const string Remainder = nameof(Remainder);
    public const string Negate = nameof(Negate);
    public const string BitwiseAnd = nameof(BitwiseAnd);
    public const string BitwiseOr = nameof(BitwiseOr);
    public const string BitwiseXor = nameof(BitwiseXor);
    public const string BitwiseNot = nameof(BitwiseNot);
    public const string ShiftLeft = nameof(ShiftLeft);
    public const string ShiftRight = nameof(ShiftRight);
    public const string Equal = nameof(Equal);
    public const string NotEqual = nameof(NotEqual);
    public const string LessThan = nameof(LessThan);
    public const string LessThanOrEqual = nameof(LessThanOrEqual);
    public const string GreaterThan = nameof(GreaterThan);
    public const string GreaterThanOrEqual = nameof(GreaterThanOrEqual);
    public const string LogicalNot = nameof(LogicalNot);
    public const string LogicalAnd = nameof(LogicalAnd);
    public const string LogicalAndAlso = nameof(LogicalAndAlso);
    public const string LogicalOr = nameof(LogicalOr);
    public const string LogicalOrElse = nameof(LogicalOrElse);
}

public sealed class Operators
{
    private static readonly ConditionalWeakTable<SymbolCache, Operators> _map =
        new ConditionalWeakTable<SymbolCache, Operators>();

    public static Operators From(SymbolCache symbols)
    {
        if (!_map.TryGetValue(symbols, out var intrinsics))
        {
            intrinsics = _map.GetValue(symbols, s => new Operators(s));
        }

        return intrinsics;
    }

    private readonly Dictionary<string, ImmutableList<FunctionSymbol>> _kindToOperators;

    public ImmutableList<FunctionSymbol> GetOperators(string kind) =>
        _kindToOperators.TryGetValue(kind, out var intrinsics)
            ? intrinsics
            : ImmutableList<FunctionSymbol>.Empty;

    public static FunctionSymbol UnaryOp(string name, string kind, TypeSymbol operandType, TypeSymbol resultType) =>
        new OperatorSymbol(
            name, 
            kind, 
            me => ImmutableList.Create(new ParameterSymbol("operand", me, operandType, null)),
            () => resultType
            );

    public static FunctionSymbol UnaryOp(string name, string kind, TypeSymbol operand) =>
        UnaryOp(name, kind, operand, operand);

    public static FunctionSymbol BinaryOp(string name, string kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType) =>
        new OperatorSymbol(
            name,
            kind,
            me => ImmutableList.Create(
                new ParameterSymbol("left", me, leftType, null),
                new ParameterSymbol("right", me, rightType, null)
                ),
            () => resultType
            );

    public static FunctionSymbol BinaryOp(string name, string kind, TypeSymbol argsType, TypeSymbol resultType) =>
        BinaryOp(name, kind, argsType, argsType, resultType);

    public static FunctionSymbol BinaryOp(string name, string kind, TypeSymbol type) =>
        BinaryOp(name, kind, type, type, type);

    // Int32 operators
    public FunctionSymbol AddInt32 { get; }
    public FunctionSymbol SubtractInt32 { get; }
    public FunctionSymbol MultiplyInt32 { get; }
    public FunctionSymbol DivideInt32 { get; }
    public FunctionSymbol RemainderInt32 { get; }
    public FunctionSymbol NegateInt32 { get; }
    public FunctionSymbol BitwiseAndInt32 { get; }
    public FunctionSymbol BitwiseOrInt32 { get; }
    public FunctionSymbol BitwiseXorInt32 { get; }
    public FunctionSymbol BitwiseNotInt32 { get; }
    public FunctionSymbol ShiftLeftInt32 { get; }
    public FunctionSymbol ShiftRightInt32 { get; }
    public FunctionSymbol EqualInt32 { get; }
    public FunctionSymbol NotEqualInt32 { get; }
    public FunctionSymbol LessThanInt32 { get; }
    public FunctionSymbol LessThanOrEqualInt32 { get; }
    public FunctionSymbol GreaterThanInt32 { get; }
    public FunctionSymbol GreaterThanOrEqualInt32 { get; }

    // string operators
    public FunctionSymbol ConcatString { get; }
    public FunctionSymbol EqualString { get; }
    public FunctionSymbol NotEqualString { get; }
    public FunctionSymbol LessThanString { get; }
    public FunctionSymbol LessThanOrEqualString { get; }
    public FunctionSymbol GreaterThanString { get; }
    public FunctionSymbol GreaterThanOrEqualString { get; }

    // boolean logical operators
    public FunctionSymbol LogicalNotBoolean { get; }
    public FunctionSymbol LogicalAndBoolean { get; }
    public FunctionSymbol LogicalAndAlsoBoolean { get; }
    public FunctionSymbol LogicalOrElseBoolean { get; }
    public FunctionSymbol LogicalOrBoolean { get; }

    private Operators(SymbolCache symbols)
    {
        this.AddInt32 = BinaryOp(nameof(AddInt32), OperatorKinds.Add, symbols.Int32);
        this.SubtractInt32 = BinaryOp(nameof(SubtractInt32), OperatorKinds.Subtract, symbols.Int32);
        this.MultiplyInt32 = BinaryOp(nameof(MultiplyInt32), OperatorKinds.Multiply, symbols.Int32);
        this.DivideInt32 = BinaryOp(nameof(DivideInt32), OperatorKinds.Divide, symbols.Int32);
        this.RemainderInt32 = BinaryOp(nameof(RemainderInt32), OperatorKinds.Remainder, symbols.Int32);
        this.NegateInt32 = UnaryOp(nameof(NegateInt32), OperatorKinds.Negate, symbols.Int32);
        this.BitwiseAndInt32 = BinaryOp(nameof(BitwiseAndInt32), OperatorKinds.BitwiseAnd, symbols.Int32);
        this.BitwiseOrInt32 = BinaryOp(nameof(BitwiseOrInt32), OperatorKinds.BitwiseOr, symbols.Int32);
        this.BitwiseXorInt32 = BinaryOp(nameof(BitwiseXorInt32), OperatorKinds.BitwiseXor, symbols.Int32);
        this.BitwiseNotInt32 = UnaryOp(nameof(BitwiseNotInt32), OperatorKinds.BitwiseNot, symbols.Int32);
        this.ShiftLeftInt32 = BinaryOp(nameof(ShiftLeftInt32), OperatorKinds.ShiftLeft, symbols.Int32);
        this.ShiftRightInt32 = BinaryOp(nameof(ShiftRightInt32), OperatorKinds.ShiftRight, symbols.Int32);
        this.EqualInt32 = BinaryOp(nameof(EqualInt32), OperatorKinds.Equal, symbols.Int32, symbols.Boolean);
        this.NotEqualInt32 = BinaryOp(nameof(NotEqualInt32), OperatorKinds.NotEqual, symbols.Int32, symbols.Boolean);
        this.LessThanInt32 = BinaryOp(nameof(LessThanInt32), OperatorKinds.LessThan, symbols.Int32, symbols.Boolean);
        this.LessThanOrEqualInt32 = BinaryOp(nameof(LessThanOrEqualInt32), OperatorKinds.LessThanOrEqual, symbols.Int32, symbols.Boolean);
        this.GreaterThanInt32 = BinaryOp(nameof(GreaterThanInt32), OperatorKinds.GreaterThan, symbols.Int32, symbols.Boolean);
        this.GreaterThanOrEqualInt32 = BinaryOp(nameof(GreaterThanOrEqualInt32), OperatorKinds.GreaterThanOrEqual, symbols.Int32, symbols.Boolean);

        // string operators
        this.ConcatString = BinaryOp(nameof(ConcatString), OperatorKinds.Add, symbols.String);
        this.EqualString = BinaryOp(nameof(EqualString), OperatorKinds.Equal, symbols.String, symbols.Boolean);
        this.NotEqualString = BinaryOp(nameof(NotEqualString), OperatorKinds.NotEqual, symbols.String, symbols.Boolean);;
        this.LessThanString = BinaryOp(nameof(LessThanString), OperatorKinds.LessThan, symbols.String, symbols.Boolean);
        this.LessThanOrEqualString = BinaryOp(nameof(LessThanOrEqualString), OperatorKinds.LessThanOrEqual, symbols.String, symbols.Boolean);
        this.GreaterThanString = BinaryOp(nameof(GreaterThanString), OperatorKinds.GreaterThan, symbols.String, symbols.Boolean);
        this.GreaterThanOrEqualString = BinaryOp(nameof(GreaterThanOrEqualString), OperatorKinds.GreaterThanOrEqual, symbols.String, symbols.Boolean);

        // boolean logical operators
        this.LogicalNotBoolean = UnaryOp(nameof(LogicalNotBoolean), OperatorKinds.LogicalNot, symbols.Boolean);
        this.LogicalAndBoolean = BinaryOp(nameof(LogicalAndBoolean), OperatorKinds.LogicalAnd, symbols.Boolean);
        this.LogicalAndAlsoBoolean = BinaryOp(nameof(LogicalAndAlsoBoolean), OperatorKinds.LogicalAndAlso, symbols.Boolean);
        this.LogicalOrElseBoolean = BinaryOp(nameof(LogicalOrElseBoolean), OperatorKinds.LogicalOrElse, symbols.Boolean);
        this.LogicalOrBoolean = BinaryOp(nameof(LogicalOrBoolean), OperatorKinds.LogicalOr, symbols.Boolean);

        // operator intrinsics
        _kindToOperators = new Dictionary<string, ImmutableList<FunctionSymbol>>
        {
            { OperatorKinds.Add, ImmutableList.Create(
                this.AddInt32,
                this.ConcatString) },

            { OperatorKinds.Subtract, ImmutableList.Create(
                this.SubtractInt32) },

            { OperatorKinds.Multiply, ImmutableList.Create(
                this.MultiplyInt32) },

            { OperatorKinds.Divide, ImmutableList.Create(
                this.DivideInt32) },

            { OperatorKinds.Remainder, ImmutableList.Create(
                this.RemainderInt32) },

            { OperatorKinds.Negate, ImmutableList.Create(
                this.NegateInt32) },

            { OperatorKinds.BitwiseAnd, ImmutableList.Create(
                this.BitwiseAndInt32) },

            { OperatorKinds.BitwiseOr, ImmutableList.Create(
                this.BitwiseOrInt32) },

            { OperatorKinds.BitwiseXor, ImmutableList.Create(
                this.BitwiseXorInt32) },

            { OperatorKinds.BitwiseNot, ImmutableList.Create(
                this.BitwiseNotInt32) },

            { OperatorKinds.Equal, ImmutableList.Create(
                this.EqualInt32,
                this.EqualString) },

            { OperatorKinds.NotEqual, ImmutableList.Create(
                this.NotEqualInt32,
                this.NotEqualString) },

            { OperatorKinds.LessThan, ImmutableList.Create(
                this.LessThanInt32) },

            { OperatorKinds.LessThanOrEqual, ImmutableList.Create(
                this.LessThanOrEqualInt32) },

            { OperatorKinds.GreaterThan, ImmutableList.Create(
                this.GreaterThanInt32) },

            { OperatorKinds.GreaterThanOrEqual, ImmutableList.Create(
                this.GreaterThanOrEqualInt32) },

            { OperatorKinds.LogicalAnd, ImmutableList.Create(
                this.LogicalAndBoolean) },

            { OperatorKinds.LogicalAndAlso, ImmutableList.Create(
                this.LogicalAndBoolean) },

            { OperatorKinds.LogicalOr, ImmutableList.Create(
                this.LogicalOrBoolean) },

            { OperatorKinds.LogicalOrElse, ImmutableList.Create(
                this.LogicalOrElseBoolean) },

            { OperatorKinds.LogicalNot, ImmutableList.Create(
                this.LogicalNotBoolean) },
        };
    }
}
