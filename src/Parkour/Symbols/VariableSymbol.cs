namespace Parkour.Symbols;

public sealed class VariableSymbol : Symbol
{
    public TypeSymbol VariableType { get; }

    public VariableSymbol(string name, TypeSymbol variableType) : base(name)
    {
        VariableType = variableType;
    }
}
