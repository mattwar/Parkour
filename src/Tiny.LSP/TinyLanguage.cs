using Parkour;
using Parkour.Reflection;
using Parkour.Services;
using Parkour.LSP;
using System.Collections.Immutable;

namespace Tiny.LSP;

internal class TinyLanguage : ParkourLanguage
{
    public override string LanguageId => "Tiny";
    public override string DocumentPattern => "**/*.ty";

    public override ICompilation CreateCompilation(ImmutableList<ISourceDocument> documents)
    {
        return new TinyCompilation(documents[0], ReflectionSymbols.CurrentMscorlib);
    }

    public override IDocumentServiceFactory CreateDocumentServiceFactory(ICompilation compilation, ISourceDocument document)
    {
        return new TinyServices(compilation, document);
    }
}
