namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// References the current instance within a code body.
/// </summary>
public sealed class ThisExpression : Expression
{
    public ThisExpression(
        ISourceLocation? location,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
    }

    public override int ChildCount => 0;
    public override SemanticElement? GetChild(int index) => null;
}