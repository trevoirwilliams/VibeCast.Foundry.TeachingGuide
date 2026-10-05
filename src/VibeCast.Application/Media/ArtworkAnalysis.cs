using VibeCast.Application.Validation;
using VibeCast.Domain.Media;

namespace VibeCast.Application.Media;

public sealed record AnalyzeArtworkRequest(
    string EpisodeTitle,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);

public sealed record ArtworkAnalysis(
    string AltText,
    string VisualSummary,
    string? VisibleText);

public sealed record ArtworkAnalysisResult(
    ArtworkAnalysis Analysis,
    string PromptVersion,
    DateTimeOffset GeneratedAtUtc);

public sealed record ArtworkWorkspace(
    Guid MediaAssetId,
    Guid? EpisodeId,
    string EpisodeTitle,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    MediaAssetStatus MediaStatus,
    string? ProposedAltText,
    string? AcceptedAltText,
    string? VisualSummary,
    string? VisibleText,
    string? PromptVersion,
    DateTimeOffset? AnalyzedAtUtc,
    DateTimeOffset? AcceptedAtUtc)
{
    public bool HasProposal =>
        !string.IsNullOrWhiteSpace(ProposedAltText);

    public bool IsAccepted =>
        !string.IsNullOrWhiteSpace(AcceptedAltText);

    public string ReviewStatus =>
        IsAccepted
            ? "Accepted"
            : HasProposal
                ? "Awaiting review"
                : "Not analyzed";
}

public sealed record ArtworkContent(
    Stream Content,
    string ContentType,
    string OriginalFileName);

public sealed class ArtworkAnalysisValidator : IValidator<ArtworkAnalysis>
{
    public const int MaximumAltTextLength = 150;
    public const int MaximumSummaryLength = 600;
    public const int MaximumVisibleTextLength = 500;

    private static readonly string[] DisallowedPrefixes =
    [
        "image of",
        "picture of",
        "photo of",
        "graphic of"
    ];

    public ValidationResult Validate(ArtworkAnalysis instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        ValidationResult result = new();

        ValidationResult altTextResult =
            ValidateAltText(instance.AltText);

        foreach (ValidationFailure failure
                 in altTextResult.Errors)
        {
            result.Add(
                failure.PropertyName,
                failure.ErrorMessage);
        }

        if (string.IsNullOrWhiteSpace(instance.VisualSummary))
        {
            result.Add(nameof(instance.VisualSummary),
                "A visual summary is required.");
        }
        else if ( instance.VisualSummary.Trim().Length > MaximumSummaryLength)
        {
            result.Add(
                nameof(instance.VisualSummary),
                $"The visual summary cannot exceed " +
                $"{MaximumSummaryLength} characters.");
        }

        if ((instance.VisibleText?.Trim().Length ?? 0) > MaximumVisibleTextLength)
        {
            result.Add(
                nameof(instance.VisibleText),
                $"Visible text cannot exceed " +
                $"{MaximumVisibleTextLength} characters.");
        }

        return result;
    }

    public ValidationResult ValidateAltText(string? altText)
    {
        ValidationResult result = new();

        if (string.IsNullOrWhiteSpace(altText))
        {
            result.Add(
                nameof(ArtworkAnalysis.AltText),
                "Alternative text is required.");

            return result;
        }

        string normalized = altText.Trim();

        if (normalized.Length > MaximumAltTextLength)
        {
            result.Add(
                nameof(ArtworkAnalysis.AltText),
                $"Alternative text cannot exceed " +
                $"{MaximumAltTextLength} characters.");
        }

        if (normalized.Contains('\r') || normalized.Contains('\n'))
        {
            result.Add(
                nameof(ArtworkAnalysis.AltText),
                "Alternative text must be one concise line.");
        }

        if (DisallowedPrefixes.Any(
                prefix =>
                    normalized.StartsWith(
                        prefix,
                        StringComparison.OrdinalIgnoreCase)))
        {
            result.Add(
                nameof(ArtworkAnalysis.AltText),
                "Start with the meaningful subject, not " +
                "a phrase such as 'image of'.");
        }

        return result;
    }
}
