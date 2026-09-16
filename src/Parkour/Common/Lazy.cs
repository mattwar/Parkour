namespace Parkour;

/// <summary>
/// Evaluates a value on demand.
/// Evaluation is thread-safe. 
/// Will returning a default value if the evaluation function is cyclic (calls back into the lazy Value property).
/// </summary>
public class Lazy<TValue>
{
    /// <summary>
    /// The function to evaluate lazily.
    /// </summary>
    private Func<TValue>? _fnValue;

    /// <summary>
    /// The evaluated value (or default value)
    /// </summary>
    private TValue _value;

    /// <summary>
    /// The lock to use to guarantee thread safety.
    /// </summary>
    private object? _syncLock;

    public Lazy(
        Func<TValue> fnValue,
        TValue defaultValue = default!)
    {
        _fnValue = fnValue;
        _value = defaultValue;
        _syncLock = this; // use this as the lock?
    }

    /// <summary>
    /// The lazily computed value.
    /// </summary>
    public TValue Value
    {
        get
        {
            if (_fnValue is { } fnValue
                && Interlocked.CompareExchange(ref _fnValue, null, fnValue) == fnValue
                && _syncLock != null)
            {
                // first one in does computation and remove lock after
                lock (_syncLock)
                {
                    _value = fnValue();
                    _syncLock = null;
                }
            }
            else if (_syncLock is { } syncLock)
            {
                // Wait until any current held lock is released..
                // Note: cyclic callers on same thread as as current lock holder will not block
                // and end up returning default value.
                lock (syncLock)
                {
                }
            }

            return _value;
        }
    }
}