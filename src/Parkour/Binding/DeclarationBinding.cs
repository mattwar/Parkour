namespace Parkour.Binding;

using Semantics;
using Symbols;

/// <summary>
/// The result of binding a set of declarations
/// </summary>
public abstract class DeclarationBinding
{
    /// <summary>
    /// The declarations given as input to the binder.
    /// </summary>
    public abstract ImmutableList<Declaration> UnboundDeclarations { get; }

    /// <summary>
    /// The external symbols given as input to the binder.
    /// </summary>
    public abstract SymbolTable ExternalSymbols { get; }

    /// <summary>
    /// The declarations after being bound.
    /// May include additional declarations introduced during binding.
    /// </summary>
    public abstract ImmutableList<Declaration> BoundDeclarations { get; }

    /// <summary>
    /// The symbols corresponding to the bound declarations.
    /// </summary>
    public abstract GlobalNamespaceSymbol DeclaredSymbols { get; }

    /// <summary>
    /// All diagnostics determined during binding.
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
    public abstract ImmutableList<Declaration> GetSymbolDeclarations(Symbol symbol);
}
