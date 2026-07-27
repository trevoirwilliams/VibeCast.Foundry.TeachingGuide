using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VibeCast.Application.Episodes;
using VibeCast.Domain.Episodes;
using VibeCast.Infrastructure.Data;

namespace VibeCast.Infrastructure.Episodes;

public class EfEpisodeService(IDbContextFactory<VibeCastDbContext> dbContextFactory) : IEpisodeService
{
    public async Task<Guid> CreateAsync(CreateEpisodeRequest request, string ownerId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        Episode episode = Episode.Create(
            title: request.Title,
            description: request.Description,
            targetAudience: request.TargetAudience,
            objective: request.Objective,
            tone: request.Tone,
            language: request.Language,
            plannedPublishDate: request.PlannedPublishDate,
            ownerId: ownerId);

        await using VibeCastDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        dbContext.Episodes.Add(episode);
        await dbContext.SaveChangesAsync(cancellationToken);
        return episode.Id;
    }

    public async Task<EpisodeDetails?> GetAsync(Guid episodeId, string ownerId, CancellationToken cancellationToken = default)
    {
        if (episodeId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid episode identifier is required.",
                nameof(episodeId));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using VibeCastDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await dbContext.Episodes
            .AsNoTracking()
            .Where(episode =>
                episode.Id == episodeId &&
                episode.OwnerId == ownerId)
            .Select(episode => new EpisodeDetails(
                episode.Id,
                episode.Title,
                episode.Description,
                episode.TargetAudience,
                episode.Objective,
                episode.Tone,
                episode.Language,
                episode.PlannedPublishDate,
                episode.Status,
                episode.CreatedAtUtc,
                episode.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
