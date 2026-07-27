namespace VibeCast.Application.Episodes;

public interface IEpisodeConceptGenerator
{
    Task<Guid> CreateAsync(
        CreateEpisodeRequest request,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<EpisodeConceptResult> GenerateAsync(
        GenerateEpisodeConceptRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(
        GenerateEpisodeConceptRequest request,
        CancellationToken cancellationToken = default);
}
