namespace Parkour.Services;

public class ServiceOptions
{
    private ImmutableDictionary<IServiceOption, object?> _valueMap;

    private ServiceOptions(
        ImmutableDictionary<IServiceOption, object?> valueMap)
    {
        _valueMap = valueMap;
    }

    public static readonly ServiceOptions Default =
        new ServiceOptions(ImmutableDictionary<IServiceOption, object?>.Empty);

    /// <summary>
    /// Returns a new <see cref="ServiceOptions"/> with the value for the specified option updated.
    /// </summary>
    public ServiceOptions WithOptionValue<TValue>(ServiceOption<TValue> option, TValue? value)
    {
        var map = _valueMap.SetItem(option, value);
        return new ServiceOptions(map);
    }

    /// <summary>
    /// Gets the value for the specified option.
    /// </summary>
    public TValue? GetOptionValue<TValue>(ServiceOption<TValue> option)
    {
        if (_valueMap.TryGetValue(option, out var boxedValue))
        {
            if (boxedValue is TValue value)
            {
                return value;
            }
            else
            {
                return default;
            }
        }
        else
        {
            return option.DefaultValue;
        }
    }

    /// <summary>
    /// Returns a new <see cref="ServiceOptions"/> with the value for the specified option updated.
    /// </summary>
    public ServiceOptions WithOptionValue(IServiceOption option, object? value)
    {
        var map = _valueMap.SetItem(option, value);
        return new ServiceOptions(map);
    }

    /// <summary>
    /// Gets the value for the specified option.
    /// </summary>
    public object? GetOptionValue(IServiceOption option)
    {
        if (_valueMap.TryGetValue(option, out var value))
        {
            return value;
        }
        else
        {
            return option.DefaultValue;
        }
    }
}

public interface IServiceOption
{
    string Name { get; }
    string Description { get; }
    object? DefaultValue { get; }
}

public class ServiceOption<TValue>
    : IServiceOption
{
    public string Name { get; }
    public string Description { get; }
    public TValue? DefaultValue { get; }
    object? IServiceOption.DefaultValue => DefaultValue;

    public ServiceOption(string name, string description, TValue defaultValue)
    {
        this.Name = name;
        this.Description = description;
        this.DefaultValue = defaultValue;
    }
}