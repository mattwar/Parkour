namespace Parkour.Expressions;

public static class ExpressionExtensions
{
    public static IReadOnlyList<TResult> SelectWhere<TResult>(this Expression root, Func<Expression, bool> walkChildren, Func<Expression, bool> predicate, Func<Expression, TResult> selector)
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

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this Expression root, Func<Expression, bool> predicate, Func<Expression, TResult> selector) =>
        SelectWhere(root, s => true, predicate, selector);

    public static IReadOnlyList<Expression> Where(this Expression root, Func<Expression, bool> walkChildren, Func<Expression, bool> predicate) =>
        SelectWhere(root, walkChildren, predicate, e => e);

    public static IReadOnlyList<Expression> Where(this Expression root, Func<Expression, bool> predicate) =>
        Where(root, s => true, predicate);

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this ImmutableList<Expression> expressions, Func<Expression, bool> walkChildren, Func<Expression, bool> predicate, Func<Expression, TResult> selector)
    {
        var list = new List<TResult>();

        Walk(
            expressions,
            walkChildren,
            action: e =>
            {
                if (predicate(e))
                    list.Add(selector(e));
            });

        return list;
    }

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this ImmutableList<Expression> expressions, Func<Expression, bool> predicate, Func<Expression, TResult> selector) =>
        SelectWhere(expressions, s => true, predicate, selector);

    public static IReadOnlyList<Expression> Where(this ImmutableList<Expression> expressions, Func<Expression, bool> walkChildren, Func<Expression, bool> predicate) =>
        SelectWhere(expressions, walkChildren, predicate, e => e);

    public static IReadOnlyList<Expression> Where(this ImmutableList<Expression> expressions, Func<Expression, bool> predicate) =>
        Where(expressions, s => true, predicate);

    /// <summary>
    /// Walks the expression tree calling the action callback for each expression
    /// including the root.
    /// </summary>
    public static void Walk(Expression expr, Func<Expression, bool> walkChildren, Action<Expression> action)
    {
        action(expr);

        if (!walkChildren(expr))
            return;

        switch (expr)
        {
            case BlockExpression block:
                Walk(block.Expressions, walkChildren, action);
                break;

            case BranchExpression branch:
                if (branch.Expression != null)
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

            case ConvertExpression convert:
                Walk(convert.Expression, walkChildren, action);
                break;

            case FunctionExpression function:
                Walk(function.Body, walkChildren, action);
                break;

            case DeclarationExpression declaration:
                Walk(declaration.Initializer, walkChildren, action);
                break;

            case PathExpression path:
                Walk(path.Expression, walkChildren, action);
                Walk(path.Reference, walkChildren, action);
                break;

            case ConstantExpression _:
            case ReferenceExpression _:
            case VoidExpression _:
                break;

            default:
                throw new InvalidOperationException($"Unhandled expression kind '{expr.GetType().Name}' in Expression.Walk");
        }
    }

    /// <summary>
    /// Walks the 
    /// </summary>
    public static void Walk(ImmutableList<Expression> expressions, Func<Expression, bool> walkChildren, Action<Expression> action)
    {
        foreach (var expr in expressions)
        {
            Walk(expr, walkChildren, action);
        }
    }

    public static ImmutableList<Expression> Rewrite(this ImmutableList<Expression> list, Func<Expression, Expression> rewriter) =>
        Rewrite<object?>(list, null, (e, a) => (rewriter(e), a)).list;

    public static (ImmutableList<Expression> list, TArg final) Rewrite<TArg>(
        this ImmutableList<Expression> list, 
        TArg arg, 
        Func<Expression, TArg, (Expression expr, TArg arg)> rewriter)
    {
        Expression[] newList = null!;

        for (int i = 0; i < list.Count; i++)
        {
            var expr = list[i];
            var result = rewriter(expr, arg);
            (var newExpr, arg) = result;
            if (newExpr != expr)
            {
                if (newList == null)
                {
                    newList = new Expression[list.Count];
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
            return (ImmutableList<Expression>.Empty.AppendRange(newList), arg);

        return (list, arg);
    }
}
