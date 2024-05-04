using Parkour.Binding;
using Parkour.Symbols;
using Parkour.Semantics;

namespace Parkour.Lowering;

/// <summary>
/// </summary>
public abstract class DeclarationLowering
{
    /// <summary>
    /// The input binding of the lowering.
    /// </summary>
    public abstract DeclarationBinding Binding { get; }

    /// <summary>
    /// The lowered declarations.
    /// May include additional declarations introduced during lowering.
    /// </summary>
    public abstract ImmutableList<Declaration> LoweredDeclarations { get; }

    /// <summary>
    /// The symbols corresponding to the lowered declarations.
    /// </summary>
    public abstract GlobalNamespaceSymbol DeclaredSymbols { get; }

    /// <summary>
    /// Diagnostics determined during lowering.
    /// </summary>
    public abstract ImmutableList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the lowered declaration of the corresponding method symbol.
    /// </summary>
    public abstract MethodDeclaration? GetMethodDeclaration(MethodSymbol methodSymbol);

    /// <summary>
    /// Gets the lowered declaration of the corresponding constructor symbol.
    /// </summary>
    public abstract ConstructorDeclaration? GetConstructorDeclaration(ConstructorSymbol constructorSymbol);
}
