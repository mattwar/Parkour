namespace Parkour.Analysis;
using Symbols;
using Syntax;

public abstract class Compilation
{
    public abstract NamespaceSymbol GlobalNamespace { get; }
    public abstract ImmutableList<Diagnostic> GetDiagnostics();
}