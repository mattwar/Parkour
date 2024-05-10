namespace Parkour.Services;

public interface ICodeActionService : IDocumentService
{
    /// <summary>
    /// Gets the list of <see cref="ICodeAction"/> that can be applied at this position.
    /// These are used to form a menu for the user to choose.
    /// </summary>
    CodeActionResult GetActions(int position, CancellationToken cancellationToken);

    /// <summary>
    /// Get the list of operations to be applied in the text editor to perform the code action.
    /// </summary>
    CodeOperationResult GetOperations(ICodeAction action, CancellationToken cancellationToken);
}

public record CodeActionResult(ImmutableList<ICodeAction> Actions)
{
    public static CodeActionResult Empty =
        new CodeActionResult(ImmutableList<ICodeAction>.Empty);
}

public record CodeOperationResult(ImmutableList<ICodeOperation> Operations)
{
    public static CodeOperationResult Empty =
        new CodeOperationResult(ImmutableList<ICodeOperation>.Empty);
}