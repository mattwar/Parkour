namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class ConstantExpression : Expression
{
    public object? Value { get; }

    public ConstantExpression(
        object? value,
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType), 
            location,
            resultType, 
            diagnostics)
    {
        this.Value = value;
    }
}

