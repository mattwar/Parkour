namespace Parkour.Semantics;
using Binding;
using Symbols;
using Syntax;

public static class SemanticFactory
{
    public static AssignExpression Assign(Expression target, Expression expression, ISourceLocation? location = null) =>
        new AssignExpression(target, expression, location, null);

    public static BlockExpression Block(ImmutableList<Expression> expressions, ISourceLocation? location = null) =>
        new BlockExpression(expressions, location, null, null);

    public static BlockExpression Block(IEnumerable<Expression> expressions, ISourceLocation? location = null) =>
        Block(expressions.ToImmutableList(), location);

    public static BlockExpression Block(params Expression[] expressions) =>
        Block(expressions.ToImmutableList());

    public static BranchExpression Branch(string labelName, Expression? expression = null, ISourceLocation? location = null) =>
        new BranchExpression(labelName, expression, location, null, null, null);

    public static BranchExpression Break(Expression? expression = null, ISourceLocation? location = null) =>
        BranchExpression.CreateBreak(expression, location, null, null);

    public static CallExpression Call(Expression target, ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        new CallExpression(target, arguments, location, null, null, null);

    public static CallExpression Call(Expression target, IEnumerable<Expression> arguments, ISourceLocation? location = null) =>
        Call(target, arguments.ToImmutableList(), location);

    public static CallExpression Call(Expression target, params Expression[] arguments) =>
        Call(target, arguments.ToImmutableList());

    public static ConditionExpression Condition(Expression test, Expression whenTrue, Expression whenFalse, ISourceLocation? location = null) =>
        new ConditionExpression(test, whenTrue, whenFalse, location, null, null);

    public static ConditionExpression Condition(Expression test, Expression whenTrue, ISourceLocation? location = null) =>
        new ConditionExpression(test, whenTrue, Void(location), location, null, null);

    public static ConstantExpression Constant(object? value, ISourceLocation? location = null) =>
        new ConstantExpression(value, location, null, null);

    public static BranchExpression Continue(ISourceLocation? location = null) =>
        BranchExpression.CreateContinue(location, null, null);

    public static ConvertExpression Convert(ConversionKind kind, Expression expression, Expression convertedType, ISourceLocation? location = null) =>
        new ConvertExpression(kind, expression, convertedType, location, null, null, null);

    public static ConvertExpression Convert(Expression expression, Expression convertedType, ISourceLocation? location = null) =>
        Convert(ConversionKind.Narrowing, expression, convertedType, location);

    public static VariableExpression Variable(Expression? variableType, string name, Expression? initializer, ISourceLocation? location = null) =>
        new VariableExpression(name, variableType, initializer, location, null, null, null);

    public static VariableExpression Variable(Expression? variableType, string name, ISourceLocation? location = null) =>
        Variable(variableType, name, null, location);

    public static VariableExpression Variable(string name, Expression? initializer, ISourceLocation? location = null) =>
        Variable(null, name, initializer, location);

    public static DefaultExpression Default(Expression? type, ISourceLocation? location = null) =>
        new DefaultExpression(type, location, null, null);

    public static DefaultExpression Default(ISourceLocation? location = null) =>
        Default(null, location);

    /// <summary>
    /// Synonym for <see cref="Branch(string, Expression?, ISourceLocation?)"/>
    /// </summary>
    public static BranchExpression Goto(string labelName, Expression? expression = null, ISourceLocation? location = null) =>
        new BranchExpression(labelName, expression, location, null, null, null);

    /// <summary>
    /// Synonym for <see cref="Condition(Expression, Expression, Expression, ISourceLocation?)"/>
    /// </summary>
    public static ConditionExpression If(Expression test, Expression whenTrue, Expression whenFalse, ISourceLocation? location = null) =>
        Condition(test, whenTrue, whenFalse, location);

    /// <summary>
    /// Synonym for <see cref="Condition(Expression, Expression, ISourceLocation?)"/>
    /// </summary>
    public static ConditionExpression If(Expression test, Expression whenTrue, ISourceLocation? location = null) =>
        Condition(test, whenTrue, location);

    public static PathExpression Path(Expression expression, ReferenceExpression reference, ISourceLocation? location = null) =>
        new PathExpression(expression, reference, location, null);

    public static PathExpression Path(Expression expression, string name, ISourceLocation? location = null) =>
        Path(expression, Name(name), location);

    public static LabelExpression Label(string name, Expression? recievingType = null, ISourceLocation? location = null) =>
        new LabelExpression(name, recievingType, location, null, null, null);

    public static LambdaExpression Lambda(ImmutableList<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        new LambdaExpression("", parameters, body, location, null, null, null, null);

    public static LambdaExpression Lambda(IEnumerable<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        Lambda(parameters.ToImmutableList(), body, location);

    public static LambdaExpression Lambda(IEnumerable<string> parameterNames, Expression body, ISourceLocation? location = null) =>
        Lambda(parameterNames.Select(n => Parameter(n)), body, location);

    public static LambdaExpression Lambda(Expression body, ISourceLocation? location = null) =>
        Lambda(ImmutableList<ParameterDeclaration>.Empty, body, location);

    public static OperatorExpression Operator(string name, ISourceLocation? location = null) =>
        new OperatorExpression(name, location, null, null, null);

    public static ReferenceExpression Reference(string name, ISourceLocation? location = null) =>
        new ReferenceExpression(name, location, null, null, null);

    public static ReferenceExpression Name(string name, ISourceLocation? location = null) =>
        Reference(name, location);

    public static ReferenceExpression Type(TypeSymbol symbol, ISourceLocation? location = null) =>
        Reference(symbol.FullName, location);

    public static BranchExpression Return(Expression? expression = null, ISourceLocation? location = null) =>
        BranchExpression.CreateReturn(expression, location, null, null);

    public static VoidExpression Void(ISourceLocation? location = null) => 
        location == null 
            ? VoidExpression.Default
            : new VoidExpression(location);

    public static LoopExpression Loop(Expression body, ISourceLocation? location = null) =>
        new LoopExpression(body, location, null, null, null, null);

    public static LoopExpression While(Expression test, Expression body, ISourceLocation? location = null) =>
        Loop(Condition(test, body, Break()));

    public static CallExpression Add(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Add), [left, right], location);

    public static CallExpression Subtract(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Subtract), [left, right], location);

    public static CallExpression Multiply(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Multiply), [left, right], location);

    public static CallExpression Divide(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Divide), [left, right], location);

    public static CallExpression Remainder(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Remainder), [left, right], location);

    public static CallExpression Negate(Expression operand, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Negate), [operand], location);

    public static CallExpression BitwiseAnd(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.BitwiseAnd), [left, right], location);

    public static CallExpression BitwiseOr(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.BitwiseOr), [left, right], location);

    public static CallExpression BitwiseXor(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.BitwiseXor), [left, right], location);

    public static CallExpression BitwiseNot(Expression operand, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.BitwiseNot), [operand], location);

    public static CallExpression ShiftLeft(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.ShiftLeft), [left, right], location);

    public static CallExpression ShiftRight(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.ShiftRight), [left, right], location);

    public static CallExpression Equal(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.Equal), [left, right], location);

    public static CallExpression NotEqual(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.NotEqual), [left, right], location);

    public static CallExpression LessThan(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LessThan), [left, right], location);

    public static CallExpression LessThanOrEqual(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LessThanOrEqual), [left, right], location);

    public static CallExpression GreaterThan(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.GreaterThan), [left, right], location);

    public static CallExpression GreaterThanOrEqual(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.GreaterThanOrEqual), [left, right], location);

    public static CallExpression And(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LogicalAnd), [left, right], location);

    public static CallExpression AndAlso(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LogicalAndAlso), [left, right], location);

    public static CallExpression Or(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LogicalOr), [left, right], location);

    public static CallExpression OrElse(Expression left, Expression right, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LogicalOrElse), [left, right], location);

    public static CallExpression Not(Expression operand, ISourceLocation? location = null) =>
        Call(Operator(OperatorKinds.LogicalNot), [operand], location);

    public static ParameterDeclaration Parameter(string name, Expression? parameterType = null, ISourceLocation? location = null) =>
        new ParameterDeclaration(name, parameterType, location, null, null);

    public static MethodDeclaration Method(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression returnType, Expression body, ISourceLocation? location = null) =>
        new MethodDeclaration(name, access, modifiers, parameters, body, returnType, location, null, null);

    public static MethodDeclaration Method(string name, ImmutableList<ParameterDeclaration> parameters, Expression returnType, Expression body, ISourceLocation? location = null) =>
        Method(name, SymbolAccess.Public, SymbolModifier.None, parameters, returnType, body, location);

    public static ConstructorDeclaration Constructor(SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        new ConstructorDeclaration(access, modifiers, parameters, body, location, null, null);

    public static ConstructorDeclaration Constructor(ImmutableList<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        Constructor(SymbolAccess.Public, SymbolModifier.None, parameters, body, location);

    public static FieldDeclaration Field(string name, SymbolAccess access, SymbolModifier modifiers, Expression fieldType, Expression? initalizer = null, ISourceLocation? location = null) =>
        new FieldDeclaration(name, access, modifiers, fieldType, initalizer, location, null, null);

    public static FieldDeclaration Field(string name, Expression fieldType, Expression? initalizer = null, ISourceLocation? location = null) =>
        Field(name, SymbolAccess.Public, SymbolModifier.None, fieldType, initalizer, location);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, MethodDeclaration getMethod, MethodDeclaration? setMethod, FieldDeclaration? backingField, Expression propertyType, ISourceLocation? location = null) =>
        new PropertyDeclaration(name, access, modifiers, propertyType, backingField, getMethod, setMethod, location, null, null);

    public static PropertyDeclaration Property(string name, MethodDeclaration getMethod, MethodDeclaration? setMethod = null, ISourceLocation? location = null) =>
        Property(name, getMethod.Access, getMethod.Modifiers, getMethod, setMethod, null, getMethod.ReturnType, location);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, Expression expression, ISourceLocation? location = null) =>
        Property(name, Method("get_" + name, access, modifiers, ImmutableList<ParameterDeclaration>.Empty, expression, propertyType, location), null, location);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, ISourceLocation? location = null) =>
        Property(name,
            access,
            modifiers,
            Method("get_" + name, access, modifiers, ImmutableList<ParameterDeclaration>.Empty, propertyType, Name("field")),
            Method("set_" + name, access, modifiers, [Parameter("value", propertyType)], Void(), Assign(Name("field"), Name("value"))),
            Field("field", SymbolAccess.Private, SymbolModifier.None, propertyType, null),
            propertyType,
            location);

    public static PropertyDeclaration Property(string name, Expression propertyType, ISourceLocation? location = null) =>
        Property(name, SymbolAccess.Public, SymbolModifier.None, propertyType, location);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        new ClassDeclaration(name, access, modifiers, baseTypes, declarations, location, null, null);

    public static ClassDeclaration Class(string name, params Declaration[] declarations) =>
        Class(name, SymbolAccess.Public, SymbolModifier.None, ImmutableList<Expression>.Empty, declarations.ToImmutableList());

    public static NamespaceDeclaration Namespace(string name, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        new NamespaceDeclaration(name, declarations, location, null, null);

    public static NamespaceDeclaration Namespace(string name, params Declaration[] declarations) =>
        Namespace(name, declarations.ToImmutableList());
}
