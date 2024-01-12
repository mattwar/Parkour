namespace Parkour.Analysis;

using Semantics;
using Symbols;

public class DeclarationBinding
{
    private readonly DeclarationBinder _binder;
    private readonly BindingScope _defaultScope;

    public NamespaceSymbol GlobalNamespace { get; }
    public ImmutableList<Declaration> Unbound { get; }

    private ImmutableList<Declaration>? _bound;
    public ImmutableList<Declaration> Bound
    {
        get
        {
            if (_bound == null)
            {
                var tmp = Unbound.Select(u => GetBound(u)).ToImmutableList();
                Interlocked.CompareExchange(ref _bound, tmp, null);
            }

            return _bound ?? ImmutableList<Declaration>.Empty;
        }
    }

    internal DeclarationBinding(
        DeclarationBinder binder,
        BindingScope defaultScope,
        NamespaceSymbol globalNamespace,
        ImmutableList<Declaration> unboundDeclarations)
    {
        _binder = binder;
        _defaultScope = defaultScope;
        GlobalNamespace = globalNamespace;
        Unbound = unboundDeclarations;
    }

    public static DeclarationBinding Create(
        IEnumerable<Declaration> declarations,
        IEnumerable<NamespaceSymbol> imports)
    {
        return new DeclarationBinder().Bind(declarations, imports);
    }

    private ImmutableDictionary<Declaration, Declaration> _unboundToBoundMap =
        ImmutableDictionary<Declaration, Declaration>.Empty;

    public Declaration GetBound(Declaration unboundDeclaration)
    {
        if (!_unboundToBoundMap.TryGetValue(unboundDeclaration, out var boundDeclaration))
        {
            var tmp = _binder.BindDeclaration(unboundDeclaration, _defaultScope);
            boundDeclaration = ImmutableInterlocked.GetOrAdd(ref _unboundToBoundMap, unboundDeclaration, tmp);
        }

        return boundDeclaration;
    }
}
