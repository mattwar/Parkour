namespace Parkour;

/// <summary>
/// Allows for thread-safe evalution of a value,
/// returning a default value if the evaluation function is cyclic.
/// </summary>
public class Lazy<TValue>
{
    private Func<TValue>? _fnValue;
    private TValue _value;
    private object? _syncLock;

    private Lazy(
        Func<TValue>? fnValue,
        TValue value,
        object? syncLock)
    {
        _fnValue = fnValue;
        _value = value;
        _syncLock = syncLock;
    }

    public Lazy(
        Func<TValue> fnValue,
        TValue defaultValue = default!)
    {
        _fnValue = fnValue;
        _value = defaultValue;
        _syncLock = fnValue;
    }

    public TValue Value
    {
        get
        {
            if (_fnValue is { } fnValue
                && Interlocked.CompareExchange(ref _fnValue, null, fnValue) == fnValue)
            {
                // first one in does computation and remove lock after
                lock (_syncLock!)
                {
                    _value = fnValue();
                    _syncLock = null;
                }
            }
            else if (_syncLock is { } syncLock)
            {
                // while there is still a lock..
                // callers on same thread as as current lock holder will not block
                // and end up returning default value.
                lock (syncLock)
                {
                }
            }

            return _value;
        }
    }
}