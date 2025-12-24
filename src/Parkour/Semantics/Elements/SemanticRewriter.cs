namespace Parkour.Semantics;

/// <summary>
/// Rewrites trees of <see cref="SemanticElement"/>
/// </summary>
public abstract class SemanticRewriter
{
    /// <summary>
    /// Rewrites the element.
    /// </summary>
    public virtual TElement? Rewrite<TElement>(TElement? element)
        where TElement : SemanticElement
    {
        if (element == null)
            return null;
        var current = element.RewriteChildren(this);
        return (TElement)Rewrite(current, element);
    }

    /// <summary>
    /// Rewrites the list of elements.
    /// </summary>
    public virtual ImmutableList<TElement> Rewrite<TElement>(ImmutableList<TElement> elements)
        where TElement : SemanticElement
    {
        return elements.Rewrite(e => Rewrite(e));
    }

    /// <summary>
    /// Rewrites the current state of an element after its children have been rewritten.
    /// </summary>
    /// <param name="current">The current state of the element after children were rewritten.</param>
    /// <param name="original">The original state of the element before children were rewritten.</param>
    protected virtual SemanticElement Rewrite(SemanticElement current, SemanticElement original)
    {
        return current;
    }
}

public static class RewriterExtensions
{
    /// <summary>
    /// Rewrite all the elements of the specified type, with the function.
    /// </summary>
    public static SemanticElement RewriteAll<TElement>(
        this SemanticElement root, 
        Func<TElement, TElement, SemanticElement> fnRewrite
        )
        where TElement : SemanticElement
    {
        return new TypedRewriter<TElement>(fnRewrite).Rewrite(root)!;
    }

    /// <summary>
    /// Rewrite all the elements of the specified type, with the function.
    /// </summary>
    public static SemanticElement RewriteAll<TElement>(
        this SemanticElement root,
        Func<TElement, SemanticElement> fnRewrite
        )
        where TElement : SemanticElement
        =>
        RewriteAll<TElement>(root, (current, original) => fnRewrite(current));


    /// <summary>
    /// Rewrite all the elements of the specified type, with the function.
    /// </summary>
    public static ImmutableList<TRoot> RewriteAll<TRoot, TElement>(
        this ImmutableList<TRoot> roots,
        Func<TElement, TElement, SemanticElement> fnRewrite
        )
        where TRoot : SemanticElement
        where TElement : SemanticElement
    {
        return new TypedRewriter<TElement>(fnRewrite).Rewrite(roots);
    }

    /// <summary>
    /// Rewrite all the elements of the specified type, with the function.
    /// </summary>
    public static ImmutableList<TRoot> RewriteAll<TRoot, TElement>(
        this ImmutableList<TRoot> roots,
        Func<TElement, SemanticElement> fnRewrite
        )
        where TRoot : SemanticElement
        where TElement : SemanticElement
        =>
        RewriteAll<TRoot, TElement>(roots, (current, original) => fnRewrite(current));


    private class TypedRewriter<TElement> : SemanticRewriter
        where TElement : SemanticElement
    {
        private readonly Func<TElement, TElement, SemanticElement> _fnRewriter;

        public TypedRewriter(Func<TElement, TElement, SemanticElement> fnRewriter)
        {
            _fnRewriter = fnRewriter;
        }

        protected override SemanticElement Rewrite(SemanticElement current, SemanticElement original)
        {
            if (current is TElement tcurrent)
            {
                return _fnRewriter(tcurrent, (TElement)original);
            }
            else
            {
                return current;
            }
        }
    }
}