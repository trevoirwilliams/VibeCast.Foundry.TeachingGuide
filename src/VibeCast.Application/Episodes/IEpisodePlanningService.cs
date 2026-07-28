namespace VibeCast.Application.Episodes;

public interface IEpisodePlanningService
{
    Task<EpisodePlanningResult> GenerateAsync(
        GenerateEpisodePlanRequest request,
        CancellationToken cancellationToken = default);
}
