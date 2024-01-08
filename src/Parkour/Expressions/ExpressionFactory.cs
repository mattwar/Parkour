namespace Parkour.Expressions;
using Analysis;
using Symbols;
using System.Reflection.Metadata;

public static class ExpressionFactory
{
    public static AssignExpression Assign(Expression target, Expression expression) =>
        new AssignExpression(target, expression);

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

    public static FunctionExpression Function(ImmutableList<ParameterDeclaration> parameters, Expression body) =>
        new FunctionExpression("", parameters, body, null, null, null);

    public static FunctionExpression Function(IEnumerable<ParameterDeclaration> parameters, Expression body) =>
        Function(parameters.ToImmutableList(), body);

    public static FunctionExpression Function(IEnumerable<string> parameterNames, Expression body) =>
        Function(parameterNames.Select(n => Parameter(n)), body);

    public static FunctionExpression Function(Expression body) =>
        Function(ImmutableList<ParameterDeclaration>.Empty, body);

    public static OperatorExpression Operator(string name) =>
        new OperatorExpression(name, null, null);

    public static ReferenceExpression Reference(string name) =>
        new ReferenceExpression(name, null, null);

    public static BranchExpression Return(Expression? expression = null) =>
        BranchExpression.CreateReturn(expression);

    public static VoidExpression Void() => VoidExpression.Instance;

    public static CallExpression Add(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.Add), ImmutableList.Create(left, right));

    public static CallExpression Subtract(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.Subtract), ImmutableList.Create(left, right));

    public static CallExpression Multiply(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.Multiply), ImmutableList.Create(left, right));

    public static CallExpression Divide(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.Divide), ImmutableList.Create(left, right));

    public static CallExpression Remainder(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.Remainder), ImmutableList.Create(left, right));

    public static CallExpression Negate(Expression operand) =>
        Call(Operator(OperatorKinds.Negate), ImmutableList.Create(operand));

    public static CallExpression BitwiseAnd(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.BitwiseAnd), ImmutableList.Create(left, right));

    public static CallExpression BitwiseOr(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.BitwiseOr), ImmutableList.Create(left, right));

    public static CallExpression BitwiseXor(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.BitwiseXor), ImmutableList.Create(left, right));

    public static CallExpression BitwiseNot(Expression operand) =>
        Call(Operator(OperatorKinds.BitwiseNot), ImmutableList.Create(operand));

    public static CallExpression Equal(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.Equal), ImmutableList.Create(left, right));

    public static CallExpression NotEqual(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.NotEqual), ImmutableList.Create(left, right));

    public static CallExpression LessThan(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.LessThan), ImmutableList.Create(left, right));

    public static CallExpression LessThanOrEqual(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.LessThanOrEqual), ImmutableList.Create(left, right));

    public static CallExpression GreaterThan(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.GreaterThan), ImmutableList.Create(left, right));

    public static CallExpression GreaterThanOrEqual(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.GreaterThanOrEqual), ImmutableList.Create(left, right));

    public static CallExpression And(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.LogicalAnd), ImmutableList.Create(left, right));

    public static CallExpression AndAlso(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.LogicalAndAlso), ImmutableList.Create(left, right));

    public static CallExpression Or(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.LogicalOr), ImmutableList.Create(left, right));

    public static CallExpression OrElse(Expression left, Expression right) =>
        Call(Operator(OperatorKinds.LogicalOrElse), ImmutableList.Create(left, right));

    public static CallExpression Not(Expression operand) =>
        Call(Operator(OperatorKinds.LogicalNot), ImmutableList.Create(operand));

    public static ParameterDeclaration Parameter(string name, Expression? parameterType = null) =>
        new ParameterDeclaration(name, parameterType);

    public static MethodDeclaration Method(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression body, Expression returnType) =>
        new MethodDeclaration(name, access, modifiers, parameters, body, returnType);

    public static FieldDeclaration Field(string name, SymbolAccess access, SymbolModifier modifiers, Expression fieldType, Expression? initalizer) =>
        new FieldDeclaration(name, access, modifiers, fieldType, initalizer);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, MethodDeclaration getMethod, MethodDeclaration? setMethod, FieldDeclaration? underlyingField, Expression propertyType) =>
        new PropertyDeclaration(name, access, modifiers, getMethod, setMethod, underlyingField, propertyType);

    public static PropertyDeclaration Property(string name, MethodDeclaration getMethod, MethodDeclaration? setMethod = null) =>
        Property(name, getMethod.Access, getMethod.Modifiers, getMethod, setMethod, null, getMethod.ReturnType);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, Expression expression) =>
        Property(name, Method("get_" + name, access, modifiers, ImmutableList<ParameterDeclaration>.Empty, expression, propertyType));

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType) =>
        Property(name,
            access,
            modifiers,
            Method("get_" + name, access, modifiers, ImmutableList<ParameterDeclaration>.Empty, Reference("field"), propertyType),
            Method("set_" + name, access, modifiers, ImmutableList.Create(Parameter("value", propertyType)), Assign(Reference("field"), Reference("value")), Void()),
            Field("field", SymbolAccess.Private, SymbolModifier.None, propertyType, null),
            propertyType);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations) =>
        new ClassDeclaration(name, access, modifiers, baseTypes, declarations, null);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, params Declaration[] declarations) =>
        Class(name, access, modifiers, ImmutableList<Expression>.Empty, declarations.ToImmutableList());

    public static NamespaceDeclaration Namespace(string name, ImmutableList<Declaration> declarations) =>
        new NamespaceDeclaration(name, declarations);

    public static NamespaceDeclaration Namespace(string name, params Declaration[] declarations) =>
        Namespace(name, declarations.ToImmutableList());
}
