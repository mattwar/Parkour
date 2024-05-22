namespace Parkour.Services;

public interface IFormattingService
{
    FormattingResult Format(
        int start, 
        int length,
        ServiceOptions options,
        CancellationToken cancellationToken);
}
