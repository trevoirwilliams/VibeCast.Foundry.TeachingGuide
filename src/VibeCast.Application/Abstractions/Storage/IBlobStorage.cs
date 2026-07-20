namespace VibeCast.Application.Abstractions.Storage;

public interface IBlobStorage
{
    Task<StoredBlob> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record StoredBlob(string StorageKey, string OriginalFileName, string ContentType, long SizeBytes);
