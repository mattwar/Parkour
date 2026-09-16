namespace Parkour;

internal static class CollectionExtensions
{
    /// <summary>
    /// Adds multiple items to a hash set.
    /// </summary>
    public static void AddRange<T>(this HashSet<T> hset, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            hset.Add(item);
        }
    }
}
