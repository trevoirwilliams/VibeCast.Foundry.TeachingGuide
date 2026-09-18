namespace VibeCast.Application.Knowledge;

public interface IGroundedBlogGenerationService
{
    Task<GroundedBlogDraft> GenerateAsync(
        GenerateGroundedBlogRequest request,
        string ownerId,
        CancellationToken cancellationToken = default);
}

public sealed record GenerateGroundedBlogRequest(IReadOnlyCollection<Guid> SourceIds);

public sealed class GroundedBlogDraft
{
    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Introduction { get; set; } = string.Empty;

    public GroundedBlogSection[] Sections { get; set; } = [];

    public string Conclusion { get; set; } = string.Empty;

    public string[] KeyTakeaways { get; set; } = [];

    public string[] SourceReferenceIds { get; set; } = [];
}

public sealed class GroundedBlogSection
{
    public string Heading { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}
