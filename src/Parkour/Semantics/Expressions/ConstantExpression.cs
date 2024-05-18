namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// A value known at compile time.
/// </summary>
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

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}