using System.Runtime.CompilerServices;

namespace Parkour.Binding;
using Symbols;

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

    private readonly Dictionary<string, ImmutableList<OperatorSymbol>> _kindToOperators;

    public ImmutableList<OperatorSymbol> GetOperators(string kind) =>
        _kindToOperators.TryGetValue(kind, out var intrinsics)
            ? intrinsics
            : ImmutableList<OperatorSymbol>.Empty;

    public static OperatorSymbol UnaryOp(string name, string kind, TypeSymbol operandType, TypeSymbol resultType) =>
        new OperatorSymbol(
            name, 
            kind, 
            me => ImmutableList.Create(new ParameterSymbol("operand", me, operandType)),
            () => resultType
            );

    public static OperatorSymbol UnaryOp(string name, string kind, TypeSymbol operand) =>
        UnaryOp(name, kind, operand, operand);

    public static OperatorSymbol BinaryOp(string name, string kind, TypeSymbol leftType, TypeSymbol rightType, TypeSymbol resultType) =>
        new OperatorSymbol(
            name,
            kind,
            me => ImmutableList.Create(
                new ParameterSymbol("left", me, leftType),
                new ParameterSymbol("right", me, rightType)
                ),
            () => resultType
            );

    public static OperatorSymbol BinaryOp(string name, string kind, TypeSymbol argsType, TypeSymbol resultType) =>
        BinaryOp(name, kind, argsType, argsType, resultType);

    public static OperatorSymbol BinaryOp(string name, string kind, TypeSymbol type) =>
        BinaryOp(name, kind, type, type, type);

    // Int32 operators
    public OperatorSymbol AddInt32 { get; }
    public OperatorSymbol SubtractInt32 { get; }
    public OperatorSymbol MultiplyInt32 { get; }
    public OperatorSymbol DivideInt32 { get; }
    public OperatorSymbol RemainderInt32 { get; }
    public OperatorSymbol NegateInt32 { get; }
    public OperatorSymbol BitwiseAndInt32 { get; }
    public OperatorSymbol BitwiseOrInt32 { get; }
    public OperatorSymbol BitwiseXorInt32 { get; }
    public OperatorSymbol BitwiseNotInt32 { get; }
    public OperatorSymbol ShiftLeftInt32 { get; }
    public OperatorSymbol ShiftRightInt32 { get; }
    public OperatorSymbol EqualInt32 { get; }
    public OperatorSymbol NotEqualInt32 { get; }
    public OperatorSymbol LessThanInt32 { get; }
    public OperatorSymbol LessThanOrEqualInt32 { get; }
    public OperatorSymbol GreaterThanInt32 { get; }
    public OperatorSymbol GreaterThanOrEqualInt32 { get; }

    // string operators
    public OperatorSymbol ConcatString { get; }
    public OperatorSymbol EqualString { get; }
    public OperatorSymbol NotEqualString { get; }
    public OperatorSymbol LessThanString { get; }
    public OperatorSymbol LessThanOrEqualString { get; }
    public OperatorSymbol GreaterThanString { get; }
    public OperatorSymbol GreaterThanOrEqualString { get; }

    // boolean logical operators
    public OperatorSymbol LogicalNotBoolean { get; }
    public OperatorSymbol LogicalAndBoolean { get; }
    public OperatorSymbol LogicalAndAlsoBoolean { get; }
    public OperatorSymbol LogicalOrElseBoolean { get; }
    public OperatorSymbol LogicalOrBoolean { get; }

    private Operators(SymbolCache symbols)
    {
        this.AddInt32 = BinaryOp(nameof(AddInt32), OperatorKind.Add, symbols.Int32);
        this.SubtractInt32 = BinaryOp(nameof(SubtractInt32), OperatorKind.Subtract, symbols.Int32);
        this.MultiplyInt32 = BinaryOp(nameof(MultiplyInt32), OperatorKind.Multiply, symbols.Int32);
        this.DivideInt32 = BinaryOp(nameof(DivideInt32), OperatorKind.Divide, symbols.Int32);
        this.RemainderInt32 = BinaryOp(nameof(RemainderInt32), OperatorKind.Remainder, symbols.Int32);
        this.NegateInt32 = UnaryOp(nameof(NegateInt32), OperatorKind.Negate, symbols.Int32);
        this.BitwiseAndInt32 = BinaryOp(nameof(BitwiseAndInt32), OperatorKind.BitwiseAnd, symbols.Int32);
        this.BitwiseOrInt32 = BinaryOp(nameof(BitwiseOrInt32), OperatorKind.BitwiseOr, symbols.Int32);
        this.BitwiseXorInt32 = BinaryOp(nameof(BitwiseXorInt32), OperatorKind.BitwiseXor, symbols.Int32);
        this.BitwiseNotInt32 = UnaryOp(nameof(BitwiseNotInt32), OperatorKind.BitwiseNot, symbols.Int32);
        this.ShiftLeftInt32 = BinaryOp(nameof(ShiftLeftInt32), OperatorKind.ShiftLeft, symbols.Int32);
        this.ShiftRightInt32 = BinaryOp(nameof(ShiftRightInt32), OperatorKind.ShiftRight, symbols.Int32);
        this.EqualInt32 = BinaryOp(nameof(EqualInt32), OperatorKind.Equal, symbols.Int32, symbols.Boolean);
        this.NotEqualInt32 = BinaryOp(nameof(NotEqualInt32), OperatorKind.NotEqual, symbols.Int32, symbols.Boolean);
        this.LessThanInt32 = BinaryOp(nameof(LessThanInt32), OperatorKind.LessThan, symbols.Int32, symbols.Boolean);
        this.LessThanOrEqualInt32 = BinaryOp(nameof(LessThanOrEqualInt32), OperatorKind.LessThanOrEqual, symbols.Int32, symbols.Boolean);
        this.GreaterThanInt32 = BinaryOp(nameof(GreaterThanInt32), OperatorKind.GreaterThan, symbols.Int32, symbols.Boolean);
        this.GreaterThanOrEqualInt32 = BinaryOp(nameof(GreaterThanOrEqualInt32), OperatorKind.GreaterThanOrEqual, symbols.Int32, symbols.Boolean);

        // string operators
        this.ConcatString = BinaryOp(nameof(ConcatString), OperatorKind.Add, symbols.String);
        this.EqualString = BinaryOp(nameof(EqualString), OperatorKind.Equal, symbols.String, symbols.Boolean);
        this.NotEqualString = BinaryOp(nameof(NotEqualString), OperatorKind.NotEqual, symbols.String, symbols.Boolean);;
        this.LessThanString = BinaryOp(nameof(LessThanString), OperatorKind.LessThan, symbols.String, symbols.Boolean);
        this.LessThanOrEqualString = BinaryOp(nameof(LessThanOrEqualString), OperatorKind.LessThanOrEqual, symbols.String, symbols.Boolean);
        this.GreaterThanString = BinaryOp(nameof(GreaterThanString), OperatorKind.GreaterThan, symbols.String, symbols.Boolean);
        this.GreaterThanOrEqualString = BinaryOp(nameof(GreaterThanOrEqualString), OperatorKind.GreaterThanOrEqual, symbols.String, symbols.Boolean);

        // boolean logical operators
        this.LogicalNotBoolean = UnaryOp(nameof(LogicalNotBoolean), OperatorKind.LogicalNot, symbols.Boolean);
        this.LogicalAndBoolean = BinaryOp(nameof(LogicalAndBoolean), OperatorKind.LogicalAnd, symbols.Boolean);
        this.LogicalAndAlsoBoolean = BinaryOp(nameof(LogicalAndAlsoBoolean), OperatorKind.LogicalAndAlso, symbols.Boolean);
        this.LogicalOrElseBoolean = BinaryOp(nameof(LogicalOrElseBoolean), OperatorKind.LogicalOrElse, symbols.Boolean);
        this.LogicalOrBoolean = BinaryOp(nameof(LogicalOrBoolean), OperatorKind.LogicalOr, symbols.Boolean);

        // operator intrinsics
        _kindToOperators = new Dictionary<string, ImmutableList<OperatorSymbol>>
        {
            { OperatorKind.Add, ImmutableList.Create(
                this.AddInt32,
                this.ConcatString) },

            { OperatorKind.Subtract, ImmutableList.Create(
                this.SubtractInt32) },

            { OperatorKind.Multiply, ImmutableList.Create(
                this.MultiplyInt32) },

            { OperatorKind.Divide, ImmutableList.Create(
                this.DivideInt32) },

            { OperatorKind.Remainder, ImmutableList.Create(
                this.RemainderInt32) },

            { OperatorKind.Negate, ImmutableList.Create(
                this.NegateInt32) },

            { OperatorKind.BitwiseAnd, ImmutableList.Create(
                this.BitwiseAndInt32) },

            { OperatorKind.BitwiseOr, ImmutableList.Create(
                this.BitwiseOrInt32) },

            { OperatorKind.BitwiseXor, ImmutableList.Create(
                this.BitwiseXorInt32) },

            { OperatorKind.BitwiseNot, ImmutableList.Create(
                this.BitwiseNotInt32) },

            { OperatorKind.Equal, ImmutableList.Create(
                this.EqualInt32,
                this.EqualString) },

            { OperatorKind.NotEqual, ImmutableList.Create(
                this.NotEqualInt32,
                this.NotEqualString) },

            { OperatorKind.LessThan, ImmutableList.Create(
                this.LessThanInt32) },

            { OperatorKind.LessThanOrEqual, ImmutableList.Create(
                this.LessThanOrEqualInt32) },

            { OperatorKind.GreaterThan, ImmutableList.Create(
                this.GreaterThanInt32) },

            { OperatorKind.GreaterThanOrEqual, ImmutableList.Create(
                this.GreaterThanOrEqualInt32) },

            { OperatorKind.LogicalAnd, ImmutableList.Create(
                this.LogicalAndBoolean) },

            { OperatorKind.LogicalAndAlso, ImmutableList.Create(
                this.LogicalAndBoolean) },

            { OperatorKind.LogicalOr, ImmutableList.Create(
                this.LogicalOrBoolean) },

            { OperatorKind.LogicalOrElse, ImmutableList.Create(
                this.LogicalOrElseBoolean) },

            { OperatorKind.LogicalNot, ImmutableList.Create(
                this.LogicalNotBoolean) },
        };
    }
}
