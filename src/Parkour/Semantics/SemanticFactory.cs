namespace Parkour.Semantics;

public static class SemanticFactory
{
    public static Semantic.Block Block(ImmutableList<Semantic> expressions) =>
        new Semantic.Block(expressions);

    public static Semantic.Block Block(IEnumerable<Semantic> expressions) =>
        Block(expressions.ToImmutableList());

    public static Semantic.Branch Branch(string target, Semantic? expression = null) =>
        new Semantic.Branch(target, expression, null);

    public static Semantic.Branch Break(Semantic? expression = null) =>
        Semantic.Branch.CreateBreak(expression);

    public static Semantic.Block Block(params Semantic[] expressions) =>
        Block(expressions.ToImmutableList());

    public static Semantic.Call Call(Semantic target, ImmutableList<Semantic> arguments) =>
        new Semantic.Call(target, arguments, null, null);

    public static Semantic.Call Call(Semantic target, IEnumerable<Semantic> arguments) =>
        Call(target, arguments.ToImmutableList());

    public static Semantic.Call Call(Semantic target, params Semantic[] arguments) =>
        Call(target, arguments.ToImmutableList());

    public static Semantic.Condition Condition(Semantic test, Semantic whenTrue, Semantic whenFalse) =>
        new Semantic.Condition(test, whenTrue, whenFalse, null);

    public static Semantic.Constant Constant(object? value) =>
        new Semantic.Constant(value, null);

    public static Semantic.Branch Continue() =>
        Semantic.Branch.CreateContinue();

    public static Semantic.Convert Convert(Semantic expression, Symbol.Type convertedType) =>
        new Semantic.Convert(Semantic.ConversionKind.Narrowing, expression, convertedType);

    public static Semantic.Declaration Declare(string name, Semantic initializer) =>
        new Semantic.Declaration(name, initializer, null, null);

    public static Semantic.Path Path(Semantic expression, Semantic.Reference reference) =>
        new Semantic.Path(expression, reference);

    public static Semantic.Path Path(Semantic expression, string name) =>
        new Semantic.Path(expression, Reference(name));

    public static Semantic.Parameter Parameter(string name, Symbol.Type? type = null) =>
        new Semantic.Parameter(name, type);

    public static Semantic.Function Function(ImmutableList<Semantic.Parameter> parameters, Semantic body) =>
        new Semantic.Function("", parameters, body, null, null, null);

    public static Semantic.Function Function(IEnumerable<Semantic.Parameter> parameters, Semantic body) =>
        Function(parameters.ToImmutableList(), body);

    public static Semantic.Function Function(IEnumerable<string> parameterNames, Semantic body) =>
        Function(parameterNames.Select(n => Parameter(n)), body);

    public static Semantic.Function Function(Semantic body) =>
        Function(ImmutableList<Semantic.Parameter>.Empty, body);

    public static Semantic.Reference Reference(string name) =>
        new Semantic.Reference(name, null, null);

    public static Semantic.Reference Reference(Symbol referencedSymbol) =>
        new Semantic.Reference(referencedSymbol.Name, referencedSymbol, null);

    public static Semantic.Branch Return(Semantic? expression = null) =>
        Semantic.Branch.CreateReturn(expression);

    public static Semantic.Void Void => Semantic.Void.Instance;

    public static Semantic.Call Add(Semantic left, Semantic right) =>
        Call(Reference(Operators.Add), ImmutableList.Create(left, right));

    public static Semantic.Call Subtract(Semantic left, Semantic right) =>
        Call(Reference(Operators.Subtract), ImmutableList.Create(left, right));

    public static Semantic.Call Multiply(Semantic left, Semantic right) =>
        Call(Reference(Operators.Multiply), ImmutableList.Create(left, right));

    public static Semantic.Call Divide(Semantic left, Semantic right) =>
        Call(Reference(Operators.Divide), ImmutableList.Create(left, right));

    public static Semantic.Call Remainder(Semantic left, Semantic right) =>
        Call(Reference(Operators.Remainder), ImmutableList.Create(left, right));

    public static Semantic.Call Negate(Semantic operand) =>
        Call(Reference(Operators.Negate), ImmutableList.Create(operand));

    public static Semantic.Call BitwiseAnd(Semantic left, Semantic right) =>
        Call(Reference(Operators.BitwiseAnd), ImmutableList.Create(left, right));

    public static Semantic.Call BitwiseOr(Semantic left, Semantic right) =>
        Call(Reference(Operators.BitwiseOr), ImmutableList.Create(left, right));

    public static Semantic.Call BitwiseXor(Semantic left, Semantic right) =>
        Call(Reference(Operators.BitwiseXor), ImmutableList.Create(left, right));

    public static Semantic.Call BitwiseNot(Semantic operand) =>
        Call(Reference(Operators.BitwiseNot), ImmutableList.Create(operand));

    public static Semantic.Call Equal(Semantic left, Semantic right) =>
        Call(Reference(Operators.Equal), ImmutableList.Create(left, right));

    public static Semantic.Call NotEqual(Semantic left, Semantic right) =>
        Call(Reference(Operators.NotEqual), ImmutableList.Create(left, right));

    public static Semantic.Call LessThan(Semantic left, Semantic right) =>
        Call(Reference(Operators.LessThan), ImmutableList.Create(left, right));

    public static Semantic.Call LessThanOrEqual(Semantic left, Semantic right) =>
        Call(Reference(Operators.LessThanOrEqual), ImmutableList.Create(left, right));

    public static Semantic.Call GreaterThan(Semantic left, Semantic right) =>
        Call(Reference(Operators.GreaterThan), ImmutableList.Create(left, right));

    public static Semantic.Call GreaterThanOrEqual(Semantic left, Semantic right) =>
        Call(Reference(Operators.GreaterThanOrEqual), ImmutableList.Create(left, right));

    public static Semantic.Call And(Semantic left, Semantic right) =>
        Call(Reference(Operators.LogicalAnd), ImmutableList.Create(left, right));

    public static Semantic.Call AndAlso(Semantic left, Semantic right) =>
        Call(Reference(Operators.LogicalAndAlso), ImmutableList.Create(left, right));

    public static Semantic.Call Or(Semantic left, Semantic right) =>
        Call(Reference(Operators.LogicalOr), ImmutableList.Create(left, right));

    public static Semantic.Call OrElse(Semantic left, Semantic right) =>
        Call(Reference(Operators.LogicalOrElse), ImmutableList.Create(left, right));

    public static Semantic.Call Not(Semantic operand) =>
        Call(Reference(Operators.LogicalNot), ImmutableList.Create(operand));
}
