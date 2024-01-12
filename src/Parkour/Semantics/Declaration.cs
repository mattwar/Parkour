namespace Parkour.Semantics;
using Symbols;
using Syntax;

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Declaration : SemanticElement
{
    private string DebugText => $"{GetType().Name}: {Name}";

    public string Name { get; }

    private protected Declaration(
        ContainsState state,
        string name,
        ISourceLocation? location,
        ImmutableList<Diagnostic>? diagnostics)
        : base(state, location, diagnostics)
    {
        this.Name = name;
    }
}