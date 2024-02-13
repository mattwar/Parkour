namespace Parkour.Binding;

using Semantics;
using Symbols;

/// <summary>
/// The result of binding a set of declarations
/// </summary>
public abstract class DeclarationBinding
{
    /// <summary>
    /// The declarations as they were before being bound.
    /// </summary>
    public abstract ImmutableList<Declaration> UnboundDeclarations { get; }

    /// <summary>
    /// The declarations after being bound.
    /// </summary>
    public abstract ImmutableList<Declaration> BoundDeclarations { get; }

    /// <summary>
    /// The global namespace the contains all the externally declared symbols.
    /// </summary>
    public abstract GlobalNamespaceSymbol ExternalSymbols { get; }

    /// <summary>
    /// The global namespace that contains all declared symbols from this binding.
    /// </summary>
    public abstract GlobalNamespaceSymbol DeclaredSymbols { get; }

    /// <summary>
    /// The global namespace that combines all the external and declared symbols.
    /// </summary>
    public abstract GlobalNamespaceSymbol CombinedSymbols { get; }

    /// <summary>
    /// All diagnostics from all bound declarations
    /// </summary>
    public abstract ImmutableList<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets the bound declaration corresponding to an unbound declaration.
    /// </summary>
    public virtual Declaration? GetBoundDeclaration(Declaration unboundDeclaration)
    {
        var index = this.UnboundDeclarations.IndexOf(unboundDeclaration);
        if (index >= 0 && index <= this.BoundDeclarations.Count)
        {
            return this.BoundDeclarations[index];
        }

        return null;
    }

    /// <summary>
    /// Get all bound declarations that correspond to a declared symbol.
    /// </summary>
    public abstract ImmutableList<Declaration> GetBoundSymbolDeclarations(Symbol symbol);
}
