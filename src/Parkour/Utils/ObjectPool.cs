namespace Parkour;

/// <summary>
/// Maintains a pool of strongly-typed items.
/// </summary>
internal class ObjectPool<T>
    where T : class
{
    /// <summary>
    /// The function that creates the item. Used when a fresh item is needed.
    /// </summary>
    private readonly Func<T> creator;

    /// <summary>
    /// The function used to reset an item so it can be reused.
    /// </summary>
    private readonly Action<T> resetter;

    /// <summary>
    /// The items in the pool.
    /// </summary>
    private readonly T?[] items;

    /// <summary>
    /// Creates a new pool.
    /// </summary>
    /// <param name="creator">The function that creates the item. Called when a fresh item is needed.</param>
    /// <param name="resetter">The function that resets an item, so it can be reused.</param>
    /// <param name="size">The size of the pool</param>
    public ObjectPool(
        Func<T> creator, 
        Action<T> resetter, 
        int size = 10)
    {
        this.creator = creator;
        this.resetter = resetter;
        this.items = new T[size];
    }

    /// <summary>
    /// Allocates an item from the pool, or creates a fresh item if not items in the pool.
    /// </summary>
    public T AllocateFromPool()
    {
        // look for item returned to pool
        for (int i = 0; i < this.items.Length; i++)
        {
            if (this.items[i] != null)
            {
                var item = Interlocked.Exchange(ref this.items[i], null);
                if (item != null)
                {
                    return item;
                }
            }
        }

        // make a new one
        return this.creator();
    }

    /// <summary>
    /// Puts an item back in the pool.
    /// The item will be reset using the resetter function supplied.
    /// </summary>
    public void ReturnToPool(T item)
    {
        // clear item
        this.resetter(item);

        // look for open space to place in pool
        // if no space is found, let GC have it.
        for (int i = 0; i < this.items.Length; i++)
        {
            if (this.items[i] == null)
            {
                var result = Interlocked.CompareExchange(ref this.items[i], item, null);
                if (result == null)
                {
                    break;
                }
            }
        }
    }
}