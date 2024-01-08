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
    public static void Walk(Expression? expr, Func<Expression, bool> walkChildren, Action<Expression> action)
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

            case DeclarationExpression declaration:
                Walk(declaration.Initializer, walkChildren, action);
                break;

            case FunctionExpression function:
                Walk(function.Parameters, walkChildren, action);
                Walk(function.Body, walkChildren, action);
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
                throw new InvalidOperationException($"Unhandled expression kind '{expr.GetType().Name}' in Expression.Walk");
        }
    }

    /// <summary>
    /// Walks the 
    /// </summary>
    public static void Walk<T>(ImmutableList<T> expressions, Func<Expression, bool> walkChildren, Action<Expression> action)
        where T : Expression
    {
        foreach (var expr in expressions)
        {
            Walk(expr, walkChildren, action);
        }
    }

    public static ImmutableList<TExpr> Rewrite<TExpr>(this ImmutableList<TExpr> list, Func<TExpr, TExpr> rewriter)
        where TExpr : Expression =>
        Rewrite<TExpr, object?>(list, null, (e, a) => (rewriter(e), a)).list;

    public static (ImmutableList<TExpr> list, TArg final) Rewrite<TExpr, TArg>(
        this ImmutableList<TExpr> list, 
        TArg arg, 
        Func<TExpr, TArg, (TExpr expr, TArg arg)> rewriter)
        where TExpr : Expression
    {
        TExpr[] newList = null!;

        for (int i = 0; i < list.Count; i++)
        {
            var expr = list[i];
            var result = rewriter(expr, arg);
            (var newExpr, arg) = result;
            if (newExpr != expr)
            {
                if (newList == null)
                {
                    newList = new TExpr[list.Count];
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
            return (ImmutableList<TExpr>.Empty.AppendRange(newList), arg);

        return (list, arg);
    }
}
