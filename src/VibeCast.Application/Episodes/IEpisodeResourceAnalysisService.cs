namespace VibeCast.Application.Episodes;

public interface IEpisodeResourceAnalysisService
{
    Task<EpisodeResourceAnalysisResult> AnalyzeAsync(
        Guid episodeId,
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken = default);
}
