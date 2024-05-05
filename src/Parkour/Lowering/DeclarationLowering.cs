using Parkour.Binding;
using Parkour.Symbols;
using Parkour.Semantics;

namespace Parkour.Lowering;

/// <summary>
/// </summary>
public abstract class DeclarationLowering
{
    /// <summary>
    /// The lowered declarations.
    /// </summary>
    public abstract ImmutableList<Declaration> Declarations { get; }

    /// <summary>
    /// Diagnostics determined during lowering.
    /// </summary>
    public abstract ImmutableList<Diagnostic> Diagnostics { get; }
}
