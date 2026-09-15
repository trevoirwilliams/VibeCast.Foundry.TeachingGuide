using System.ComponentModel.DataAnnotations;

namespace VibeCast.Infrastructure.Options;

public sealed class KnowledgeStorageOptions
{
    public const string SectionName = "KnowledgeStorage";

    [Required]
    public string ServiceUri { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = "vibecast-knowledge";
}
