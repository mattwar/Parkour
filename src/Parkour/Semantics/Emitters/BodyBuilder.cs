namespace Parkour.Semantics;

using Symbols;

/// <summary>
/// Emits expressions method bodies.
/// </summary>
public abstract class BodyBuilder
{
    /// <summary>
    /// Emits an expression as a method body.
    /// </summary>
    public abstract void BuildBody(
        Expression? body,
        TypeSymbol returnType,
        LabelSymbol? returnLabel);
}
