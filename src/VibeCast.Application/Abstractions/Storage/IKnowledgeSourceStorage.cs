namespace VibeCast.Application.Abstractions.Storage;

public interface IKnowledgeSourceStorage
{
    Task<StoredKnowledgeSource> SaveAsync(
        Guid mediaAssetId,
        string ownerId,
        string originalFileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid mediaAssetId,
        string ownerId,
        string originalFileName,
        CancellationToken cancellationToken = default);

    Uri GetUri(
        Guid mediaAssetId,
        string ownerId,
        string originalFileName);
}

public sealed record StoredKnowledgeSource(
    string StorageKey,
    Uri BlobUri);
