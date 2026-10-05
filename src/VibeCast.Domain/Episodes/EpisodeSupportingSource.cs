using VibeCast.Domain.Common;
using VibeCast.Domain.Media;

namespace VibeCast.Domain.Episodes;

public sealed class EpisodeSupportingSource : Entity
{
    private EpisodeSupportingSource()
    {
    }

    private EpisodeSupportingSource(
        Guid episodeId,
        Guid mediaAssetId,
        string ownerId,
        string summary,
        string relevanceRationale,
        string relevantPointsJson,
        string matchedEvidenceRequirementsJson,
        string analyzerId,
        string relevancePromptVersion,
        DateTimeOffset analyzedAtUtc)
    {
        if (episodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid episode identifier is required.",
                nameof(episodeId));
        }

        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid media asset identifier is required.",
                nameof(mediaAssetId));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An owner is required.",
                nameof(ownerId));
        }

        EpisodeId = episodeId;
        MediaAssetId = mediaAssetId;
        OwnerId = ownerId.Trim();

        SetAnalysis(
            summary,
            relevanceRationale,
            relevantPointsJson,
            matchedEvidenceRequirementsJson,
            analyzerId,
            relevancePromptVersion,
            analyzedAtUtc);
    }

    public Guid EpisodeId { get; private set; }

    public Guid MediaAssetId { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    public string Summary { get; private set; } = string.Empty;

    public string RelevanceRationale { get; private set; } = string.Empty;

    public string RelevantPointsJson { get; private set; } = "[]";

    public string MatchedEvidenceRequirementsJson { get; private set; } = "[]";

    public string AnalyzerId { get; private set; } = string.Empty;

    public string RelevancePromptVersion
    {
        get;
        private set;
    } = string.Empty;

    public DateTimeOffset AnalyzedAtUtc
    {
        get;
        private set;
    }


    public Episode? Episode { get; private set; }
    public MediaAsset? MediaAsset { get; private set; }

    public static EpisodeSupportingSource Create(
        Guid episodeId,
        Guid mediaAssetId,
        string ownerId,
        string summary,
        string relevanceRationale,
        string relevantPointsJson,
        string matchedEvidenceRequirementsJson,
        string analyzerId,
        string relevancePromptVersion,
        DateTimeOffset analyzedAtUtc) =>
        new(
            episodeId,
            mediaAssetId,
            ownerId,
            summary,
            relevanceRationale,
            relevantPointsJson,
            matchedEvidenceRequirementsJson,
            analyzerId,
            relevancePromptVersion,
            analyzedAtUtc);

    public void RefreshAnalysis(
        string summary,
        string relevanceRationale,
        string relevantPointsJson,
        string matchedEvidenceRequirementsJson,
        string analyzerId,
        string relevancePromptVersion,
        DateTimeOffset analyzedAtUtc)
    {
        SetAnalysis(
            summary,
            relevanceRationale,
            relevantPointsJson,
            matchedEvidenceRequirementsJson,
            analyzerId,
            relevancePromptVersion,
            analyzedAtUtc);

        MarkUpdated();
    }

    private void SetAnalysis(
        string summary,
        string relevanceRationale,
        string relevantPointsJson,
        string matchedEvidenceRequirementsJson,
        string analyzerId,
        string relevancePromptVersion,
        DateTimeOffset analyzedAtUtc)
    {
        Summary = RequireText(summary, nameof(summary));

        RelevanceRationale = RequireText(
                relevanceRationale,
                nameof(relevanceRationale));

        RelevantPointsJson = RequireText(
                relevantPointsJson,
                nameof(relevantPointsJson));

        MatchedEvidenceRequirementsJson = RequireText(
                matchedEvidenceRequirementsJson,
                nameof(matchedEvidenceRequirementsJson));

        AnalyzerId = RequireText(
                analyzerId,
                nameof(analyzerId));

        RelevancePromptVersion = RequireText(
                relevancePromptVersion,
                nameof(relevancePromptVersion));

        if (analyzedAtUtc == default)
        {
            throw new ArgumentException(
                "The analysis timestamp is required.",
                nameof(analyzedAtUtc));
        }

        AnalyzedAtUtc = analyzedAtUtc;
    }

    private static string RequireText(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                parameterName);
        }

        return value.Trim();
    }
}
