using System.ComponentModel.DataAnnotations;

namespace VibeCast.Infrastructure.Options;

public sealed class ContentUnderstandingOptions
{
    public const string SectionName = "ContentUnderstanding";

    [Required]
    [Url]
    public string Endpoint { get; init; } = string.Empty;

    public string? ApiKey { get; init; }
}
