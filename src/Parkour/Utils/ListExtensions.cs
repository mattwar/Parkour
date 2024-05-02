namespace Parkour.Utils;

internal static class CollectionExtensions
{
    public static void AddRange<T>(this HashSet<T> hset, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            hset.Add(item);
        }
    }
}
