namespace VibeCast.Application.Episodes;

public sealed record GenerateEpisodePlanRequest(
    Guid EpisodeId,
    string Title,
    string? Description,
    string TargetAudience,
    string Objective,
    string Tone,
    string Language,
    DateOnly? PlannedPublishDate);
