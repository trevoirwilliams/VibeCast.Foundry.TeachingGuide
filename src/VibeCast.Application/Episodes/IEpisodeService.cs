using System;
using System.Collections.Generic;
using System.Text;

namespace VibeCast.Application.Episodes;

public interface IEpisodeService
{
    Task<IReadOnlyList<EpisodeSummary>> ListAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
    CreateEpisodeRequest request,
    string ownerId,
    CancellationToken cancellationToken = default);

    Task<EpisodeDetails?> GetAsync(
        Guid episodeId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task SavePlanAsync(
        Guid episodeId,
        string ownerId,
        EpisodePlanningResult planningResult,
        CancellationToken cancellationToken = default);
}
