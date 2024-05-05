namespace Parkour.Binding;

using Semantics;
using Symbols;

/// <summary>
/// The result of binding a set of declarations
/// </summary>
public abstract class DeclarationBinding
{
    /// <summary>
    /// The declarations after being bound.
    /// </summary>
    public abstract ImmutableList<Declaration> Declarations { get; }

    /// <summary>
    /// The combined external and declared symbol table.
    /// </summary>
    public abstract SymbolTable Symbols { get; }

    /// <summary>
    /// All diagnostics determined during binding.
    /// </summary>
    public abstract ImmutableList<Diagnostic> Diagnostics { get; }
}
