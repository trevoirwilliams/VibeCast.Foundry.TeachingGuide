using System.ComponentModel.DataAnnotations;

namespace VibeCast.Application.Episodes;

public sealed class CreateEpisodeRequest
{
    [Required, StringLength(160, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2_000)]
    public string? Description { get; set; }

    [Required, StringLength(160, MinimumLength = 3)]
    public string TargetAudience { get; set; } = string.Empty;

    [Required, StringLength(600, MinimumLength = 3)]
    public string Objective { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Tone { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Language { get; set; } = string.Empty;

    public DateOnly? PlannedPublishDate { get; set; }

    public DateTimeOffset? ScheduledForUtc { get; set; }
}
