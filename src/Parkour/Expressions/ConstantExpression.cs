namespace Parkour.Expressions;
using Symbols;

public sealed class ConstantExpression : Expression
{
    public object? Value { get; }

    public ConstantExpression(
        object? value,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(ContainsState.None, resultType, diagnostics)
    {
        this.Value = value;
    }
}

