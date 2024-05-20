using System;

namespace Parkour.Semantics;

public static class SemanticExtensions
{ 
    /// <summary>
    /// Returns the list of values computed from the semantic elements in the tree 
    /// starting at the specified element that match the predicate.
    /// </summary>
    public static IReadOnlyList<TValue> SelectWhere<TValue>(
        this SemanticElement element, 
        Func<SemanticElement, bool> walkChildren, 
        Func<SemanticElement, bool> predicate,
        Func<SemanticElement, TValue> selector)
    {
        var list = new List<TValue>();

        Walk(
            element,
            walkChildren,
            action: e =>
            {
                if (predicate(e))
                    list.Add(selector(e));
            });

        return list;
    }

    /// <summary>
    /// Returns the list of values computed from the semantic elements in the tree 
    /// starting at the specified element that match the predicate.
    /// </summary>
    public static IReadOnlyList<TValue> SelectWhere<TValue>(
        this SemanticElement root, 
        Func<SemanticElement, bool> predicate, 
        Func<SemanticElement, TValue> selector)
    {
        return SelectWhere(root, s => true, predicate, selector);
    }

    /// <summary>
    /// Returns the list of <see cref="SemanticElement"/> in the tree starting at the specified element
    /// that match the predicate.
    /// </summary>
    public static IReadOnlyList<SemanticElement> Where(
        this SemanticElement root, 
        Func<SemanticElement, bool> walkChildren, 
        Func<SemanticElement, bool> predicate)
    {
        return SelectWhere(root, walkChildren, predicate, e => e);
    }

    /// <summary>
    /// Returns the list of <see cref="SemanticElement"/> in the tree starting at the specified element
    /// that match the predicate.
    /// </summary>
    public static IReadOnlyList<SemanticElement> Where(
        this SemanticElement root, 
        Func<SemanticElement, bool> predicate)
    {
        return Where(root, s => true, predicate);
    }

    /// <summary>
    /// Returns the list of values computed from the semantic elements in the trees 
    /// starting at the specified elements that match the predicate.
    /// </summary>
    public static IReadOnlyList<TValue> SelectWhere<TValue>(
        this ImmutableList<SemanticElement> elements, 
        Func<SemanticElement, bool> walkChildren, 
        Func<SemanticElement, bool> predicate, 
        Func<SemanticElement, TValue> selector)
    {
        var list = new List<TValue>();

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

    /// <summary>
    /// Returns the list of values computed from the semantic elements in the trees 
    /// starting at the specified elements that match the predicate.
    /// </summary>
    public static IReadOnlyList<TValue> SelectWhere<TValue>(
        this ImmutableList<SemanticElement> expressions, 
        Func<SemanticElement, bool> predicate, 
        Func<SemanticElement, TValue> selector)
    {
        return SelectWhere(expressions, s => true, predicate, selector);
    }

    /// <summary>
    /// Returns the list of <see cref="SemanticElement"/> in the trees starting at the specified elements 
    /// that match the predicate.
    /// </summary>
    public static IReadOnlyList<SemanticElement> Where(
        this ImmutableList<SemanticElement> expressions, 
        Func<SemanticElement, bool> walkChildren, 
        Func<SemanticElement, bool> predicate)
    {
        return SelectWhere(expressions, walkChildren, predicate, e => e);
    }

    /// <summary>
    /// Returns the list of <see cref="SemanticElement"/> in the trees starting at the specified elements 
    /// that match the predicate.
    /// </summary>
    public static IReadOnlyList<SemanticElement> Where(
        this ImmutableList<SemanticElement> expressions, 
        Func<SemanticElement, bool> predicate)
    {
        return Where(expressions, s => true, predicate);
    }

    /// <summary>
    /// Walks the semantic tree calling the action callback for each element including the root.
    /// </summary>
    public static void Walk(
        SemanticElement? element, 
        Func<SemanticElement, bool> walkChildren, 
        Action<SemanticElement> action)
    {
        if (element == null)
            return;

        action(element);

        if (!walkChildren(element))
            return;

        for (int i = 0, n = element.ChildCount; i < n; i++)
        {
            if (element.GetChild(i) is SemanticElement child)
                Walk(child, walkChildren, action);
        }
    }

    /// <summary>
    /// Returns the first element that matches the type and predicate.
    /// </summary>
    public static TElement? FirstDescendantOrSelf<TElement>(
        this SemanticElement? element,
        Func<SemanticElement, bool>? predicate = null,
        Func<SemanticElement, bool>? walkChildren = null
        )
        where TElement : SemanticElement
    {
        if (element == null)
            return null;

        if (element is TElement telem
            && (predicate == null || predicate(telem)))
            return telem;

        if (walkChildren != null && !walkChildren(element))
            return null;

        for (int i = 0, n = element.ChildCount; i < n; i++)
        {
            if (element.GetChild(i) is SemanticElement child)
            {
                var childFirst = FirstDescendantOrSelf<TElement>(child, predicate, walkChildren);
                if (childFirst != null)
                    return childFirst;
            }
        }

        return null;
    }

    /// <summary>
    /// Walks the semantic's sub-expressions recursively.
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

    /// <summary>
    /// Rewrites a list of semantic elements, 
    /// return the original list if no elements where changed.
    /// </summary>
    public static ImmutableList<TSemantic> Rewrite<TSemantic>(
        this ImmutableList<TSemantic> list, 
        Func<TSemantic, TSemantic?> rewriter)
        where TSemantic : SemanticElement
    {
        return Rewrite<TSemantic, object?>(list, null, (e, a) => (rewriter(e), a)).list;
    }

    /// <summary>
    /// Rewrites a list of semantic elements, 
    /// return the original list if no elements where changed.
    /// </summary>
    public static (ImmutableList<TSemantic> list, TArg final) Rewrite<TSemantic, TArg>(
        this ImmutableList<TSemantic> list, 
        TArg arg, 
        Func<TSemantic, TArg, (TSemantic? expr, TArg arg)> rewriter)
        where TSemantic : SemanticElement
    {
        List<TSemantic> newList = null!;

        for (int i = 0; i < list.Count; i++)
        {
            var expr = list[i];
            var result = rewriter(expr, arg);
            (var newExpr, arg) = result;

            if (newExpr != expr)
            {
                if (newExpr != null)
                {
                    if (newList == null)
                    {
                        newList = new List<TSemantic>(list.Count);
                        if (i > 0)
                            newList.AddRange(list.Take(i));
                    }
                    newList.Add(newExpr);
                }
            }
            else if (newList != null)
            {
                newList.Add(expr);
            }
        }

        if (newList != null)
            return (ImmutableList<TSemantic>.Empty.AppendRange(newList), arg);

        return (list, arg);
    }
}
