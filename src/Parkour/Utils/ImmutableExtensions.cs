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
}