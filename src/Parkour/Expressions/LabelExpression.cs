namespace Parkour.Expressions;
using Symbols;

public class LabelExpression : Expression
{
    public string Name { get; }
    public TargetSymbol? Target { get; }

    public LabelExpression(
        string name,
        TargetSymbol? target,
        TypeSymbol? resultType)
        : base(ContainsState.None, resultType ?? target?.Type, null)
    {
        this.Name = name;
        this.Target = target;
    }
}

