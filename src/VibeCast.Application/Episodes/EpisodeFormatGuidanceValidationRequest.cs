namespace VibeCast.Application.Episodes;

public sealed record EpisodeFormatGuidanceValidationRequest(
    EpisodePlan Plan,
    EpisodeFormatGuidance Guidance);
