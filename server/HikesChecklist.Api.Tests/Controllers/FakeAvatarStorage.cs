using HikesChecklist.Api.Services;

namespace HikesChecklist.Api.Tests.Controllers;

public class FakeAvatarStorage : IAvatarStorage
{
    private readonly Dictionary<string, byte[]> _files = new();

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    public Task SaveAsync(string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        content.CopyTo(memory);
        _files[fileName] = memory.ToArray();
        return Task.CompletedTask;
    }

    public Task<Stream?> ReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        Stream? result = _files.TryGetValue(fileName, out var bytes) ? new MemoryStream(bytes) : null;
        return Task.FromResult(result);
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        _files.Remove(fileName);
        return Task.CompletedTask;
    }
}
