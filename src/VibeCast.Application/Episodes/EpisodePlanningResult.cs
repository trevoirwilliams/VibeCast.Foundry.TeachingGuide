namespace VibeCast.Application.Episodes;

public sealed record EpisodePlanningResult(
    EpisodePlan Plan,
    string PromptVersion,
    DateTimeOffset GeneratedAtUtc,
    bool RepairAttempted,
    string? RepairPromptVersion,
    string? FormatPolicyVersion = null);
