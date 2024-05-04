using Parkour.Semantics;
using Parkour.Symbols;

namespace Parkour.Binding;

public abstract class Binder
{
    /// <summary>
    /// Creates symbols for all symbol declarations,
    /// rewrites expressions to include referenced symbols, result types and diagnostics.
    /// </summary>
    public abstract DeclarationBinding BindDeclarations(
        ImmutableList<Declaration> declarations,
        SymbolTable externalSymbols);

    /// <summary>
    /// Rewrites expressions to include referenced symbols, result types and diagnostics.
    /// </summary>
    public abstract ExpressionBinding BindExpression(
        Expression expression,
        SymbolTable externalSymbols,
        BindingScope scope);

    /// <summary>
    /// Rewrites expressions to include referenced symbols, result types and diagnostics.
    /// </summary>
    public abstract ExpressionBinding BindExpression(
        Expression expression,
        SymbolTable externalSymbols);
}