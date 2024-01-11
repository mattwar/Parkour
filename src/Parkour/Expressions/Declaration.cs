namespace Parkour.Expressions;
using Symbols;
using Syntax;

public abstract class Declaration : SemanticElement
{
    public string Name { get; }

    private protected Declaration(
        ContainsState state,
        string name,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(state, diagnostics, syntax)
    {
        this.Name = name;
    }
}