namespace Parkour.Symbols;

public sealed class VariableSymbol : Symbol
{
    public TypeSymbol VariableType { get; }

    public VariableSymbol(string name, TypeSymbol variableType) : base(name)
    {
        VariableType = variableType;
    }

    public override int ReferenceCount => 1;
    public override Symbol? GetReference(int index) => index == 0 ? this.VariableType : null;
}
