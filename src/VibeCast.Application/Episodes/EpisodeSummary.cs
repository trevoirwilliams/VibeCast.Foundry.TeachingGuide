using VibeCast.Domain.Episodes;

namespace VibeCast.Application.Episodes;

public sealed record EpisodeSummary(
    Guid Id,
    string Title,
    string? Description,
    string TargetAudience,
    EpisodeStatus Status,
    DateTimeOffset UpdatedAtUtc);
