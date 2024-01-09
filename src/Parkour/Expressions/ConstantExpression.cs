namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class ConstantExpression : Expression
{
    public object? Value { get; }

    public ConstantExpression(
        object? value,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            ContainsState.None, 
            resultType, 
            diagnostics,
            syntax)
    {
        this.Value = value;
    }
}

