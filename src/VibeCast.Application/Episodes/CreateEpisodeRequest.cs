using System.ComponentModel.DataAnnotations;

namespace VibeCast.Application.Episodes;

public sealed class CreateEpisodeRequest
{
    [Required, StringLength(160, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2_000)]
    public string? Description { get; set; }

    public DateTimeOffset? ScheduledForUtc { get; set; }
}
