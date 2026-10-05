namespace VibeCast.Application.Episodes;

public sealed record EpisodeFormatGuidanceContext(
    string TargetAudience,
    string Tone,
    DateTimeOffset PlanningTimeUtc);

public sealed record EpisodeFormatGuidance(
    string PolicyVersion,
    int TargetDurationMinutes,
    string PacingGuidance,
    string Rationale);
