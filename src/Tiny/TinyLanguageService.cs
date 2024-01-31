using Parkour;
using Parkour.Services;
using Parkour.Symbols;

namespace Tiny;

public class TinyLanguageService
{
    public static LanguageService Create(string text, GlobalNamespaceSymbol externalSymbols)
    {
        var compilation = new TinyCompilation(text, externalSymbols);
        var document = compilation.Documents[0];
        return new CompilationLanguageService(document, () => compilation);
    }
}
