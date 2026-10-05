using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Infrastructure.Storage;

public sealed class LocalBlobStorage(IOptions<BlobStorageOptions> options) : IBlobStorage
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<StoredBlob> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        Directory.CreateDirectory(_rootPath);

        var extension = Path.GetExtension(Path.GetFileName(originalFileName));
        var key = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";
        var fullPath = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);
        return new StoredBlob(key, Path.GetFileName(originalFileName), contentType, target.Length);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(ResolvePath(storageKey));
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));
        if (!fullPath.StartsWith(_rootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The blob key resolves outside the configured storage root.");
        }

        return fullPath;
    }
}
