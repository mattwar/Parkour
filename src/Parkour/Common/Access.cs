namespace Parkour;

/// <summary>
/// The base class of an open heirarchy of access restrictions.
/// Each access is a singleton subtype of <see cref="Access"/>.
/// </summary>
public abstract class Access
{
    public string Name => this.GetType().Name;

    public override string ToString() => this.Name;
}
