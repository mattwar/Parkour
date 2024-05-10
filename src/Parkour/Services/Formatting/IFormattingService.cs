namespace Parkour.Services;

public interface IFormattingService
{
    FormattingResult Format(int start, int length, CancellationToken cancellationToken);
}
