using System.ComponentModel.DataAnnotations;

namespace VibeCast.Infrastructure.Options;

public sealed class MediaStorageOptions
{
    public const string SectionName = "MediaStorage";

    [Required]
    [Url]
    public string ServiceUri { get; init; } = string.Empty;

    [Required]
    public string ContainerName { get; init; } = "vibecast-media";
}
