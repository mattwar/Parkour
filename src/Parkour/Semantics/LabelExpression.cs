namespace Parkour.Semantics;
using Symbols;
using Syntax;

public class LabelExpression : Expression
{
    public string Name { get; }
    public TargetSymbol? Target { get; }

    public LabelExpression(
        string name,
        ISourceLocation? location,
        TargetSymbol? target,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            ContainsState.None, 
            location,
            resultType ?? target?.Type, 
            diagnostics)
    {
        this.Name = name;
        this.Target = target;
    }
}

