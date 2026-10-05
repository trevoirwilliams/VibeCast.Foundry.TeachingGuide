using VibeCast.Domain.Common;
using VibeCast.Domain.Episodes;

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

    public string? GenerationModelDeployment { get; private set; }
    public string? GenerationPromptVersion { get; private set; }
    public DateTimeOffset? GeneratedAtUtc { get; private set; }

    public bool IsGenerated => GeneratedAtUtc.HasValue;

    public string? TranscriptText { get; private set; }

    public string? TranscriptionLocale { get; private set; }

    public DateTimeOffset? TranscribedAtUtc { get; private set; }

    public bool HasTranscript => !string.IsNullOrWhiteSpace(TranscriptText);

    public IList<EpisodeSupportingSource> SupportingSources { get; private set; } = new List<EpisodeSupportingSource>();

    public static MediaAsset Create(Guid? episodeId, string ownerId, string originalFileName, string storageKey, string contentType, long sizeBytes) =>
        new(episodeId, ownerId, originalFileName, storageKey, contentType, sizeBytes);

    public void RecordGeneration(string modelDeployment,
        string promptVersion,
        DateTimeOffset generatedAtUtc)
    {
        if (Status != MediaAssetStatus.Validated)
        {
            throw new InvalidOperationException(
                "Generation metadata can only be recorded " +
                "for validated media.");
        }

        if (string.IsNullOrWhiteSpace(modelDeployment))
        {
            throw new ArgumentException(
                "The generation model deployment is required.",
                nameof(modelDeployment));
        }

        if (string.IsNullOrWhiteSpace(promptVersion))
        {
            throw new ArgumentException(
                "The generation prompt version is required.",
                nameof(promptVersion));
        }

        if (generatedAtUtc == default)
        {
            throw new ArgumentException(
                "The generation timestamp is required.",
                nameof(generatedAtUtc));
        }

        GenerationModelDeployment = modelDeployment.Trim();
        GenerationPromptVersion = promptVersion.Trim();
        GeneratedAtUtc = generatedAtUtc;

        MarkUpdated();
    }

    public void SaveArtworkProposal(string altText,
        string visualSummary,
        string? visibleText,
        string promptVersion)
    {
        if (!MediaAssetHelpers.IsSupportedArtwork(ContentType))
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

    public void SaveTranscript(
        string transcriptText,
        string locale,
        DateTimeOffset transcribedAtUtc)
    {
        if (!MediaAssetHelpers.IsAudio(ContentType))
        {
            throw new InvalidOperationException(
                "Only audio assets can receive transcripts.");
        }

        if (Status != MediaAssetStatus.Validated &&
            Status != MediaAssetStatus.Ready)
        {
            throw new InvalidOperationException(
                "Only validated audio can be transcribed.");
        }

        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            throw new ArgumentException(
                "Transcript text is required.",
                nameof(transcriptText));
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new ArgumentException(
                "The transcription locale is required.",
                nameof(locale));
        }

        if (transcribedAtUtc == default)
        {
            throw new ArgumentException(
                "The transcription timestamp is required.",
                nameof(transcribedAtUtc));
        }

        TranscriptText = transcriptText.Trim();
        TranscriptionLocale = locale.Trim();
        TranscribedAtUtc = transcribedAtUtc;

        MarkUpdated();
    }

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
