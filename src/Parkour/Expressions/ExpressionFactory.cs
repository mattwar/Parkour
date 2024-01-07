namespace Parkour.Expressions;
using Analysis;
using Symbols;

public static class ExpressionFactory
{
    public static BlockExpression Block(ImmutableList<Expression> expressions) =>
        new BlockExpression(expressions);

    public static BlockExpression Block(IEnumerable<Expression> expressions) =>
        Block(expressions.ToImmutableList());

    public static BranchExpression Branch(string target, Expression? expression = null) =>
        new BranchExpression(target, expression, null);

    public static BranchExpression Break(Expression? expression = null) =>
        BranchExpression.CreateBreak(expression);

    public static BlockExpression Block(params Expression[] expressions) =>
        Block(expressions.ToImmutableList());

    public static CallExpression Call(Expression target, ImmutableList<Expression> arguments) =>
        new CallExpression(target, arguments, null, null);

    public static CallExpression Call(Expression target, IEnumerable<Expression> arguments) =>
        Call(target, arguments.ToImmutableList());

    public static CallExpression Call(Expression target, params Expression[] arguments) =>
        Call(target, arguments.ToImmutableList());

    public static ConditionExpression Condition(Expression test, Expression whenTrue, Expression whenFalse) =>
        new ConditionExpression(test, whenTrue, whenFalse, null);

    public static ConstantExpression Constant(object? value) =>
        new ConstantExpression(value, null);

    public static BranchExpression Continue() =>
        BranchExpression.CreateContinue();

    public static ConvertExpression Convert(Expression expression, TypeSymbol convertedType) =>
        new ConvertExpression(ConversionKind.Narrowing, expression, convertedType);

    public static DeclarationExpression Declare(string name, Expression initializer) =>
        new DeclarationExpression(name, initializer, null, null);

    public static PathExpression Path(Expression expression, ReferenceExpression reference) =>
        new PathExpression(expression, reference);

    public static PathExpression Path(Expression expression, string name) =>
        new PathExpression(expression, Reference(name));

    public static ParameterDeclaration Parameter(string name, TypeSymbol? type = null) =>
        new ParameterDeclaration(name, type);

    public static FunctionExpression Function(ImmutableList<ParameterDeclaration> parameters, Expression body) =>
        new FunctionExpression("", parameters, body, null, null, null);

    public static FunctionExpression Function(IEnumerable<ParameterDeclaration> parameters, Expression body) =>
        Function(parameters.ToImmutableList(), body);

    public static FunctionExpression Function(IEnumerable<string> parameterNames, Expression body) =>
        Function(parameterNames.Select(n => Parameter(n)), body);

    public static FunctionExpression Function(Expression body) =>
        Function(ImmutableList<ParameterDeclaration>.Empty, body);

    public static ReferenceExpression Reference(string name) =>
        new ReferenceExpression(name, null, null);

    public static ReferenceExpression Reference(Symbol referencedSymbol) =>
        new ReferenceExpression(referencedSymbol.Name, referencedSymbol, null);

    public static BranchExpression Return(Expression? expression = null) =>
        BranchExpression.CreateReturn(expression);

    public static VoidExpression Void => VoidExpression.Instance;

    public static CallExpression Add(Expression left, Expression right) =>
        Call(Reference(Operators.Add), ImmutableList.Create(left, right));

    public static CallExpression Subtract(Expression left, Expression right) =>
        Call(Reference(Operators.Subtract), ImmutableList.Create(left, right));

    public static CallExpression Multiply(Expression left, Expression right) =>
        Call(Reference(Operators.Multiply), ImmutableList.Create(left, right));

    public static CallExpression Divide(Expression left, Expression right) =>
        Call(Reference(Operators.Divide), ImmutableList.Create(left, right));

    public static CallExpression Remainder(Expression left, Expression right) =>
        Call(Reference(Operators.Remainder), ImmutableList.Create(left, right));

    public static CallExpression Negate(Expression operand) =>
        Call(Reference(Operators.Negate), ImmutableList.Create(operand));

    public static CallExpression BitwiseAnd(Expression left, Expression right) =>
        Call(Reference(Operators.BitwiseAnd), ImmutableList.Create(left, right));

    public static CallExpression BitwiseOr(Expression left, Expression right) =>
        Call(Reference(Operators.BitwiseOr), ImmutableList.Create(left, right));

    public static CallExpression BitwiseXor(Expression left, Expression right) =>
        Call(Reference(Operators.BitwiseXor), ImmutableList.Create(left, right));

    public static CallExpression BitwiseNot(Expression operand) =>
        Call(Reference(Operators.BitwiseNot), ImmutableList.Create(operand));

    public static CallExpression Equal(Expression left, Expression right) =>
        Call(Reference(Operators.Equal), ImmutableList.Create(left, right));

    public static CallExpression NotEqual(Expression left, Expression right) =>
        Call(Reference(Operators.NotEqual), ImmutableList.Create(left, right));

    public static CallExpression LessThan(Expression left, Expression right) =>
        Call(Reference(Operators.LessThan), ImmutableList.Create(left, right));

    public static CallExpression LessThanOrEqual(Expression left, Expression right) =>
        Call(Reference(Operators.LessThanOrEqual), ImmutableList.Create(left, right));

    public static CallExpression GreaterThan(Expression left, Expression right) =>
        Call(Reference(Operators.GreaterThan), ImmutableList.Create(left, right));

    public static CallExpression GreaterThanOrEqual(Expression left, Expression right) =>
        Call(Reference(Operators.GreaterThanOrEqual), ImmutableList.Create(left, right));

    public static CallExpression And(Expression left, Expression right) =>
        Call(Reference(Operators.LogicalAnd), ImmutableList.Create(left, right));

    public static CallExpression AndAlso(Expression left, Expression right) =>
        Call(Reference(Operators.LogicalAndAlso), ImmutableList.Create(left, right));

    public static CallExpression Or(Expression left, Expression right) =>
        Call(Reference(Operators.LogicalOr), ImmutableList.Create(left, right));

    public static CallExpression OrElse(Expression left, Expression right) =>
        Call(Reference(Operators.LogicalOrElse), ImmutableList.Create(left, right));

    public static CallExpression Not(Expression operand) =>
        Call(Reference(Operators.LogicalNot), ImmutableList.Create(operand));
}
