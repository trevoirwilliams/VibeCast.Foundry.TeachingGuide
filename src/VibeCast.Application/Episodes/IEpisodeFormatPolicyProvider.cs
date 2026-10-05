namespace VibeCast.Application.Episodes;

public interface IEpisodeFormatPolicyProvider
{
    Task<EpisodeFormatGuidance> GetCurrentAsync(
        EpisodeFormatGuidanceContext context,
        CancellationToken cancellationToken = default);
}
