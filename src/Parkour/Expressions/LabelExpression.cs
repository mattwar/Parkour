namespace Parkour.Expressions;
using Symbols;
using Syntax;

public class LabelExpression : Expression
{
    public string Name { get; }
    public TargetSymbol? Target { get; }

    public LabelExpression(
        string name,
        TargetSymbol? target,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            ContainsState.None, 
            resultType ?? target?.Type, 
            diagnostics,
            syntax)
    {
        this.Name = name;
        this.Target = target;
    }
}

