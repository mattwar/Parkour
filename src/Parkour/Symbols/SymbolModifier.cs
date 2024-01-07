namespace Parkour.Symbols;

[Flags]
public enum SymbolModifier
{
    None = 0,
    Static = 0b0000_0001,
    Abstract = 0b0000_0010,
    Virtual = 0b0000_0100,
    Override = 0b0000_1000,
    Sealed = 0b0001_0000,
    HideBySig = 0b0010_0000,
    Special = 0b0100_0000,
    ReadOnly = 0b1000_0000
}