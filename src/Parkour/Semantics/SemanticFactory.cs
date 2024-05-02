namespace Parkour.Semantics;
using Binding;
using Symbols;

public static class SemanticFactory
{
    /// <summary>
    /// Converts the referenced symbol to an array of that type.
    /// </summary>
    public static ArrayExpression Array(Expression expression, ISourceLocation? location = null) =>
        new ArrayExpression(expression, location, null, null, null);

    /// <summary>
    /// Filter the referenced symbol(s) to only those with the specified arity.
    /// </summary>
    public static ArityExpression Arity(Expression expression, int arity, ISourceLocation? location = null) =>
        new ArityExpression(expression, arity, location, null, null, null);

    /// <summary>
    /// Assign a source value to a target location.
    /// </summary>
    public static AssignExpression Assign(Expression target, Expression expression, ISourceLocation? location = null) =>
        new AssignExpression(target, expression, location, null);

    /// <summary>
    /// Defines a block expression of multiple expressions. The final expression determines the block expression's result.
    /// </summary>
    public static BlockExpression Block(ImmutableList<Expression> expressions, ISourceLocation? location = null) =>
        new BlockExpression(expressions, location, null, null);

    /// <summary>
    /// Defines a block expression of multiple expressions. The final expression determines the block expression's result.
    /// </summary>
    public static BlockExpression Block(IEnumerable<Expression> expressions, ISourceLocation? location = null) =>
        Block(expressions.ToImmutableList(), location);

    /// <summary>
    /// Defines a block expression of multiple expressions. The final expression determines the block expression's result.
    /// </summary>
    public static BlockExpression Block(params Expression[] expressions) =>
        Block(expressions.ToImmutableList());

    /// <summary>
    /// Branches to a label.
    /// </summary>
    public static BranchExpression Branch(string labelName, Expression? expression = null, ISourceLocation? location = null) =>
        new BranchExpression(labelName, expression, location, null, null, null);

    /// <summary>
    /// Branches to the break location of a loop.
    /// </summary>
    public static BranchExpression Break(Expression? expression = null, ISourceLocation? location = null) =>
        BranchExpression.CreateBreak(expression, location, null, null);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(Expression target, ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        new CallExpression(target, arguments, location, null, null, null);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(Expression target, IEnumerable<Expression> arguments, ISourceLocation? location = null) =>
        Call(target, arguments.ToImmutableList(), location);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(Expression target, params Expression[] arguments) =>
        Call(target, arguments.ToImmutableList());

    /// <summary>
    /// Invokes a method.
    /// </summary>
    public static CallExpression Call(Expression instance, string name, params Expression[] arguments) =>
        Call(Member(instance, name), arguments.ToImmutableList());

    /// <summary>
    /// Invokes a method.
    /// </summary>
    public static CallExpression Call(string name, params Expression[] arguments) =>
        Call(Name(name), arguments);

    /// <summary>
    /// Access the element of an expression
    /// </summary>
    public static ElementExpression Element(Expression target, ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        new ElementExpression(target, arguments, location, null, null, null);

    /// <summary>
    /// Access the element of an expression
    /// </summary>
    public static ElementExpression Element(Expression target, Expression index, ISourceLocation? location = null) =>
        new ElementExpression(target, [index], location, null, null, null);

    /// <summary>
    /// Evaluates the whenTrue expression if the test expression results in true or otherwise evaluates the whenFalse expression.
    /// </summary>
    public static ConditionExpression Condition(Expression test, Expression whenTrue, Expression whenFalse, ISourceLocation? location = null) =>
        new ConditionExpression(test, whenTrue, whenFalse, location, null, null);

    /// <summary>
    /// Evaluates the whenTrue expression if the test expressions results in true.
    /// </summary>
    public static ConditionExpression Condition(Expression test, Expression whenTrue, ISourceLocation? location = null) =>
        new ConditionExpression(test, whenTrue, Void(location), location, null, null);

    /// <summary>
    /// Produces the constant value specified at runtime.
    /// </summary>
    public static ConstantExpression Constant(object? value, ISourceLocation? location = null) =>
        new ConstantExpression(value, location, null, null);

    /// <summary>
    /// Constructs the type or method with the specified type arguments.
    /// </summary>
    public static ConstructExpression Construct(Expression expression, ImmutableList<Expression> typeArguments, ISourceLocation? location = null) =>
        new ConstructExpression(expression, typeArguments, location, null, null, null);

    /// <summary>
    /// Branches to the loop's continue location.
    /// </summary>
    public static BranchExpression Continue(ISourceLocation? location = null) =>
        BranchExpression.CreateContinue(location, null, null);

    /// <summary>
    /// Converts an expression to a specific type.
    /// </summary>
    public static ConvertExpression Convert(Expression expression, Expression convertedType, ISourceLocation? location = null) =>
        new ConvertExpression(expression, convertedType, location, null, null, null);

    /// <summary>
    /// Declares a variable of a specific type and initializer.
    /// </summary>
    public static VariableExpression Variable(Expression variableType, string name, Expression initializer, ISourceLocation? location = null) =>
        new VariableExpression(name, variableType, initializer, location, null, null, null);

    /// <summary>
    /// Declares a variable of a specific type.
    /// </summary>
    public static VariableExpression Variable(Expression variableType, string name, ISourceLocation? location = null) =>
        new VariableExpression(name, variableType, null, location, null, null, null);

    /// <summary>
    /// Declares and initializes a variable.
    /// </summary>
    public static VariableExpression Variable(string name, Expression initializer, ISourceLocation? location = null) =>
        new VariableExpression(name, null, initializer, location, null, null, null);

    /// <summary>
    /// Produces the default value for the specified type.
    /// </summary>
    public static DefaultExpression Default(Expression type, ISourceLocation? location = null) =>
        new DefaultExpression(type, location, null, null);

    /// <summary>
    /// Produces the default value for the infered type.
    /// </summary>
    public static DefaultExpression Default(ISourceLocation? location = null) =>
        new DefaultExpression(null, location, null, null);

    /// <summary>
    /// Branches to the specified label.
    /// This is a synonym for <see cref="Branch(string, Expression?, ISourceLocation?)"/>
    /// </summary>
    public static BranchExpression Goto(string labelName, Expression? expression = null, ISourceLocation? location = null) =>
        new BranchExpression(labelName, expression, location, null, null, null);

    /// <summary>
    /// Evaluates the whenTrue expression if the test expressions results in true.
    /// This is a synonym for <see cref="Condition(Expression, Expression, Expression, ISourceLocation?)"/>
    /// </summary>
    public static ConditionExpression If(Expression test, Expression whenTrue, Expression whenFalse, ISourceLocation? location = null) =>
        Condition(test, whenTrue, whenFalse, location);

    /// <summary>
    /// Evaluates the whenTrue expression if the test expressions results in true.
    /// This is a synonym for <see cref="Condition(Expression, Expression, ISourceLocation?)"/>
    /// </summary>
    public static ConditionExpression If(Expression test, Expression whenTrue, ISourceLocation? location = null) =>
        Condition(test, whenTrue, location);

    /// <summary>
    /// A label for branch targets.
    /// </summary>
    public static LabelExpression Label(string name, Expression? recievingType = null, ISourceLocation? location = null) =>
        new LabelExpression(name, recievingType, location, null, null, null);

    /// <summary>
    /// Creates a lambda function.
    /// </summary>
    public static LambdaExpression Lambda(ImmutableList<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        new LambdaExpression("", parameters, body, location, null, null, null, null);

    /// <summary>
    /// Creates a lambda function.
    /// </summary>
    public static LambdaExpression Lambda(IEnumerable<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        Lambda(parameters.ToImmutableList(), body, location);

    /// <summary>
    /// Creates a lambda function.
    /// </summary>
    public static LambdaExpression Lambda(IEnumerable<string> parameterNames, Expression body, ISourceLocation? location = null) =>
        Lambda(parameterNames.Select(n => Parameter(n)), body, location);

    /// <summary>
    /// Creates a lambda function.
    /// </summary>
    public static LambdaExpression Lambda(Expression body, ISourceLocation? location = null) =>
        Lambda(ImmutableList<ParameterDeclaration>.Empty, body, location);

    /// <summary>
    /// A loop that continues to repeat the body until a break exits the loop.
    /// </summary>
    public static LoopExpression Loop(Expression body, ISourceLocation? location = null) =>
        new LoopExpression(body, location, null, null, null, null);

    /// <summary>
    /// Accesses the member of the expression.
    /// </summary>
    public static MemberExpression Member(Expression expression, string name, ISourceLocation? location = null) =>
        new MemberExpression(expression, name, location, null, null, null);

    /// <summary>
    /// References a named symbol in scope.
    /// </summary>
    public static NameExpression Name(string name, ISourceLocation? location = null) =>
        new NameExpression(name, location, null, null, null);

    /// <summary>
    /// Creates an new instance of the specfied type.
    /// </summary>
    public static NewExpression New(Expression typeExpression, ImmutableList<Expression>? arguments = null, ISourceLocation? location = null) =>
        new NewExpression(typeExpression, arguments, location, null, null, null);

    /// <summary>
    /// Creates an new instance of the infered type.
    /// </summary>
    public static NewExpression New(ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        new NewExpression(null, arguments, location, null, null, null);

    /// <summary>
    /// Creates an new instance of the infered type.
    /// </summary>
    public static NewExpression New(ISourceLocation? location = null) =>
        new NewExpression(null, null, location, null, null, null);

    /// <summary>
    /// Creates a new array instance with the specified values.
    /// </summary>
    public static NewArrayInitExpression NewArray(Expression elementType, ImmutableList<Expression> values, ISourceLocation? location = null) =>
        new NewArrayInitExpression(elementType, values, location, null, null, null);

    /// <summary>
    /// Creates a new array instance with the specified values, with inferred element type.
    /// </summary>
    public static NewArrayInitExpression NewArray(ImmutableList<Expression> values, ISourceLocation? location = null) =>
        new NewArrayInitExpression(null, values, location, null, null, null);

    /// <summary>
    /// Creates a new array instance of the specified size.
    /// </summary>
    public static NewArraySizeExpression NewArray(Expression elementType, Expression size, ISourceLocation? location = null) =>
        new NewArraySizeExpression(elementType, size, location, null, null, null);

    /// <summary>
    /// Creates a new array instance of the specified size, with inferred element type.
    /// </summary>
    public static NewArraySizeExpression NewArray(Expression size, ISourceLocation? location = null) =>
        new NewArraySizeExpression(null, size, location, null, null, null);

    /// <summary>
    /// Invokes an intrinsic operator.
    /// </summary>
    public static OperatorExpression Operator(string name, ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        new OperatorExpression(name, arguments, location, null, null, null);


    /// <summary>
    /// Return from a method or lambda.
    /// </summary>
    public static BranchExpression Return(Expression? expression = null, ISourceLocation? location = null) =>
        BranchExpression.CreateReturn(expression, location, null, null);

    /// <summary>
    /// Reference a declared symbol.
    /// </summary>
    public static SymbolExpression Symbol(MemberSymbol symbol, ISourceLocation? location = null) =>
        Symbol(symbol.FullName, location);

    /// <summary>
    /// Reference a declared symbol by its full name; ignores scoping rules.
    /// </summary>
    public static SymbolExpression Symbol(string fullName, ISourceLocation? location = null) =>
        new SymbolExpression(fullName, location, null, null, null);

    /// <summary>
    /// References the current instance.
    /// </summary>
    public static ThisExpression This(ISourceLocation? location = null) =>
        new ThisExpression(location, null, null);

    /// <summary>
    /// An expression that does nothing and returns nothing.
    /// </summary>
    public static VoidExpression Void(ISourceLocation? location = null) => 
        location == null 
            ? VoidExpression.Default
            : new VoidExpression(location);


    /// <summary>
    /// A loop that continues to repeat the body until the test fails or a break exists the loop.
    /// </summary>
    public static Expression While(Expression test, Expression body, ISourceLocation? location = null) =>
        Loop(If(test, body, Break()));

    /// <summary>
    /// A loop that iterates a variable over a range
    /// </summary>
    public static Expression For(string name, Expression start, Expression end, Expression body, ISourceLocation? location = null) =>
        For(name, start, end, body, Constant(1), location);

    /// <summary>
    /// A loop that iterates a variable over a range
    /// </summary>
    public static Expression For(string name, Expression start, Expression end, Expression increment, Expression body, ISourceLocation? location = null) =>
        Block(
            Variable(name, start),
            Loop(
                If(LessThan(Name(name), end),
                    Block(
                        body,
                        Assign(Name(name), Add(Name(name), increment))),
                    Break())
                ),
            Name(name));

    /// <summary>
    /// Applies the Add operator to two values.
    /// </summary>
    public static OperatorExpression Add(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.Add, [left, right], location);

    /// <summary>
    /// Applies the Subtract operator to two values.
    /// </summary>
    public static OperatorExpression Subtract(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.Subtract, [left, right], location);

    /// <summary>
    /// Applies the Multiply operator to two values.
    /// </summary>
    public static OperatorExpression Multiply(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.Multiply, [left, right], location);

    /// <summary>
    /// Applies the Divide operator to two values.
    /// </summary>
    public static OperatorExpression Divide(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.Divide, [left, right], location);

    /// <summary>
    /// Applies the Remainder operator to two values.
    /// </summary>
    public static OperatorExpression Remainder(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.Remainder, [left, right], location);

    /// <summary>
    /// Applies the Negate operator to a value.
    /// </summary>
    public static OperatorExpression Negate(Expression operand, ISourceLocation? location = null) =>
        Operator(OperatorKind.Negate, [operand], location);

    public static OperatorExpression BitwiseAnd(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.BitwiseAnd, [left, right], location);

    public static OperatorExpression BitwiseOr(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.BitwiseOr, [left, right], location);

    public static OperatorExpression BitwiseXor(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.BitwiseXor, [left, right], location);

    public static OperatorExpression BitwiseNot(Expression operand, ISourceLocation? location = null) =>
        Operator(OperatorKind.BitwiseNot, [operand], location);

    public static OperatorExpression LogicalAnd(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.LogicalAnd, [left, right], location);

    public static OperatorExpression LogicalOr(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.LogicalOr, [left, right], location);

    public static OperatorExpression LogicalXor(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.BitwiseXor, [left, right], location);

    public static OperatorExpression LogicalNot(Expression operand, ISourceLocation? location = null) =>
        Operator(OperatorKind.LogicalNot, [operand], location);

    public static OperatorExpression LogicalAndAlso(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.LogicalAndAlso, [left, right], location);

    public static OperatorExpression LogicalOrElse(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.LogicalOrElse, [left, right], location);

    public static OperatorExpression And(Expression left, Expression right, ISourceLocation? location = null) =>
        LogicalAnd(left, right, location);

    public static OperatorExpression Or(Expression left, Expression right, ISourceLocation? location = null) =>
        LogicalOr(left, right, location);

    public static OperatorExpression AndAlso(Expression left, Expression right, ISourceLocation? location = null) =>
        LogicalAndAlso(left, right, location);

    public static OperatorExpression OrElse(Expression left, Expression right, ISourceLocation? location = null) =>
        LogicalOrElse(left, right, location);

    public static OperatorExpression Not(Expression operand, ISourceLocation? location = null) =>
        LogicalNot(operand, location);

    public static OperatorExpression ShiftLeft(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.ShiftLeft, [left, right], location);

    public static OperatorExpression ShiftRight(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.ShiftRight, [left, right], location);

    public static OperatorExpression Equal(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.Equal, [left, right], location);

    public static OperatorExpression NotEqual(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.NotEqual, [left, right], location);

    public static OperatorExpression LessThan(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.LessThan, [left, right], location);

    public static OperatorExpression LessThanOrEqual(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.LessThanOrEqual, [left, right], location);

    public static OperatorExpression GreaterThan(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.GreaterThan, [left, right], location);

    public static OperatorExpression GreaterThanOrEqual(Expression left, Expression right, ISourceLocation? location = null) =>
        Operator(OperatorKind.GreaterThanOrEqual, [left, right], location);

    public static ParameterDeclaration Parameter(string name, Expression? parameterType = null, ISourceLocation? location = null) =>
        new ParameterDeclaration(name, parameterType, location, null, null);

    public static MethodDeclaration Method(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<TypeParameterDeclaration> typeParameters, ImmutableList<ParameterDeclaration> parameters, Expression returnType, Expression body, ISourceLocation? location = null) =>
        new MethodDeclaration(name, access, modifiers, typeParameters, parameters, returnType, body, location, null, null, null);

    public static MethodDeclaration Method(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression returnType, Expression body, ISourceLocation? location = null) =>
        Method(name, access, modifiers, ImmutableList<TypeParameterDeclaration>.Empty, parameters, returnType, body, location);

    public static MethodDeclaration Method(string name, ImmutableList<ParameterDeclaration> parameters, Expression returnType, Expression body, ISourceLocation? location = null) =>
        Method(name, SymbolAccess.Public, SymbolModifier.None, parameters, returnType, body, location);

    public static ConstructorDeclaration Constructor(SymbolAccess access, SymbolModifier modifiers, ImmutableList<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        new ConstructorDeclaration(access, modifiers, parameters, body, location, null, null, null);

    public static ConstructorDeclaration Constructor(ImmutableList<ParameterDeclaration> parameters, Expression body, ISourceLocation? location = null) =>
        Constructor(SymbolAccess.Public, SymbolModifier.None, parameters, body, location);

    public static ConstructorDeclaration Constructor(Expression body, ISourceLocation? location = null) =>
        Constructor(SymbolAccess.Public, SymbolModifier.None, [], body, location);

    public static ConstructorDeclaration Constructor(ISourceLocation? location = null) =>
        Constructor(SymbolAccess.Public, SymbolModifier.None, [], Block(), location);

    public static FieldDeclaration Field(string name, SymbolAccess access, SymbolModifier modifiers, Expression fieldType, Expression? initalizer = null, ISourceLocation? location = null) =>
        new FieldDeclaration(name, access, modifiers, fieldType, initalizer, location, null, null);

    public static FieldDeclaration Field(string name, Expression fieldType, Expression? initalizer = null, ISourceLocation? location = null) =>
        Field(name, SymbolAccess.Public, SymbolModifier.None, fieldType, initalizer, location);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, MethodDeclaration getMethod, MethodDeclaration? setMethod, FieldDeclaration? backingField, Expression propertyType, ISourceLocation? location = null) =>
        new PropertyDeclaration(name, access, modifiers, propertyType, backingField, getMethod, setMethod, location, null, null);

    public static PropertyDeclaration Property(string name, MethodDeclaration getMethod, MethodDeclaration? setMethod = null, ISourceLocation? location = null) =>
        Property(name, getMethod.Access, getMethod.Modifiers, getMethod, setMethod, null, getMethod.ReturnType, location);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, Expression expression, ISourceLocation? location = null) =>
        Property(
            name, 
            Method("get_" + name, access, modifiers | SymbolModifier.HideBySig | SymbolModifier.Special, ImmutableList<ParameterDeclaration>.Empty, expression, propertyType, location), null, location);

    public static PropertyDeclaration Property(string name, SymbolAccess access, SymbolModifier modifiers, Expression propertyType, ISourceLocation? location = null)
    {
        var fieldName = $"__{name}_backingField";
        return Property(
            name,
            access,
            modifiers,
            Method("get_" + name, access, modifiers | SymbolModifier.HideBySig | SymbolModifier.Special, ImmutableList<ParameterDeclaration>.Empty, propertyType, Name(fieldName)),
            Method("set_" + name, access, modifiers | SymbolModifier.HideBySig | SymbolModifier.Special, [Parameter("value", propertyType)], Void(), Assign(Name(fieldName), Name("value"))),
            Field(fieldName, SymbolAccess.Private, SymbolModifier.None, propertyType, null),
            propertyType,
            location);
    }

    public static PropertyDeclaration Property(string name, Expression propertyType, ISourceLocation? location = null) =>
        Property(name, SymbolAccess.Public, SymbolModifier.None, propertyType, location);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<TypeParameterDeclaration> typeParameters, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        new ClassDeclaration(name, access, modifiers, typeParameters, baseTypes, declarations, location, null, null);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        Class(name, access, modifiers, ImmutableList<TypeParameterDeclaration>.Empty, baseTypes, declarations, location);

    public static ClassDeclaration Class(string name, SymbolAccess access, SymbolModifier modifiers, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        Class(name, access, modifiers, ImmutableList<Expression>.Empty, declarations, location);

    public static ClassDeclaration Class(string name, ImmutableList<TypeParameterDeclaration> typeParameters, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        Class(name, SymbolAccess.Public, SymbolModifier.None, typeParameters, baseTypes, declarations, location);

    public static ClassDeclaration Class(string name, ImmutableList<Expression> baseTypes, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        Class(name, ImmutableList<TypeParameterDeclaration>.Empty, baseTypes, declarations, location);

    public static ClassDeclaration Class(string name, ImmutableList<Declaration> declarations) =>
        Class(name, ImmutableList<Expression>.Empty, declarations);

    public static NamespaceDeclaration Namespace(string name, ImmutableList<Declaration> declarations, ISourceLocation? location = null) =>
        new NamespaceDeclaration(name, declarations, location, null, null);

    public static TypeParameterDeclaration TypeParameter(string name, ISourceLocation? location = null) =>
        new TypeParameterDeclaration(name, location, null, null);

    public static UsingDeclaration Using(string name, Expression @namespace, ISourceLocation? location = null) =>
        new UsingDeclaration(name, @namespace, location, null, null);

    public static UsingDeclaration Using(Expression @namespace, ISourceLocation? location = null) =>
        new UsingDeclaration("", @namespace, location, null, null);
}
