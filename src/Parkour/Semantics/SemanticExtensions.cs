namespace Parkour.Semantics;

public static class SemanticExtensions
{
    public static IReadOnlyList<TResult> SelectWhere<TResult>(this Semantic root, Func<Semantic, bool> walkChildren, Func<Semantic, bool> predicate, Func<Semantic, TResult> selector)
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

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this Semantic root, Func<Semantic, bool> predicate, Func<Semantic, TResult> selector) =>
        SelectWhere(root, s => true, predicate, selector);

    public static IReadOnlyList<Semantic> Where(this Semantic root, Func<Semantic, bool> walkChildren, Func<Semantic, bool> predicate) =>
        SelectWhere(root, walkChildren, predicate, e => e);

    public static IReadOnlyList<Semantic> Where(this Semantic root, Func<Semantic, bool> predicate) =>
        Where(root, s => true, predicate);

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this ImmutableList<Semantic> expressions, Func<Semantic, bool> walkChildren, Func<Semantic, bool> predicate, Func<Semantic, TResult> selector)
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

    public static IReadOnlyList<TResult> SelectWhere<TResult>(this ImmutableList<Semantic> expressions, Func<Semantic, bool> predicate, Func<Semantic, TResult> selector) =>
        SelectWhere(expressions, s => true, predicate, selector);

    public static IReadOnlyList<Semantic> Where(this ImmutableList<Semantic> expressions, Func<Semantic, bool> walkChildren, Func<Semantic, bool> predicate) =>
        SelectWhere(expressions, walkChildren, predicate, e => e);

    public static IReadOnlyList<Semantic> Where(this ImmutableList<Semantic> expressions, Func<Semantic, bool> predicate) =>
        Where(expressions, s => true, predicate);

    /// <summary>
    /// Walks the expression tree calling the action callback for each expression
    /// including the root.
    /// </summary>
    public static void Walk(Semantic expr, Func<Semantic, bool> walkChildren, Action<Semantic> action)
    {
        action(expr);

        if (!walkChildren(expr))
            return;

        switch (expr)
        {
            case Semantic.Block block:
                Walk(block.Expressions, walkChildren, action);
                break;

            case Semantic.Branch branch:
                if (branch.Expression != null)
                    Walk(branch.Expression, walkChildren, action);
                break;

            case Semantic.Call call:
                Walk(call.Expression, walkChildren, action);
                Walk(call.Arguments, walkChildren, action);
                break;

            case Semantic.Condition condition:
                Walk(condition.Test, walkChildren, action);
                Walk(condition.WhenTrue, walkChildren, action);
                Walk(condition.WhenFalse, walkChildren, action);
                break;

            case Semantic.Convert convert:
                Walk(convert.Expression, walkChildren, action);
                break;

            case Semantic.Function function:
                Walk(function.Body, walkChildren, action);
                break;

            case Semantic.Declaration declaration:
                Walk(declaration.Initializer, walkChildren, action);
                break;

            case Semantic.Path path:
                Walk(path.Expression, walkChildren, action);
                Walk(path.Reference, walkChildren, action);
                break;

            case Semantic.Constant _:
            case Semantic.Reference _:
            case Semantic.Void _:
                break;

            default:
                throw new InvalidOperationException($"Unhandled expression kind '{expr.GetType().Name}' in Expression.Walk");
        }
    }

    /// <summary>
    /// Walks the 
    /// </summary>
    public static void Walk(ImmutableList<Semantic> expressions, Func<Semantic, bool> walkChildren, Action<Semantic> action)
    {
        foreach (var expr in expressions)
        {
            Walk(expr, walkChildren, action);
        }
    }


    public static ImmutableList<Semantic> Rewrite(this ImmutableList<Semantic> list, Func<Semantic, Semantic> rewriter) =>
        Rewrite<object?>(list, null, (e, a) => (rewriter(e), a)).list;

    public static (ImmutableList<Semantic> list, TArg final) Rewrite<TArg>(
        this ImmutableList<Semantic> list, 
        TArg arg, 
        Func<Semantic, TArg, (Semantic expr, TArg arg)> rewriter)
    {
        Semantic[] newList = null!;

        for (int i = 0; i < list.Count; i++)
        {
            var expr = list[i];
            var result = rewriter(expr, arg);
            (var newExpr, arg) = result;
            if (newExpr != expr)
            {
                if (newList == null)
                {
                    newList = new Semantic[list.Count];
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
            return (ImmutableList<Semantic>.Empty.AppendRange(newList), arg);

        return (list, arg);
    }
}
