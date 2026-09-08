using System.Runtime.CompilerServices;

namespace Parkour.Services;

public class LanguageServices 
    : ILanguage,
      ILanguageServiceFactory,
      ICompilationServiceFactoryLanguageService,
      IClassificationLanguageService
{
    public string LanguageName { get; }

    protected LanguageServices(string languageName)
    {
        this.LanguageName = languageName;
    }

    static LanguageServices()
    {
        System.Diagnostics.Debug.Assert(DefaultClassificationKinds.Select(k => k.ToString()).ToHashSet().Count == DefaultClassificationKinds.Count,
            "DefaultClassificationKinds contains duplicate entries.");

        System.Diagnostics.Debug.Assert(DefaultClassificationModifiers.Select(m => m.ToString()).ToHashSet().Count == DefaultClassificationModifiers.Count,
            "DefaultClassificationModifiers contains duplicate entries.");
    }

    ILanguage ILanguageService.Language => this;

    ILanguageServiceFactory ILanguage.Factory => this;

    string ILanguage.Name => this.LanguageName;

    public bool TryGetLanguageService<TService>(
        [NotNullWhen(true)] out TService? service)
        where TService : class, ILanguageService
    {
        if (this is TService langService)
        {
            service = langService;
            return true;
        }
        service = null;
        return false;
    }

    /// <summary>
    /// Gets the compilation service factory for the given compilation.
    /// </summary>
    public ICompilationServiceFactory GetCompilationServiceFactory(ICompilation compilation)
    {
        if (!_compilationToFactoryMap.TryGetValue(compilation, out var factory))
        {
            factory = new CompilationServices(this, compilation);
            _compilationToFactoryMap.Add(compilation, factory);
        }
        return factory;
    }

    private readonly ConditionalWeakTable<ICompilation, ICompilationServiceFactory> _compilationToFactoryMap =
        new();

    /// <summary>
    /// Override this to provide a more specific document service factory for the given document.
    /// </summary>
    protected virtual ICompilationServiceFactory CreateCompilationServiceFactory(ICompilation compilation)
    {
        return new CompilationServices(this, compilation);
    }

    /// <summary>
    /// Returns the <see cref="ClassificationKind"/> supported by this language.
    /// </summary>
    public virtual ImmutableList<ClassificationKind> SupportedClassificationKinds => DefaultClassificationKinds;

    /// <summary>
    /// Set of known possible classification kinds.
    /// These are the kinds that the language will claim to support if not overridden.
    /// </summary>
    public static ImmutableList<ClassificationKind> DefaultClassificationKinds { get; } = [
        // LSP token types
        ClassificationKind.Namespace(),
        ClassificationKind.Type(),
        ClassificationKind.Class(),
        ClassificationKind.Enum(),
        ClassificationKind.Interface(),
        ClassificationKind.Struct(),
        ClassificationKind.TypeParameter(),
        ClassificationKind.Parameter(),
        ClassificationKind.Variable(),
        ClassificationKind.Property(),
        ClassificationKind.EnumMember(),
        ClassificationKind.Event(),
        ClassificationKind.Function(),
        ClassificationKind.Method(),
        ClassificationKind.Macro(),
        ClassificationKind.Keyword(),
        ClassificationKind.Modifier(),
        ClassificationKind.Comment(),
        ClassificationKind.String(),
        ClassificationKind.Number(),
        ClassificationKind.RegExp(),
        ClassificationKind.Operator(),
        ClassificationKind.Decorator(),

        // Additional kinds
        ClassificationKind.Field(),
        ClassificationKind.Text(),
        ClassificationKind.Boolean(),
        ClassificationKind.Type(),
        ClassificationKind.Punctuation(),
        ClassificationKind.Trivia(),
        ClassificationKind.Literal(),
        ClassificationKind.Namespace(),
        ClassificationKind.Label(),
    ];

    /// <summary>
    /// The classification modifiers supported by this language.
    /// </summary>
    public virtual ImmutableList<ClassificationModifier> SupportedClassificationModifiers => DefaultClassificationModifiers;

    public static ImmutableList<ClassificationModifier> DefaultClassificationModifiers { get; } = [
        ClassificationModifier.Declaration(),
        ClassificationModifier.Definition(),
        ClassificationModifier.ReadOnly(),
        ClassificationModifier.Static(),
        ClassificationModifier.Deprecated(),
        ClassificationModifier.Abstract(),
        ClassificationModifier.Async(),
        ClassificationModifier.Modification(),
        ClassificationModifier.Documentation(),
        ClassificationModifier.DefaultLibrary()
    ];
}