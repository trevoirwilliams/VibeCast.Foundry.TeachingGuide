namespace VibeCast.Application.Episodes;

public sealed record SupportingSourceAssessment(
    bool IsRelevant,
    string Rationale,
    string Summary,
    string[] RelevantPoints,
    string[] MatchedEvidenceRequirements);

public sealed record SupportingSourceAssessmentValidationRequest(
    SupportingSourceAssessment Assessment,
    IReadOnlyList<string> AllowedEvidenceRequirements);

public sealed record EpisodeSupportingSourceSummary(
    Guid Id,
    Guid MediaAssetId,
    string SourceFileName,
    string ContentType,
    string Summary,
    string Rationale,
    IReadOnlyList<string> RelevantPoints,
    IReadOnlyList<string> MatchedEvidenceRequirements,
    string AnalyzerId,
    string RelevancePromptVersion,
    DateTimeOffset AnalyzedAtUtc);

public sealed record EpisodeResourceAnalysisResult(
    bool IsRelevant,
    string Message,
    EpisodeSupportingSourceSummary? SupportingSource);
