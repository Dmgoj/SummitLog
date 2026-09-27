namespace SummitLog.Api.Services;

public interface IAvatarStorage
{
    Task SaveAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> ReadAsync(string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileName, CancellationToken cancellationToken = default);
}
