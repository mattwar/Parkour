namespace Parkour.Services;

public interface ILanguage
{
    /// <summary>
    /// The name of the language.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The factory for creating language services.
    /// </summary>
    ILanguageServiceFactory Factory { get; }
}