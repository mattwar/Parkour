namespace Parkour.Lowering;

using Semantics;
using Symbols;

/// <summary>
/// Rewrites declarations and expressions into a form compatible with emitting.
/// </summary>
public abstract class DeclarationLowerer
{
    /// <summary>
    /// Rewrites declarations and body expressions into a form compatible with emitting.
    /// </summary>
    public abstract DeclarationLowering LowerDeclarations(
        ImmutableList<Declaration> declarations,
        SymbolTable externalSymbols);

    /// <summary>
    /// Rewrites expressions into a form compatible with emitting into a method body.
    /// </summary>
    public abstract ExpressionLowering LowerExpression(
        Expression expression,
        SymbolTable externalSymbols);
}
