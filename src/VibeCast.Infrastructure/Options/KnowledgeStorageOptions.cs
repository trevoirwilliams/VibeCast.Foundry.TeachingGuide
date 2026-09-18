using System.ComponentModel.DataAnnotations;

namespace VibeCast.Infrastructure.Options;

public sealed class KnowledgeStorageOptions
{
    public const string SectionName = "KnowledgeStorage";

    [Required]
    public string ServiceUri { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = "vibecast-knowledge";

    [Required]
    public string SearchEndpoint { get; set; } = string.Empty;

    [Required]
    public string KnowledgeBaseName { get; set; } = "vibecast-kb";

    [Required]
    public string KnowledgeSourceName { get; set; } = "vibecast-index-ks";

    [Required]
    public string SourcePathField { get; set; } = "blob_url";
}
