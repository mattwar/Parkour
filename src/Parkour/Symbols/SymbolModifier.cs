namespace Parkour.Symbols;

/// <summary>
/// Common modifiers for symbols.
/// </summary>
public class SymbolModifier
{
    private readonly string _name;

    public SymbolModifier(string name)
    {
        _name = name;
    }

    public override string ToString() => _name;

    /// <summary>
    /// No modifiers
    /// </summary>
    public static BitSet<SymbolModifier> None = 
        BitSet<SymbolModifier>.Empty;

    /// <summary>
    /// The type or member is abstract.
    /// </summary>
    public static readonly BitSet<SymbolModifier> Abstract = 
        new SymbolModifier(nameof(Abstract));

    /// <summary>
    /// The field is constant.
    /// </summary>
    public static readonly BitSet<SymbolModifier> Constant = 
        new SymbolModifier(nameof(Constant));

    /// <summary>
    /// The member should be ignored when searching for symbols.
    /// </summary>
    public static readonly BitSet<SymbolModifier> HideBySig = 
        new SymbolModifier(nameof(HideBySig));

    /// <summary>
    /// The member overrides an abstract or virtual member in the base.
    /// </summary>
    public static readonly BitSet<SymbolModifier> Override = 
        new SymbolModifier(nameof(Override));

    /// <summary>
    /// The type or member is sealed.
    /// </summary>
    public static readonly BitSet<SymbolModifier> Sealed = 
        new SymbolModifier(nameof(Sealed));

    /// <summary>
    /// The member is static (not instance).
    /// </summary>
    public static readonly BitSet<SymbolModifier> Static = 
        new SymbolModifier(nameof(Static));

    /// <summary>
    /// The member is virtual (has a body and can be overridden).
    /// </summary>
    public static readonly BitSet<SymbolModifier> Virtual =
        new SymbolModifier(nameof(Virtual));

    /// <summary>
    /// The field is read only.
    /// </summary>
    public static readonly BitSet<SymbolModifier> ReadOnly = 
        new SymbolModifier(nameof(ReadOnly));

    /// <summary>
    /// The member is special in some way.
    /// </summary>
    public static readonly BitSet<SymbolModifier> Special = 
        new SymbolModifier(nameof(Special));

    /// <summary>
    /// The member in input only
    /// </summary>
    public static readonly BitSet<SymbolModifier> In =
        new SymbolModifier(nameof(In));

    /// <summary>
    /// The member is output only
    /// </summary>
    public static readonly BitSet<SymbolModifier> Out =
        new SymbolModifier(nameof(Out));

    /// <summary>
    /// The member is ref
    /// </summary>
    public static readonly BitSet<SymbolModifier> Ref =
        new SymbolModifier(nameof(Ref));
}
