namespace Parkour.Services;

public abstract class LanguageService
{
    public virtual CompletionService Completion =>
        CompletionService.NotSupported;
}
