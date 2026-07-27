using VibeCast.Domain.Episodes;

namespace VibeCast.Application.Episodes;

public sealed record EpisodeDetails(
    Guid Id,
    string Title,
    string? Description,
    string TargetAudience,
    string Objective,
    string Tone,
    string Language,
    DateOnly? PlannedPublishDate,
    EpisodeStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
