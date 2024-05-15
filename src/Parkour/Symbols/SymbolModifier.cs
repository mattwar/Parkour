namespace Parkour.Symbols;

[Flags]
public enum SymbolModifier
{
    None = 0,
    Static      = 1,
    Abstract    = 1 << 2,
    Virtual     = 1 << 3,
    Override    = 1 << 4,
    Sealed      = 1 << 5,
    HideBySig   = 1 << 6,
    Special     = 1 << 7,
    ReadOnly    = 1 << 8,
    Constant    = 1 << 9
}