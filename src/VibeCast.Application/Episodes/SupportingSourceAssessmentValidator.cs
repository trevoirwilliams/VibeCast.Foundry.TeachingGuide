using VibeCast.Application.Validation;

namespace VibeCast.Application.Episodes;

public sealed class SupportingSourceAssessmentValidator
    : IValidator<SupportingSourceAssessmentValidationRequest>
{
    public const int MaximumSummaryLength = 2_000;
    public const int MaximumRationaleLength = 1_000;
    public const int MaximumPointLength = 600;
    public const int MaximumRelevantPoints = 5;

    public ValidationResult Validate(SupportingSourceAssessmentValidationRequest instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(instance.Assessment);

        ValidationResult result = new();

        SupportingSourceAssessment assessment =
            instance.Assessment;

        if (string.IsNullOrWhiteSpace(assessment.Rationale) || assessment.Rationale.Trim().Length >
            MaximumRationaleLength)
        {
            result.Add(
                nameof(assessment.Rationale),
                $"A relevance rationale between 1 and " +
                $"{MaximumRationaleLength} characters is required.");
        }

        if (assessment.Summary?.Trim().Length > MaximumSummaryLength)
        {
            result.Add(
                nameof(assessment.Summary),
                $"The summary cannot exceed " +
                $"{MaximumSummaryLength} characters.");
        }

        string[] relevantPoints = assessment.RelevantPoints ?? [];

        string[] matchedRequirements = assessment.MatchedEvidenceRequirements ?? [];

        if (relevantPoints.Length > MaximumRelevantPoints)
        {
            result.Add(
                nameof(assessment.RelevantPoints),
                $"No more than {MaximumRelevantPoints} " +
                "relevant points are allowed.");
        }

        if (relevantPoints.Any(point => string.IsNullOrWhiteSpace(point) || point.Trim().Length > MaximumPointLength))
        {
            result.Add(
                nameof(assessment.RelevantPoints),
                "Relevant points must contain valid bounded text.");
        }

        if (assessment.IsRelevant)
        {
            if (string.IsNullOrWhiteSpace(assessment.Summary))
            {
                result.Add(
                    nameof(assessment.Summary),
                    "A relevant resource requires a summary.");
            }

            if (relevantPoints.Length == 0)
            {
                result.Add(
                    nameof(assessment.RelevantPoints),
                    "A relevant resource requires at least " +
                    "one supporting point.");
            }
        }
        else if (relevantPoints.Length > 0 || matchedRequirements.Length > 0)
        {
            result.Add(
                nameof(assessment.IsRelevant),
                "An irrelevant resource cannot contain " +
                "accepted supporting evidence.");
        }

        HashSet<string> allowedRequirements = instance.AllowedEvidenceRequirements
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string matchedRequirement in matchedRequirements)
        {
            if (!allowedRequirements.Contains(matchedRequirement))
            {
                result.Add(nameof(assessment.MatchedEvidenceRequirements),
                    "The assessment referenced an evidence " +
                    "requirement that does not exist in the " +
                    "accepted episode plan.");
            }
        }

        return result;
    }
}
