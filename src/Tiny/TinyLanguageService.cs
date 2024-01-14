using Parkour;
using Parkour.Binding;
using Parkour.Compilations;
using Parkour.Services;
using Parkour.Symbols;

namespace Tiny;

public class TinyLanguageService : CompilationLanguageService
{
    private readonly string _text;
    private readonly NamespaceSymbol _globalNamespace;

    public TinyLanguageService(
        string text,
        NamespaceSymbol? globalNamespace = null)
    {
        _text = text;
        _globalNamespace = globalNamespace ?? RuntimeSymbols.DefaultGlobalNamespace;
    }

    protected override Compilation CreateCompilation()
    {
        return new TinyCompilation(_text, _globalNamespace);
    }
}
