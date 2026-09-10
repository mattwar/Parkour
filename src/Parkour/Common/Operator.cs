namespace Parkour;

/// <summary>
/// An open hierarchy of operators.
/// </summary>
public abstract class Operator
{
    public string Name => this.GetType().Name;

    public override string ToString() => this.Name;
}