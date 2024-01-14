namespace Parkour.Compilations;
using Binding;
using Semantics;
using Symbols;

public class DeclarationCompilation : Compilation
{
    private readonly ImmutableList<ISyntaxTree> _syntaxTrees;
    private readonly NamespaceSymbol _externalSymbols;
    private readonly Func<ImmutableList<ISyntaxTree>, NamespaceSymbol, DeclarationBinding> _fnBind;

    public override ImmutableList<ISourceDocument> Documents { get; }

    public DeclarationCompilation(
        ImmutableList<ISyntaxTree> syntaxTrees,
        NamespaceSymbol externalSymbols,
        Func<ImmutableList<ISyntaxTree>, NamespaceSymbol, DeclarationBinding> fnBind)
    {
        _syntaxTrees = syntaxTrees;
        _externalSymbols = externalSymbols;
        _fnBind = fnBind;

        this.Documents = _syntaxTrees
            .OfType<ISourceDocument>()
            .ToImmutableList();
    }

    public DeclarationCompilation(
        ImmutableList<ISyntaxTree> syntaxTrees,
        DeclarationBinding binding)
        : this(syntaxTrees, binding.ExternalSymbols, (_trees, _externals) => binding)
    {
    }

    private DeclarationBinding? _binding;

    private DeclarationBinding GetBinding()
    {
        if (_binding == null)
        {
            var tmp = _fnBind(_syntaxTrees, _externalSymbols);
            Interlocked.CompareExchange(ref _binding, tmp, null);
        }

        return _binding;
    }

    private Dictionary<ISourceDocument, Declaration>? _docToUnboundDeclarations;

    private Declaration? GetUnboundDeclaration(ISourceDocument document)
    {
        if (_docToUnboundDeclarations == null)
        {
            var binding = GetBinding();
            var tmp = binding.UnboundDeclarations
                .Where(d => d.Location != null)
                .ToDictionary(d => d.Location!.Document, d => d);
            Interlocked.CompareExchange(ref _docToUnboundDeclarations, tmp, null);
        }

        _docToUnboundDeclarations.TryGetValue(document, out var unbound);
        return unbound;
    }

    public override ISyntaxTree? GetSyntaxTree(ISourceDocument document)
    {
        return document as ISyntaxTree;
    }

    public override void GetAnnotations<TAnnotation>(
        ISourceDocument document, 
        int position, 
        Func<TAnnotation, bool>? filter, 
        List<TAnnotation> annotations)
    {
        if (_syntaxTrees.FirstOrDefault(t => t.Document == document) is { } tree)
        {
            tree.Annotations.GetAnnotations(position, filter, annotations);
        }
    }

    public override void GetDiagnostics(ISourceDocument document, Func<Diagnostic, bool>? filter, List<Diagnostic> diagnostics)
    {
        diagnostics.AddRange(
            _syntaxTrees
            .SelectMany(t => t.Diagnostics)
            .Where(d => filter == null || filter(d))
            );

        var binding = GetBinding();
        if (GetUnboundDeclaration(document) is { } unbound
            && binding.GetBoundDeclaration(unbound) is { } bound)
        {
            bound.GetContainedDiagnostics(filter, diagnostics);
        }
    }
}
