namespace Parkour.Services;

public record CompletionResult(ImmutableList<CompletionItem> Completions)
{
    public static CompletionResult Empty =
        new CompletionResult(ImmutableList<CompletionItem>.Empty);
}