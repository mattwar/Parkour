namespace Parkour.Binding;

using Semantics;
using Symbols;

public abstract class DeclarationBinding
{
    public abstract ImmutableList<Declaration> UnboundDeclarations { get; }
    public abstract ImmutableList<Declaration> BoundDeclarations { get; }
    public abstract NamespaceSymbol ExternalSymbols { get; }
    public abstract NamespaceSymbol DeclarationSymbols { get; }
    public abstract NamespaceSymbol GlobalNamespace { get; }

    public virtual Declaration? GetBoundDeclaration(Declaration unboundDeclaration)
    {
        var index = this.UnboundDeclarations.IndexOf(unboundDeclaration);
        if (index >= 0 && index <= this.BoundDeclarations.Count)
        {
            return this.BoundDeclarations[index];
        }

        return null;
    }
}

#if false
public class BoundDeclarationBinding : DeclarationBinding
{
    public override ImmutableList<Declaration> UnboundDeclarations { get; }
    public override ImmutableList<Declaration> BoundDeclarations { get; }
    public override NamespaceSymbol CombinedGlobalNamespace { get; }

    public BoundDeclarationBinding(
        ImmutableList<Declaration> unboundDeclarations,
        ImmutableList<Declaration> boundDeclarations,
        NamespaceSymbol combinedGlobalNamespace)
    {
        this.UnboundDeclarations = unboundDeclarations;
        this.BoundDeclarations = boundDeclarations;
        this.CombinedGlobalNamespace = combinedGlobalNamespace;
    }
}
#endif