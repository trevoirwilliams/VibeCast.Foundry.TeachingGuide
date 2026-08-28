using System.ComponentModel.DataAnnotations;

namespace VibeCast.Infrastructure.Options;

public sealed class FoundryOptions
{
    public const string SectionName = "Foundry";

    [Required]
    [Url]
    public string ProjectEndpoint { get; init; } = string.Empty;

    [Required]
    public string ChatModelDeployment { get; init; } = string.Empty;

    [Required]
    public string ImageModelDeployment { get; init; } = string.Empty;

    public string? ApiKey { get; init; } = string.Empty;

    [Range(0, 5)]
    public int MaxRetries { get; init; } = 2;
}
