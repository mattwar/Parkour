namespace Parkour;

/// <summary>
/// The base class of an open hierarchy of operators.
/// Each operator is a singleton subtype of <see cref="Operator"/>
/// </summary>
public abstract class Operator
{
    public string Name => this.GetType().Name;

    public override string ToString() => this.Name;
}