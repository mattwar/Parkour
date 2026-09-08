namespace Parkour.Services;

public record CompletionResult(
    ImmutableList<CompletionItem> Items,
    CompletionDefaults? Defaults = null)
{
    public static CompletionResult Empty =
        new CompletionResult(
            ImmutableList<CompletionItem>.Empty);
}
