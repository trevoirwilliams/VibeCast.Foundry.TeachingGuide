using System;
using System.Collections.Generic;
using System.Text;

namespace VibeCast.Application.Media;

public interface IEpisodeArtworkGenerationService
{
    Task<MediaAssetSummary> GenerateAsync(
        Guid episodeId,
        string ownerId,
        CancellationToken cancellationToken = default);
}
