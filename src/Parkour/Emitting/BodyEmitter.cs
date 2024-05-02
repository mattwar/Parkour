namespace Parkour.Emitting;

using Semantics;
using Symbols;

/// <summary>
/// Emits expressions method bodies.
/// </summary>
public abstract class BodyEmitter
{
    /// <summary>
    /// Emits an expression as a method body.
    /// </summary>
    public abstract void EmitBody(
        Expression body,
        TypeSymbol returnType,
        LabelSymbol? returnLabel);
}
