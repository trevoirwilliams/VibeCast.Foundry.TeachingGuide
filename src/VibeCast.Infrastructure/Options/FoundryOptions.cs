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

    [Range(5, 300)]
    public int ChatTimeoutSeconds { get; init; } = 60;

    [Range(1, 32)]
    public int MaxConcurrentChatRequests { get; init; } = 4;

    [Range(0, 100)]
    public int ChatQueueLimit { get; init; } = 8;
}
