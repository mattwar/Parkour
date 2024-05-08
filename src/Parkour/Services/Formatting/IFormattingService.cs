namespace Parkour.Services;

public interface IFormattingService
{
    Task<FormattingResult> FormatAsync(int start, int length, CancellationToken cancellationToken);
}
