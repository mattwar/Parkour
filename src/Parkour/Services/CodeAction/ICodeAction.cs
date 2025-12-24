namespace Parkour.Services;

public interface ICodeAction
{
    string Title { get; }
    string Description { get; }
}

public interface IGroupedActions : ICodeAction
{
    ImmutableList<ICodeAction> Actions { get; }
}
