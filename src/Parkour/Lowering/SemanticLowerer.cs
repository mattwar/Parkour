namespace Parkour.Lowering;

using Semantics;
using Symbols;

/// <summary>
/// Converts high-level semantics into low-level semantics,
/// rewriting declarations and expressions into a form compatible with emitting.
/// </summary>
public abstract class SemanticLowerer
{
    /// <summary>
    /// Converts high-level declarations into low-level declarations.
    /// </summary>
    public abstract DeclarationLowering LowerDeclarations(
        ImmutableList<Declaration> declarations,
        SymbolTable externalSymbols);

    /// <summary>
    /// Converts high-level expressions into low-level expressions.
    /// </summary>
    public abstract ExpressionLowering LowerExpression(
        Expression expression,
        SymbolTable externalSymbols);
}