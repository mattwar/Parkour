namespace Parkour.Semantics;

using Symbols;

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

    public abstract Symbol? DeclaredSymbol { get; }

    /// <summary>
    /// Builds a map between symbols and their declarations
    /// </summary>
    public static ImmutableDictionary<Symbol, ImmutableList<Declaration>> BuildSymbolToDeclarationMap(
        ImmutableList<Declaration> declarations)
    {
        var map = new Dictionary<Symbol, List<Declaration>>();
        foreach (var decl in declarations)
        {
            BuildMap(decl);
        }

        var id = map.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableList());
        return id;

        void BuildMap(Declaration declaration)
        {
            if (declaration.DeclaredSymbol is Symbol symbol)
            {
                if (!map.TryGetValue(symbol, out var list))
                {
                    list = new List<Declaration>();
                    map.Add(symbol, list);
                }

                list.Add(declaration);
            }

            for (int i = 0; i < declaration.ChildCount; i++)
            {
                var child = declaration.GetChild(i);
                if (child is Declaration childDeclaration)
                    BuildMap(childDeclaration);
            }
        }
    }
}