using VibeCast.Domain.Common;

namespace VibeCast.Domain.Media;

public enum MediaAssetStatus
{
    Uploaded = 0,
    Processing = 1,
    Ready = 2,
    Failed = 3,
    Validated = 4
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
    public string? ProposedAltText { get; private set; }
    public string? AcceptedAltText { get; private set; }
    public string? ArtworkSummary { get; private set; }
    public string? ArtworkVisibleText { get; private set; }
    public string? ArtworkPromptVersion { get; private set; }
    public DateTimeOffset? ArtworkAnalyzedAtUtc { get; private set; }
    public DateTimeOffset? ArtworkAcceptedAtUtc { get; private set; }

    public static MediaAsset Create(Guid? episodeId, string ownerId, string originalFileName, string storageKey, string contentType, long sizeBytes) =>
        new(episodeId, ownerId, originalFileName, storageKey, contentType, sizeBytes);

    public void SaveArtworkProposal(string altText,
        string visualSummary,
        string? visibleText,
        string promptVersion)
    {
        if (!IsSupportedArtwork())
        {
            throw new InvalidOperationException("Only PNG and JPEG assets can receive artwork descriptions.");
        }

        if (Status != MediaAssetStatus.Validated)
        {
            throw new InvalidOperationException("Only a validated asset can be analyzed.");
        }

        if (!string.IsNullOrWhiteSpace(AcceptedAltText))
        {
            throw new InvalidOperationException("An accepted artwork description cannot be " +
                "replaced by another generated proposal.");
        }

        ProposedAltText = altText.Trim();
        ArtworkSummary = visualSummary.Trim();
        ArtworkVisibleText =
            string.IsNullOrWhiteSpace(visibleText)
                ? null
                : visibleText.Trim();

        ArtworkPromptVersion = promptVersion.Trim();
        ArtworkAnalyzedAtUtc = DateTimeOffset.UtcNow;
        
        MarkUpdated();
    }

    public void AcceptArtworkDescription(string altText)
    {
        if (string.IsNullOrWhiteSpace(ProposedAltText))
        {
            throw new InvalidOperationException(
                "Artwork must be analyzed before its " +
                "description can be accepted.");
        }

        if (Status != MediaAssetStatus.Validated)
        {
            throw new InvalidOperationException(
                "Only a validated artwork proposal can " +
                "be accepted.");
        }

        AcceptedAltText = altText.Trim();
        ArtworkAcceptedAtUtc = DateTimeOffset.UtcNow;

        MarkReady();
    }

    private bool IsSupportedArtwork() =>
        ContentType.Equals(
            "image/png",
            StringComparison.OrdinalIgnoreCase) ||
        ContentType.Equals(
            "image/jpeg",
            StringComparison.OrdinalIgnoreCase);

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

    public void MarkValidated()
    {
        if (Status != MediaAssetStatus.Uploaded)
        {
            throw new InvalidOperationException("Only an uploaded asset can be validated.");
        }

        Status = MediaAssetStatus.Validated;
        MarkUpdated();
    }

}
