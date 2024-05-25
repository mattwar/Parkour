using Parkour.Semantics;

namespace System.Collections.Immutable;

internal static class ImmutableExtensions
{
    public static ImmutableList<T> AppendRange<T>(this ImmutableList<T> list, IEnumerable<T> items) =>
        list.AddRange(items);

    public static void CopyTo<T>(this ImmutableList<T> list, int start, Span<T> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (start + i < list.Count)
                span[i] = list[start + i];
        }
    }

    /// <summary>
    /// Maps the items of the to instance of the same type.
    /// If all items stay the same instance, the original list is returned.
    /// </summary>
    public static ImmutableList<TItem> Map<TItem>(
        this ImmutableList<TItem> list,
        Func<TItem, TItem> fnMapper,
        IEqualityComparer<TItem>? comparer = null)
        where TItem : class
    {
        List<TItem> newList = null!;
        comparer = comparer ?? EqualityComparer<TItem>.Default;

        for (int i = 0; i < list.Count; i++)
        {
            var expr = list[i];
            var newExpr = fnMapper(expr);

            if (!comparer.Equals(newExpr, expr))
            {
                if (newList == null)
                {
                    newList = new List<TItem>(list.Count);
                    if (i > 0)
                        newList.AddRange(list.Take(i));
                }

                newList.Add(newExpr);
            }
            else if (newList != null)
            {
                newList.Add(expr);
            }
        }

        if (newList != null)
        {
            return ImmutableList<TItem>.Empty.AppendRange(newList);
        }
        else
        {
            return list;
        }
    }
}