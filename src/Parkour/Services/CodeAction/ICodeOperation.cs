using Parkour.Text;

namespace Parkour.Services;

public interface ICodeOperation
{
}

public interface ITextChangedOperation : ICodeOperation
{
    public ISourceDocument Document { get; }
    public EditString ChangedText { get; }
}

public interface  ICursorPositionOperation : ICodeOperation
{
    public ISourceDocument Document { get; }
    public int NewPosition { get; }
}
