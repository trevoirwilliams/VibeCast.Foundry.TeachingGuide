namespace VibeCast.Application.Episodes;

public interface IEpisodeConceptGenerator
{
    Task<EpisodeConceptResult> GenerateAsync(
        GenerateEpisodeConceptRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(
        GenerateEpisodeConceptRequest request,
        CancellationToken cancellationToken = default);
}