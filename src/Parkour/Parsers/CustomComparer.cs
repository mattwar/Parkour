using System.Diagnostics.CodeAnalysis;

namespace Parkour.Parsers;

/// <summary>
/// An <see cref="EqualityComparer{T}"/> implemented by supplied functions.
/// </summary>
internal class CustomEqualityComparer<T> : EqualityComparer<T>
{
    private readonly Func<T, T, bool> _fnEquals;
    private readonly Func<T, int> _fnHashCode;
    
    public CustomEqualityComparer(
        Func<T, T, bool> fnEquals, 
        Func<T, int> fnHashCode)
    {
        _fnEquals = fnEquals;
        _fnHashCode = fnHashCode;
    }

    public override bool Equals(T? x, T? y)
    {
        return x != null && y != null && _fnEquals(x, y);
    }

    public override int GetHashCode([DisallowNull] T obj)
    {
        return _fnHashCode(obj);
    }
}