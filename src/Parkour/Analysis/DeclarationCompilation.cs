namespace Parkour.Analysis;

using Expressions;
using Symbols;

public class DeclarationCompilation : Compilation
{
    private DeclarationBinding _binding;

    public override NamespaceSymbol GlobalNamespace => 
        _binding.GlobalNamespace;

    private DeclarationCompilation(
        DeclarationBinding bound)
    {
        _binding = bound;
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

    private ImmutableList<Diagnostic>? _diagnostics;

    public override ImmutableList<Diagnostic> GetDiagnostics()
    {
        if (_diagnostics == null)
        {
            var tmp = new List<Diagnostic>();
            foreach (var decl in _binding.Unbound)
            {
                var bound = _binding.GetBound(decl);
                tmp.AddRange(bound.GetContainedDiagnostics());
            }

            Interlocked.CompareExchange(ref _diagnostics, tmp.ToImmutableList(), null);
        }

        return _diagnostics ?? ImmutableList<Diagnostic>.Empty;
    }
}
