namespace Parkour.Semantics;

/// <summary>
/// Operator functions that are unbound.
/// </summary>
public static class Operators
{
    public static Symbol.Function UnaryOp(string name, Symbol.Type operandType, Symbol.Type resultType) =>
        SymbolFactory.Operator(name, resultType, SymbolFactory.Parameter("operand", operandType));

    public static Symbol.Function UnaryOp(string name, Symbol.Type operand) =>
        UnaryOp(name, operand, operand);

    public static Symbol.Function BinaryOp(string name, Symbol.Type leftType, Symbol.Type rightType, Symbol.Type resultType) =>
        SymbolFactory.Operator(name, resultType, SymbolFactory.Parameter("left", leftType), SymbolFactory.Parameter("right", rightType));

    public static Symbol.Function BinaryOp(string name, Symbol.Type argsType, Symbol.Type resultType) =>
        BinaryOp(name, argsType, argsType, resultType);

    public static Symbol.Function BinaryOp(string name, Symbol.Type type) =>
        BinaryOp(name, type, type, type);

    public static readonly Symbol.Function Add =
        BinaryOp(nameof(Add), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function Subtract =
        BinaryOp(nameof(Subtract), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function Multiply =
        BinaryOp(nameof(Multiply), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function Divide =
        BinaryOp(nameof(Divide), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function Remainder =
        BinaryOp(nameof(Remainder), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function Negate =
        UnaryOp(nameof(Negate), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function BitwiseAnd =
        BinaryOp(nameof(BitwiseAnd), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function BitwiseOr =
        BinaryOp(nameof(BitwiseOr), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function BitwiseXor =
        BinaryOp(nameof(BitwiseXor), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function BitwiseNot =
        UnaryOp(nameof(BitwiseNot), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function ShiftLeft =
        BinaryOp(nameof(ShiftLeft), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function ShiftRight =
        BinaryOp(nameof(ShiftRight), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function Equal =
        BinaryOp(nameof(Equal), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function NotEqual =
        BinaryOp(nameof(NotEqual), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LessThan =
        BinaryOp(nameof(LessThan), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LessThanOrEqual =
        BinaryOp(nameof(LessThanOrEqual), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function GreaterThan =
        BinaryOp(nameof(GreaterThan), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function GreaterThanOrEqual =
        BinaryOp(nameof(GreaterThanOrEqual), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LogicalNot =
        UnaryOp(nameof(LogicalNot), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LogicalAnd =
        BinaryOp(nameof(LogicalAnd), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LogicalAndAlso =
        BinaryOp(nameof(LogicalAndAlso), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LogicalOr =
        BinaryOp(nameof(LogicalOr), SymbolModel.Any, SymbolModel.Unknown);

    public static readonly Symbol.Function LogicalOrElse =
        BinaryOp(nameof(LogicalOrElse), SymbolModel.Any, SymbolModel.Unknown);

}

public sealed class Intrinsics
{
    public static Symbol.Function UnaryOp(string name, Symbol.Function related, Symbol.Type operandType, Symbol.Type resultType) =>
        SymbolFactory.Intrinsic(name, related, resultType, SymbolFactory.Parameter("operand", operandType));

    public static Symbol.Function UnaryOp(string name, Symbol.Function related, Symbol.Type operand) =>
        UnaryOp(name, related, operand, operand);

    public static Symbol.Function BinaryOp(string name, Symbol.Function related, Symbol.Type leftType, Symbol.Type rightType, Symbol.Type resultType) =>
        SymbolFactory.Intrinsic(name, related, resultType, SymbolFactory.Parameter("left", leftType), SymbolFactory.Parameter("right", rightType));

    public static Symbol.Function BinaryOp(string name, Symbol.Function related, Symbol.Type argsType, Symbol.Type resultType) =>
        BinaryOp(name, related, argsType, argsType, resultType);

    public static Symbol.Function BinaryOp(string name, Symbol.Function related, Symbol.Type type) =>
        BinaryOp(name, related, type, type, type);

    // Int32 operators
    public Symbol.Function AddInt32 { get; }
    public Symbol.Function SubtractInt32 { get; }
    public Symbol.Function MultiplyInt32 { get; }
    public Symbol.Function DivideInt32 { get; }
    public Symbol.Function RemainderInt32 { get; }
    public Symbol.Function NegateInt32 { get; }
    public Symbol.Function BitwiseAndInt32 { get; }
    public Symbol.Function BitwiseOrInt32 { get; }
    public Symbol.Function BitwiseXorInt32 { get; }
    public Symbol.Function BitwiseNotInt32 { get; }
    public Symbol.Function ShiftLeftInt32 { get; }
    public Symbol.Function ShiftRightInt32 { get; }
    public Symbol.Function EqualInt32 { get; }
    public Symbol.Function NotEqualInt32 { get; }
    public Symbol.Function LessThanInt32 { get; }
    public Symbol.Function LessThanOrEqualInt32 { get; }
    public Symbol.Function GreaterThanInt32 { get; }
    public Symbol.Function GreaterThanOrEqualInt32 { get; }

    // string operators
    public Symbol.Function ConcatString { get; }
    public Symbol.Function EqualString { get; }
    public Symbol.Function NotEqualString { get; }
    public Symbol.Function LessThanString { get; }
    public Symbol.Function LessThanOrEqualString { get; }
    public Symbol.Function GreaterThanString { get; }
    public Symbol.Function GreaterThanOrEqualString { get; }

    // boolean logical operators
    public Symbol.Function LogicalNotBoolean { get; }
    public Symbol.Function LogicalAndBoolean { get; }
    public Symbol.Function LogicalAndAlsoBoolean { get; }
    public Symbol.Function LogicalOrElseBoolean { get; }
    public Symbol.Function LogicalOrBoolean { get; }

    public Intrinsics(SymbolModel model)
    {
        this.AddInt32 = BinaryOp(nameof(AddInt32), Operators.Add, model.Int32);
        this.SubtractInt32 = BinaryOp(nameof(SubtractInt32), Operators.Subtract, model.Int32);
        this.MultiplyInt32 = BinaryOp(nameof(MultiplyInt32), Operators.Multiply, model.Int32);
        this.DivideInt32 = BinaryOp(nameof(DivideInt32), Operators.Divide, model.Int32);
        this.RemainderInt32 = BinaryOp(nameof(RemainderInt32), Operators.Remainder, model.Int32);
        this.NegateInt32 = UnaryOp(nameof(NegateInt32), Operators.Negate, model.Int32);
        this.BitwiseAndInt32 = BinaryOp(nameof(BitwiseAndInt32), Operators.BitwiseAnd, model.Int32);
        this.BitwiseOrInt32 = BinaryOp(nameof(BitwiseOrInt32), Operators.BitwiseOr, model.Int32);
        this.BitwiseXorInt32 = BinaryOp(nameof(BitwiseXorInt32), Operators.BitwiseXor, model.Int32);
        this.BitwiseNotInt32 = UnaryOp(nameof(BitwiseNotInt32), Operators.BitwiseNot, model.Int32);
        this.ShiftLeftInt32 = BinaryOp(nameof(ShiftLeftInt32), Operators.ShiftLeft, model.Int32);
        this.ShiftRightInt32 = BinaryOp(nameof(ShiftRightInt32), Operators.ShiftRight, model.Int32);
        this.EqualInt32 = BinaryOp(nameof(EqualInt32), Operators.Equal, model.Int32, model.Boolean);
        this.NotEqualInt32 = BinaryOp(nameof(NotEqualInt32), Operators.NotEqual, model.Int32, model.Boolean);
        this.LessThanInt32 = BinaryOp(nameof(LessThanInt32), Operators.LessThan, model.Int32, model.Boolean);
        this.LessThanOrEqualInt32 = BinaryOp(nameof(LessThanOrEqualInt32), Operators.LessThanOrEqual, model.Int32, model.Boolean);
        this.GreaterThanInt32 = BinaryOp(nameof(GreaterThanInt32), Operators.GreaterThan, model.Int32, model.Boolean);
        this.GreaterThanOrEqualInt32 = BinaryOp(nameof(GreaterThanOrEqualInt32), Operators.GreaterThanOrEqual, model.Int32, model.Boolean);

        // string operators
        this.ConcatString = BinaryOp(nameof(ConcatString), Operators.Add, model.String);
        this.EqualString = BinaryOp(nameof(EqualString), Operators.Equal, model.String, model.Boolean);
        this.NotEqualString = BinaryOp(nameof(NotEqualString), Operators.NotEqual, model.String, model.Boolean);;
        this.LessThanString = BinaryOp(nameof(LessThanString), Operators.LessThan, model.String, model.Boolean);
        this.LessThanOrEqualString = BinaryOp(nameof(LessThanOrEqualString), Operators.LessThanOrEqual, model.String, model.Boolean);
        this.GreaterThanString = BinaryOp(nameof(GreaterThanString), Operators.GreaterThan, model.String, model.Boolean);
        this.GreaterThanOrEqualString = BinaryOp(nameof(GreaterThanOrEqualString), Operators.GreaterThanOrEqual, model.String, model.Boolean);

        // boolean logical operators
        this.LogicalNotBoolean = UnaryOp(nameof(LogicalNotBoolean), Operators.LogicalNot, model.Boolean);
        this.LogicalAndBoolean = BinaryOp(nameof(LogicalAndBoolean), Operators.LogicalAnd, model.Boolean);
        this.LogicalAndAlsoBoolean = BinaryOp(nameof(LogicalAndAlsoBoolean), Operators.LogicalAndAlso, model.Boolean);
        this.LogicalOrElseBoolean = BinaryOp(nameof(LogicalOrElseBoolean), Operators.LogicalOrElse, model.Boolean);
        this.LogicalOrBoolean = BinaryOp(nameof(LogicalOrBoolean), Operators.LogicalOr, model.Boolean);

        // operator intrinsics
        _nameToIntrinsics = new Dictionary<Symbol.Function, ImmutableList<Symbol.Function>>
        {
            { Operators.Add, ImmutableList.Create(
                this.AddInt32,
                this.ConcatString) },

            { Operators.Subtract, ImmutableList.Create(
                this.SubtractInt32) },

            { Operators.Multiply, ImmutableList.Create(
                this.MultiplyInt32) },

            { Operators.Divide, ImmutableList.Create(
                this.DivideInt32) },

            { Operators.Remainder, ImmutableList.Create(
                this.RemainderInt32) },

            { Operators.Negate, ImmutableList.Create(
                this.NegateInt32) },

            { Operators.BitwiseAnd, ImmutableList.Create(
                this.BitwiseAndInt32) },

            { Operators.BitwiseOr, ImmutableList.Create(
                this.BitwiseOrInt32) },

            { Operators.BitwiseXor, ImmutableList.Create(
                this.BitwiseXorInt32) },

            { Operators.BitwiseNot, ImmutableList.Create(
                this.BitwiseNotInt32) },

            { Operators.Equal, ImmutableList.Create(
                this.EqualInt32,
                this.EqualString) },

            { Operators.NotEqual, ImmutableList.Create(
                this.NotEqualInt32,
                this.NotEqualString) },

            { Operators.LessThan, ImmutableList.Create(
                this.LessThanInt32) },

            { Operators.LessThanOrEqual, ImmutableList.Create(
                this.LessThanOrEqualInt32) },

            { Operators.GreaterThan, ImmutableList.Create(
                this.GreaterThanInt32) },

            { Operators.GreaterThanOrEqual, ImmutableList.Create(
                this.GreaterThanOrEqualInt32) },

            { Operators.LogicalAnd, ImmutableList.Create(
                this.LogicalAndBoolean) },

            { Operators.LogicalAndAlso, ImmutableList.Create(
                this.LogicalAndBoolean) },

            { Operators.LogicalOr, ImmutableList.Create(
                this.LogicalOrBoolean) },

            { Operators.LogicalOrElse, ImmutableList.Create(
                this.LogicalOrElseBoolean) },

            { Operators.LogicalNot, ImmutableList.Create(
                this.LogicalNotBoolean) },
        };
    }

    public ImmutableList<Symbol.Function> GetOperatorIntrinsics(Symbol.Function @operator) =>
        _nameToIntrinsics.TryGetValue(@operator, out var intrinsics)
            ? intrinsics
            : ImmutableList<Symbol.Function>.Empty;

    private readonly Dictionary<Symbol.Function, ImmutableList<Symbol.Function>> _nameToIntrinsics;



}
