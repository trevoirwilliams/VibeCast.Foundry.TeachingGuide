namespace VibeCast.Application.Episodes;

public sealed record GenerateEpisodeConceptRequest(
    string Title,
    string? Description,
    string TargetAudience,
    string Objective,
    string Tone,
    string Language);