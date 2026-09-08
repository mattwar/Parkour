namespace Parkour.Services;

public interface IClassificationLanguageService : ILanguageService
{
    /// <summary>
    /// Gets the list of all <see cref="ClassificationKind"/> produced by the language.
    /// </summary>
    ImmutableList<ClassificationKind> SupportedClassificationKinds { get; }

    /// <summary>
    /// Gets the list of all <see cref="ClassificationModifier"/> produced by the language.
    /// </summary>
    ImmutableList<ClassificationModifier> SupportedClassificationModifiers { get; }
}