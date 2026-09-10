namespace Parkour;

/// <summary>
/// A collection of settings for services and operations.
/// </summary>
public sealed class Settings
{
    private ImmutableDictionary<ISetting, object?> _valueMap;

    private Settings(
        ImmutableDictionary<ISetting, object?> valueMap)
    {
        _valueMap = valueMap;
    }

    public static readonly Settings Default =
        new Settings(ImmutableDictionary<ISetting, object?>.Empty);

    /// <summary>
    /// Returns a new <see cref="Settings"/> with the value for the specified setting updated.
    /// </summary>
    public Settings WithSettingValue<TValue>(Setting<TValue> option, TValue? value)
    {
        var map = _valueMap.SetItem(option, value);
        return new Settings(map);
    }

    /// <summary>
    /// Gets the value for the specified setting.
    /// </summary>
    public TValue? GetSettingValue<TValue>(Setting<TValue> option)
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
    /// Returns a new <see cref="Settings"/> with the value for the specified setting updated.
    /// </summary>
    public Settings WithSettingValue(ISetting option, object? value)
    {
        var map = _valueMap.SetItem(option, value);
        return new Settings(map);
    }

    /// <summary>
    /// Gets the value for the specified setting.
    /// </summary>
    public object? GetSettingValue(ISetting option)
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

/// <summary>
/// A untyped setting access interface.
/// </summary>
public interface ISetting
{
    string Name { get; }
    string Description { get; }
    object? DefaultValue { get; }
}

/// <summary>
/// A setting with a strongly-typed value.
/// </summary>
public class Setting<TValue>
    : ISetting
{
    public string Name { get; }
    public string Description { get; }
    public TValue? DefaultValue { get; }
    object? ISetting.DefaultValue => DefaultValue;

    public Setting(string name, string description, TValue defaultValue)
    {
        this.Name = name;
        this.Description = description;
        this.DefaultValue = defaultValue;
    }
}