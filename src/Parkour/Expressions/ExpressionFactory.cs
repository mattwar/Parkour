namespace Parkour.Expressions;
using Analysis;
using Symbols;
using Syntax;

public static class ExpressionFactory
{
    public static AssignExpression Assign(Expression target, Expression expression, SyntaxElement? syntax = null) =>
        new AssignExpression(target, expression, null, syntax);

    public static BlockExpression Block(ImmutableList<Expression> expressions, SyntaxElement? syntax = null) =>
        new BlockExpression(expressions, null, syntax);

    public static BlockExpression Block(IEnumerable<Expression> expressions, SyntaxElement? syntax = null) =>
        Block(expressions.ToImmutableList(), syntax);

    public static BlockExpression Block(params Expression[] expressions) =>
        Block(expressions.ToImmutableList());

    public static BranchExpression Branch(string target, Expression? expression = null, SyntaxElement? syntax = null) =>
        new BranchExpression(target, expression, null, null, syntax);

    public static BranchExpression Break(Expression? expression = null, SyntaxElement? syntax = null) =>
        BranchExpression.CreateBreak(expression, null, null, syntax);

    public static CallExpression Call(Expression target, ImmutableList<Expression> arguments, SyntaxElement? syntax = null) =>
        new CallExpression(target, arguments, null, null, null, syntax);

    public static CallExpression Call(Expression target, IEnumerable<Expression> arguments, SyntaxElement? syntax = null) =>
        Call(target, arguments.ToImmutableList(), syntax);

    public static CallExpression Call(Expression target, params Expression[] arguments) =>
        Call(target, arguments.ToImmutableList());

    public static ConditionExpression Condition(Expression test, Expression whenTrue, Expression whenFalse, SyntaxElement? syntax = null) =>
        new ConditionExpression(test, whenTrue, whenFalse, null, null, syntax);

    public static ConstantExpression Constant(object? value, SyntaxElement? syntax = null) =>
        new ConstantExpression(value, null, null, syntax);

    public static BranchExpression Continue(SyntaxElement? syntax = null) =>
        BranchExpression.CreateContinue(null, null, syntax);

    public static ConvertExpression Convert(Expression expression, TypeSymbol convertedType, SyntaxElement? syntax = null) =>
        new ConvertExpression(ConversionKind.Narrowing, expression, convertedType, null, null, null, syntax);

    public static DeclarationExpression Declare(string name, Expression initializer, SyntaxElement? syntax = null) =>
        new DeclarationExpression(name, initializer, null, null, null, syntax);

    public static PathExpression Path(Expression expression, ReferenceExpression reference, SyntaxElement? syntax = null) =>
        new PathExpression(expression, reference, null, syntax);

    public static PathExpression Path(Expression expression, string name, SyntaxElement? syntax = null) =>
        Path(expression, Reference(name), syntax);

    public static FunctionExpression Function(ImmutableList<ParameterDeclaration> parameters, Expression body, SyntaxElement? syntax = null) =>
        new FunctionExpression("", parameters, body, null, null, null, null, syntax);

    public static FunctionExpression Function(IEnumerable<ParameterDeclaration> parameters, Expression body, SyntaxElement? syntax = null) =>
        Function(parameters.ToImmutableList(), body, syntax);

    public static FunctionExpression Function(IEnumerable<string> parameterNames, Expression body, SyntaxElement? syntax = null) =>
        Function(parameterNames.Select(n => Parameter(n)), body, syntax);

    public static FunctionExpression Function(Expression body, SyntaxElement? syntax = null) =>
        Function(ImmutableList<ParameterDeclaration>.Empty, body, syntax);

    public static OperatorExpression Operator(string name, SyntaxElement? syntax = null) =>
        new OperatorExpression(name, null, null, null, syntax);

    public static ReferenceExpression Reference(string name, SyntaxElement? syntax = null) =>
        new ReferenceExpression(name, null, null, null, syntax);

    public static BranchExpression Return(Expression? expression = null, SyntaxElement? syntax = null) =>
        BranchExpression.CreateReturn(expression, null, null, syntax);

    public static VoidExpression Void() => VoidExpression.Instance;

    public static CallExpression Add(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Add), ImmutableList.Create(left, right), syntax);

    public static CallExpression Subtract(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Subtract), ImmutableList.Create(left, right), syntax);

    public static CallExpression Multiply(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Multiply), ImmutableList.Create(left, right), syntax);

    public static CallExpression Divide(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Divide), ImmutableList.Create(left, right), syntax);

    public static CallExpression Remainder(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Remainder), ImmutableList.Create(left, right), syntax);

    public static CallExpression Negate(Expression operand, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Negate), ImmutableList.Create(operand), syntax);

    public static CallExpression BitwiseAnd(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.BitwiseAnd), ImmutableList.Create(left, right), syntax);

    public static CallExpression BitwiseOr(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.BitwiseOr), ImmutableList.Create(left, right), syntax);

    public static CallExpression BitwiseXor(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.BitwiseXor), ImmutableList.Create(left, right), syntax);

    public static CallExpression BitwiseNot(Expression operand, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.BitwiseNot), ImmutableList.Create(operand), syntax);

    public static CallExpression Equal(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.Equal), ImmutableList.Create(left, right), syntax);

    public static CallExpression NotEqual(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.NotEqual), ImmutableList.Create(left, right), syntax);

    public static CallExpression LessThan(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LessThan), ImmutableList.Create(left, right), syntax);

    public static CallExpression LessThanOrEqual(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LessThanOrEqual), ImmutableList.Create(left, right), syntax);

    public static CallExpression GreaterThan(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.GreaterThan), ImmutableList.Create(left, right), syntax);

    public static CallExpression GreaterThanOrEqual(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.GreaterThanOrEqual), ImmutableList.Create(left, right), syntax);

    public static CallExpression And(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LogicalAnd), ImmutableList.Create(left, right), syntax);

    public static CallExpression AndAlso(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LogicalAndAlso), ImmutableList.Create(left, right), syntax);

    public static CallExpression Or(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LogicalOr), ImmutableList.Create(left, right), syntax);

    public static CallExpression OrElse(Expression left, Expression right, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LogicalOrElse), ImmutableList.Create(left, right), syntax);

    public static CallExpression Not(Expression operand, SyntaxElement? syntax = null) =>
        Call(Operator(OperatorKinds.LogicalNot), ImmutableList.Create(operand), syntax);

    public static ParameterDeclaration Parameter(string name, Expression? parameterType = null, SyntaxElement? syntax = null) =>
        new ParameterDeclaration(name, parameterType, null, syntax);

    public static MethodDeclaration Method(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression body, Expression returnType, SyntaxElement? syntax = null) =>
        new MethodDeclaration(name, access, modifiers, parameters, body, returnType, null, syntax);

    public static FieldDeclaration Field(string name, SymbolAccess access, SymbolModifier modifiers, Expression fieldType, Expression? initalizer, SyntaxElement? syntax = null) =>
        new FieldDeclaration(name, access, modifiers, fieldType, initalizer, null, syntax);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, MethodDeclaration getMethod, MethodDeclaration? setMethod, FieldDeclaration? underlyingField, Expression propertyType, SyntaxElement? syntax = null) =>
        new PropertyDeclaration(name, access, modifiers, getMethod, setMethod, underlyingField, propertyType, null, syntax);

    public static PropertyDeclaration Property(string name, MethodDeclaration getMethod, MethodDeclaration? setMethod = null, SyntaxElement? syntax = null) =>
        Property(name, getMethod.Access, getMethod.Modifiers, getMethod, setMethod, null, getMethod.ReturnType, syntax);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, Expression expression, SyntaxElement? syntax = null) =>
        Property(name, Method("get_" + name, access, modifiers, ImmutableList<ParameterDeclaration>.Empty, expression, propertyType, syntax), null, syntax);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, SyntaxElement? syntax = null) =>
        Property(name,
            access,
            modifiers,
            Method("get_" + name, access, modifiers, ImmutableList<ParameterDeclaration>.Empty, Reference("field"), propertyType),
            Method("set_" + name, access, modifiers, ImmutableList.Create(Parameter("value", propertyType)), Assign(Reference("field"), Reference("value")), Void()),
            Field("field", SymbolAccess.Private, SymbolModifier.None, propertyType, null),
            propertyType,
            syntax);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations, SyntaxElement? syntax = null) =>
        new ClassDeclaration(name, access, modifiers, baseTypes, declarations, null, syntax);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, params Declaration[] declarations) =>
        Class(name, access, modifiers, ImmutableList<Expression>.Empty, declarations.ToImmutableList());

    public static NamespaceDeclaration Namespace(string name, ImmutableList<Declaration> declarations, SyntaxElement? syntax = null) =>
        new NamespaceDeclaration(name, declarations, null, syntax);

    public static NamespaceDeclaration Namespace(string name, params Declaration[] declarations) =>
        Namespace(name, declarations.ToImmutableList());
}
