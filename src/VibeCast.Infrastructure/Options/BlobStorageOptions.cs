using System.ComponentModel.DataAnnotations;

namespace VibeCast.Infrastructure.Options;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    [Required]
    public string RootPath { get; set; } = ".vibecast/blobs";
}

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    [Range(1, 10_000)]
    public int Capacity { get; set; } = 100;
}
