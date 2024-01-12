namespace Parkour;
using Binding;
using Semantics;
using Symbols;

public class DeclarationCompilation : Compilation
{
    private readonly DeclarationBinding _binding;
    private readonly Dictionary<ISourceDocument, ImmutableList<Declaration>> _docToUnboundDeclarations;

    public override ImmutableList<ISourceDocument> Documents { get; }

    public override NamespaceSymbol GlobalNamespace => 
        _binding.GlobalNamespace;

    private DeclarationCompilation(
        DeclarationBinding binding)
    {
        _binding = binding;

        this.Documents = binding.Unbound
            .Where(d => d.Location != null)
            .Select(d => d.Location!.Document)
            .Distinct()
            .ToImmutableList();

        _docToUnboundDeclarations = binding.Unbound
            .Where(d => d.Location != null)
            .GroupBy(d => d.Location!.Document)
            .ToDictionary(g => g.Key, g => g.ToImmutableList());
    }

    public static DeclarationCompilation Create(
        ImmutableList<Declaration> declarations,
        ImmutableList<NamespaceSymbol> imports)
    {
        var binder = new DeclarationBinder();
        var bound = binder.Bind(declarations, imports);
        return new DeclarationCompilation(bound);
    }

    public static DeclarationCompilation Create(
        ImmutableList<Declaration> declarations,
        params NamespaceSymbol[] imports)
    {
        return Create(declarations, imports.ToImmutableList());
    }

    public override void GetDiagnostics(ISourceDocument source, List<Diagnostic> diagnostics)
    {
        var tmp = new List<Diagnostic>();

        if (_docToUnboundDeclarations.TryGetValue(source, out var unboundDecls))
        {
            foreach (var unbound in unboundDecls)
            {
                _binding.GetBound(unbound);
                tmp.AddRange(unbound.GetContainedDiagnostics());
            }
        }
    }
}
