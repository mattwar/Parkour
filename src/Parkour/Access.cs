namespace Parkour;

/// <summary>
/// The base class of an open enum for access restrictions.
/// </summary>
public abstract class Access
{
    public string Name => this.GetType().Name;

    public override string ToString() => this.Name;
}
