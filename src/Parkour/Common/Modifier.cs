namespace Parkour;

/// <summary>
/// The base class of an open hierarchy of modifiers.
/// Each modifier is a singleton subtype of <see cref="Modifier"/>.
/// </summary>
public abstract class Modifier
{
    public string Name => this.GetType().Name;

    public override string ToString() => this.Name;

    #region BitSet Helpers

    /// <summary>
    /// No modifiers
    /// </summary>
    public static BitSet<Modifier> None =
        BitSet<Modifier>.Empty;

    public static BitSet<Modifier> operator |(Modifier modifier1, Modifier modifier2) =>
        ((BitSet<Modifier>)modifier1) | ((BitSet<Modifier>)modifier2);

    public static BitSet<Modifier> operator +(Modifier modifier1, Modifier modifier2) =>
        ((BitSet<Modifier>)modifier1) + ((BitSet<Modifier>)modifier2);

    public static BitSet<Modifier> operator -(Modifier modifier1, Modifier modifier2) =>
        ((BitSet<Modifier>)modifier1) - ((BitSet<Modifier>)modifier2);
    #endregion
}