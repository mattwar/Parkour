namespace Parkour.Semantics;

public static class SemanticFactoryExtensions
{
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
    /// Converts the referenced symbol to a constructed version of that symbol
    /// with the specified type arguments.
    /// </summary>
    public static TypeArgumentsExpression WithTypeArguments(this Expression expression, ImmutableList<Expression> typeArguments) =>
        SemanticFactory.TypeArguments(expression, typeArguments);

    /// <summary>
    /// Converts an expression to a specific type.
    /// </summary>
    public static ConvertExpression ConvertTo(this Expression expression, Expression convertedType, ISourceLocation? location = null) =>
        SemanticFactory.Convert(expression, convertedType, location);

    /// <summary>
    /// Accesses the member of the expression's type or instance.
    /// </summary>
    public static MemberExpression Member(this Expression expression, string name, ISourceLocation? location = null) =>
        SemanticFactory.Member(expression, name, location);
}
