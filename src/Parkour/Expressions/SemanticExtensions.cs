namespace Parkour.Expressions;

public static class SemanticExtensions
{
    public static IReadOnlyList<TResult> SelectWhere<TResult>(
        this SemanticElement root, 
        Func<SemanticElement, bool> walkChildren, 
        Func<SemanticElement, bool> predicate,
        Func<SemanticElement, TResult> selector)
    {
        var list = new List<TResult>();

        Walk(
            root,
            walkChildren,
            action: e =>
            {
                if (predicate(e))
                    list.Add(selector(e));
            });

        return list;
    }

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this SemanticElement root, Func<SemanticElement, bool> predicate, Func<SemanticElement, TResult> selector) =>
        SelectWhere(root, s => true, predicate, selector);

    public static IReadOnlyList<SemanticElement> Where(this SemanticElement root, Func<SemanticElement, bool> walkChildren, Func<SemanticElement, bool> predicate) =>
        SelectWhere(root, walkChildren, predicate, e => e);

    public static IReadOnlyList<SemanticElement> Where(this SemanticElement root, Func<SemanticElement, bool> predicate) =>
        Where(root, s => true, predicate);

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this ImmutableList<SemanticElement> elements, Func<SemanticElement, bool> walkChildren, Func<SemanticElement, bool> predicate, Func<SemanticElement, TResult> selector)
    {
        var list = new List<TResult>();

        Walk(
            elements,
            walkChildren,
            action: e =>
            {
                if (predicate(e))
                    list.Add(selector(e));
            });

        return list;
    }

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this ImmutableList<SemanticElement> expressions, Func<SemanticElement, bool> predicate, Func<SemanticElement, TResult> selector) =>
        SelectWhere(expressions, s => true, predicate, selector);

    public static IReadOnlyList<SemanticElement> Where(this ImmutableList<SemanticElement> expressions, Func<SemanticElement, bool> walkChildren, Func<SemanticElement, bool> predicate) =>
        SelectWhere(expressions, walkChildren, predicate, e => e);

    public static IReadOnlyList<SemanticElement> Where(this ImmutableList<SemanticElement> expressions, Func<SemanticElement, bool> predicate) =>
        Where(expressions, s => true, predicate);

    /// <summary>
    /// Walks the expression tree calling the action callback for each expression
    /// including the root.
    /// </summary>
    public static void Walk(SemanticElement? expr, Func<SemanticElement, bool> walkChildren, Action<SemanticElement> action)
    {
        if (expr == null)
            return;

        action(expr);

        if (!walkChildren(expr))
            return;

        switch (expr)
        {
            case BlockExpression block:
                Walk(block.Expressions, walkChildren, action);
                break;

            case BranchExpression branch:
                Walk(branch.Expression, walkChildren, action);
                break;

            case CallExpression call:
                Walk(call.Expression, walkChildren, action);
                Walk(call.Arguments, walkChildren, action);
                break;

            case ConditionExpression condition:
                Walk(condition.Test, walkChildren, action);
                Walk(condition.WhenTrue, walkChildren, action);
                Walk(condition.WhenFalse, walkChildren, action);
                break;

            case ConstructorDeclaration constructor:
                Walk(constructor.Parameters, walkChildren, action);
                Walk(constructor.Body, walkChildren, action);
                break;

            case ConvertExpression convert:
                Walk(convert.Expression, walkChildren, action);
                break;

            case DeclarationExpression declaration:
                Walk(declaration.Initializer, walkChildren, action);
                break;

            case LambdaExpression lambda:
                Walk(lambda.Parameters, walkChildren, action);
                Walk(lambda.Body, walkChildren, action);
                break;

            case PathExpression path:
                Walk(path.Expression, walkChildren, action);
                Walk(path.Reference, walkChildren, action);
                break;

            case ClassDeclaration cd:
                Walk(cd.BaseTypes, walkChildren, action);
                Walk(cd.Declarations, walkChildren, action);
                break;

            case MethodDeclaration md:
                Walk(md.Parameters, walkChildren, action);
                Walk(md.Body, walkChildren, action);
                Walk(md.ReturnType, walkChildren, action);
                break;

            case ParameterDeclaration pd:
                Walk(pd.ParameterType, walkChildren, action);
                break;

            case FieldDeclaration fd:
                Walk(fd.FieldType, walkChildren, action);
                break;

            case PropertyDeclaration prd:
                Walk(prd.PropertyType, walkChildren, action);
                Walk(prd.GetMethod, walkChildren, action);
                Walk(prd.SetMethod, walkChildren, action);
                break;

            case OperatorExpression _:
            case ConstantExpression _:
            case ReferenceExpression _:
            case VoidExpression _:
            case LabelExpression _:
                break;

            default:
                throw new InvalidOperationException($"Unhandled expression kind '{expr.GetType().Name}' in Semantic.Walk");
        }
    }

    /// <summary>
    /// Walks the Semantic's sub-expressions recursively.
    /// </summary>
    public static void Walk<TSemantic>(
        ImmutableList<TSemantic> expressions, 
        Func<SemanticElement, bool> walkChildren, 
        Action<SemanticElement> action)
        where TSemantic : SemanticElement
    {
        foreach (var expr in expressions)
        {
            Walk(expr, walkChildren, action);
        }
    }

    public static ImmutableList<TSemantic> Rewrite<TSemantic>(
        this ImmutableList<TSemantic> list, 
        Func<TSemantic, TSemantic> rewriter)
        where TSemantic : SemanticElement =>
        Rewrite<TSemantic, object?>(list, null, (e, a) => (rewriter(e), a)).list;

    public static (ImmutableList<TSemantic> list, TArg final) Rewrite<TSemantic, TArg>(
        this ImmutableList<TSemantic> list, 
        TArg arg, 
        Func<TSemantic, TArg, (TSemantic expr, TArg arg)> rewriter)
        where TSemantic : SemanticElement
    {
        TSemantic[] newList = null!;

        for (int i = 0; i < list.Count; i++)
        {
            var expr = list[i];
            var result = rewriter(expr, arg);
            (var newExpr, arg) = result;
            if (newExpr != expr)
            {
                if (newList == null)
                {
                    newList = new TSemantic[list.Count];
                    if (i > 0)
                        list.CopyTo(0, newList.AsSpan().Slice(0, i + 1));
                }
                newList[i] = newExpr;
            }
            else if (newList != null)
            {
                newList[i] = expr;
            }
        }

        if (newList != null)
            return (ImmutableList<TSemantic>.Empty.AppendRange(newList), arg);

        return (list, arg);
    }
}
