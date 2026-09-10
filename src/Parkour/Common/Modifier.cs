namespace Parkour;

/// <summary>
/// An open-hierarchy of modifiers.
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