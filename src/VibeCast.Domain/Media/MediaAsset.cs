using VibeCast.Domain.Common;

namespace VibeCast.Domain.Media;

public enum MediaAssetStatus
{
    Uploaded = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3
}

public sealed class MediaAsset : Entity
{
    private MediaAsset() { }

    private MediaAsset(Guid? episodeId, string ownerId, string originalFileName, string storageKey, string contentType, long sizeBytes)
    {
        EpisodeId = episodeId;
        OwnerId = ownerId;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    public Guid? EpisodeId { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public MediaAssetStatus Status { get; private set; } = MediaAssetStatus.Uploaded;

    public static MediaAsset Create(Guid? episodeId, string ownerId, string originalFileName, string storageKey, string contentType, long sizeBytes) =>
        new(episodeId, ownerId, originalFileName, storageKey, contentType, sizeBytes);

    public void MarkProcessing()
    {
        Status = MediaAssetStatus.Processing;
        MarkUpdated();
    }

    public void MarkReady()
    {
        Status = MediaAssetStatus.Ready;
        MarkUpdated();
    }

    public void MarkFailed()
    {
        Status = MediaAssetStatus.Failed;
        MarkUpdated();
    }
}
