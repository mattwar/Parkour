namespace Parkour.Symbols;

public class CombinedSymbols
{
    /// <summary>
    /// Creates a new global namespace from a set of global namespaces.
    /// </summary>
    public static GlobalNamespaceSymbol Create(
        ImmutableList<GlobalNamespaceSymbol> globalNamespaces)
    {
        return Create(ns => globalNamespaces);
    }

    /// <summary>
    /// Creates a new global namespace from a set of global namespaces.
    /// </summary>
    public static GlobalNamespaceSymbol Create(
        Func<NamespaceSymbol, ImmutableList<GlobalNamespaceSymbol>> fnCreateGlobalNamespaces)
    {
        return new GlobalNamespaceSymbol(
            ns =>
            {
                var globalNamespaces = fnCreateGlobalNamespaces(ns);

                var globalNamespaceMembers = globalNamespaces.SelectMany(s =>
                    s is NamespaceSymbol ns && ns.Name == ""
                        ? (IEnumerable<Symbol>)ns.Members
                        : new[] { s })
                    .ToList();

                return CombineMembers(ns, globalNamespaceMembers);
            });
    }
        
    private static ImmutableList<Symbol> CombineMembers(NamespaceSymbol container, IEnumerable<Symbol> members)
    {
        var newMembers = new List<Symbol>();

        var namespaceMembers = members.OfType<NamespaceSymbol>();
        var namespaceGroups = namespaceMembers.GroupBy(s => s.Name);
        var combinedNamespaces = namespaceGroups.Select(g =>
            new NamespaceSymbol(g.Key, container, _ns => CombineMembers(_ns, g.SelectMany(n => n.Members))))
            .ToList();
        newMembers.AddRange(combinedNamespaces);

        var otherMembers = members.Where(m => !(m is NamespaceSymbol)).ToList();
        newMembers.AddRange(otherMembers);

        return newMembers.ToImmutableList();
    }
}
