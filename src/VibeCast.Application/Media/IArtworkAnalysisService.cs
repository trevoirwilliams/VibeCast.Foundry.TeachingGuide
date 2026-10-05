namespace VibeCast.Application.Media;

public interface IArtworkAnalysisService
{
    Task<ArtworkAnalysisResult> AnalyzeAsync(
        AnalyzeArtworkRequest request,
        Stream content,
        CancellationToken cancellationToken = default);
}
