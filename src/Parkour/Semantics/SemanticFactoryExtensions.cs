namespace Parkour.Semantics;

public static class SemanticFactoryExtensions
{
    /// <summary>
    /// Filter the referenced symbol(s) to only those with matching arity.
    /// </summary>
    public static ArityExpression Arity(this Expression expression, int arity, ISourceLocation? location = null) =>
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
    /// Construct the referenced type or method with the specified type arguments.
    /// </summary>
    public static ConstructExpression Construct(this Expression expression, ImmutableList<Expression> typeArguments) =>
        SemanticFactory.Construct(expression, typeArguments);

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
