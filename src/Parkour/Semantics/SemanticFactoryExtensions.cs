namespace Parkour.Semantics;

using Parkour;
using Symbols;

public static class SemanticFactoryExtensions
{
    #region Declarations

    public static PropertyDeclaration WithGetBody(
        this PropertyDeclaration property, 
        Expression body) 
        =>
        property.WithGetMethod(
            property.GetMethod != null
                ? property.GetMethod.WithBody(body)
                : SemanticFactory.Method(
                    "get_" + property.Name, 
                    parameters: [],
                    property.PropertyType, 
                    body, 
                    body.Location)
                    .WithAccess(property.Access)
                    .WithModifiers(property.Modifiers)
            );

    public static PropertyDeclaration WithGetAccess(
        this PropertyDeclaration property,
        Access access)
        =>
        property.WithGetMethod(
            property.GetMethod.WithAccess(access)
            );

    public static PropertyDeclaration WithSetBody(
        this PropertyDeclaration property,
        Expression body)
        =>
        property.WithSetMethod(
            property.SetMethod != null
                ? property.SetMethod.WithBody(body)
                : SemanticFactory.Method(
                    "set_" + property.Name,
                    [SemanticFactory.Parameter("value", property.PropertyType, body.Location)],
                    SemanticFactory.VoidType,
                    body,
                    body.Location)
                    .WithAccess(property.Access)
                    .WithModifiers(property.Modifiers)
            );

    public static PropertyDeclaration WithSetAccess(
        this PropertyDeclaration property,
        Access access)
        =>
        property.SetMethod != null
            ? property.WithSetMethod(property.SetMethod.WithAccess(access))
            : property;

    #endregion

    #region Expressions

    /// <summary>
    /// Converts the referenced symbol to an array of that element type.
    /// </summary>
    public static ArrayExpression Array(this Expression expression, ISourceLocation? location = null) =>
        SemanticFactory.Array(expression, location);

    /// <summary>
    /// Filter the referenced symbol(s) to only those with matching arity.
    /// </summary>
    public static ArityExpression WithArity(this Expression expression, int arity, ISourceLocation? location = null) =>
        SemanticFactory.Arity(expression, arity, location);

    /// <summary>
    /// Assign a source value to a target location.
    /// </summary>
    public static AssignExpression Assign(this Expression target, Expression expression, ISourceLocation? location = null) =>
        SemanticFactory.Assign(target, expression, location);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(this Expression target, ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        SemanticFactory.Call(target, arguments, location);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(this Expression target, IEnumerable<Expression> arguments, ISourceLocation? location = null) =>
        SemanticFactory.Call(target, arguments, location);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(this Expression target, params Expression[] arguments) =>
        SemanticFactory.Call(target, arguments);

    /// <summary>
    /// Invokes a delegate, lambda function or method.
    /// </summary>
    public static CallExpression Call(this Expression target, string name, params Expression[] arguments) =>
        SemanticFactory.Call(target.Member(name), arguments);

    /// <summary>
    /// Converts the referenced symbol to a constructed version of that symbol
    /// with the specified type arguments.
    /// </summary>
    public static ConstructExpression Construct(this Expression expression, ImmutableList<Expression> typeArguments) =>
        SemanticFactory.Construct(expression, typeArguments);

    /// <summary>
    /// Converts an expression to a specific type.
    /// </summary>
    public static ConvertExpression ConvertTo(this Expression expression, Expression convertedType, ISourceLocation? location = null) =>
        SemanticFactory.Convert(expression, convertedType, location);

    /// <summary>
    /// Tests the expression if it is an instance of the specified type.
    /// </summary>
    public static IsTypeExpression IsType(this Expression expression, Expression type, ISourceLocation? location = null) =>
        SemanticFactory.IsType(expression, type, location);

    /// <summary>
    /// Converts the expression to the specified type if it is an instance of that type or null if it is not.
    /// </summary>
    public static AsTypeExpression AsType(this Expression expression, Expression type, ISourceLocation? location = null) =>
        SemanticFactory.AsType(expression, type, location);

    /// <summary>
    /// Accesses the element of an expression
    /// </summary>
    public static ElementExpression Element(this Expression target, Expression index, ISourceLocation? location = null) =>
        SemanticFactory.Element(target, index, location);

    /// <summary>
    /// Accesses the element of an expression
    /// </summary>
    public static ElementExpression Element(this Expression target, ImmutableList<Expression> indices, ISourceLocation? location = null) =>
        SemanticFactory.Element(target, indices, location);

    /// <summary>
    /// Accesses the member of the expression's type or instance.
    /// </summary>
    public static MemberExpression Member(this Expression expression, string name, ISourceLocation? location = null) =>
        SemanticFactory.Member(expression, name, location);

    /// <summary>
    /// Creates a new instance of the type, by calling a constructor with the specified arguments.
    /// </summary>
    public static NewExpression New(this Expression typeExpression, ImmutableList<Expression> arguments, ISourceLocation? location = null) =>
        SemanticFactory.New(typeExpression, arguments, location);

    /// <summary>
    /// Creates a new instance of the type by calling the default constructor.
    /// </summary>
    public static NewExpression New(this Expression typeExpression, ISourceLocation? location = null) =>
        SemanticFactory.New(typeExpression, ImmutableList<Expression>.Empty, location);

    /// <summary>
    /// Creates a new array of the specified size.
    /// </summary>
    public static NewArrayExpression NewArray(this Expression elementType, Expression size, ISourceLocation? location = null) =>
        SemanticFactory.NewArray(elementType, size, location);

    /// <summary>
    /// Creates a new array with the specified values.
    /// </summary>
    public static NewArrayExpression NewArray(this Expression elementType, ImmutableList<Expression> values, ISourceLocation? location = null) =>
        SemanticFactory.NewArray(elementType, values, location);

    /// <summary>
    /// Creates a new multi-dimensional array with the specified dimension sizes.
    /// </summary>
    public static NewArrayExpression NewMultiDimensionalArray(this Expression elementType, ImmutableList<Expression> sizes, ISourceLocation? location = null) =>
        SemanticFactory.NewMultiDimensionalArray(elementType, sizes, location);

    #endregion
}
