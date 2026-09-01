using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;
using VibeCast.Domain.Episodes;
using VibeCast.Infrastructure.Data;

namespace VibeCast.Infrastructure.Episodes;

public class EfEpisodeService(IDbContextFactory<VibeCastDbContext> dbContextFactory) : IEpisodeService
{
    private static readonly JsonSerializerOptions JsonOptions =
    new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<EpisodeSummary>> ListAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using VibeCastDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<EpisodeSummary> episodes = await dbContext.Episodes
            .AsNoTracking()
            .Where(episode => episode.OwnerId == ownerId)
            .Select(episode => new EpisodeSummary(
                episode.Id,
                episode.Title,
                episode.Description,
                episode.TargetAudience,
                episode.Status,
                episode.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return episodes
            .OrderByDescending(episode => episode.UpdatedAtUtc)
            .ToList();
    }
    
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

        Episode? episode =
        await dbContext.Episodes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == episodeId &&
                    item.OwnerId == ownerId,
                cancellationToken);

        if (episode is null)
        {
            return null;
        }

        EpisodePlanningResult? acceptedPlan = CreateAcceptedPlan(episode);

        List<MediaAssetSummary> mediaAssets = await dbContext.MediaAssets
            .AsNoTracking()
            .Where(asset =>
                asset.OwnerId == ownerId &&
                asset.EpisodeId == episodeId)
            .Select(asset => new MediaAssetSummary(
                asset.Id,
                asset.EpisodeId,
                asset.OriginalFileName,
                asset.ContentType,
                asset.SizeBytes,
                asset.Status,
                asset.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        IReadOnlyList<MediaAssetSummary> orderedMediaAssets = mediaAssets
            .OrderByDescending(asset => asset.CreatedAtUtc)
            .ToList();

        List<EpisodeTranscript>? transcripts =
            await dbContext.MediaAssets
                .AsNoTracking()
                .Where(asset =>
                    asset.OwnerId == ownerId &&
                    asset.EpisodeId == episodeId &&
                    asset.TranscribedAtUtc != null &&
                    asset.TranscriptText != null)
                .Select(asset =>
                    new EpisodeTranscript(
                        asset.Id,
                        episodeId,
                        asset.OriginalFileName,
                        asset.TranscriptText!,
                        asset.TranscriptionLocale!,
                        asset.TranscribedAtUtc!.Value))
                .ToListAsync(cancellationToken);

        var transcript = transcripts
            .OrderByDescending(q => q.TranscribedAtUtc)
            .FirstOrDefault();

        var supportingSourceEntities = await dbContext.EpisodeSupportingSources
            .AsNoTracking()
            .Include(s => s.MediaAsset)
            .Where(s =>
                s.OwnerId == ownerId &&
                s.EpisodeId == episodeId)
            .Select(s => new
            {
                s.Id,
                s.MediaAssetId,
                MediaAssetOriginalFileName = s.MediaAsset != null ? s.MediaAsset.OriginalFileName : string.Empty,
                MediaAssetContentType = s.MediaAsset != null ? s.MediaAsset.ContentType : string.Empty,
                s.Summary,
                s.RelevanceRationale,
                s.RelevantPointsJson,
                s.MatchedEvidenceRequirementsJson,
                s.AnalyzerId,
                s.RelevancePromptVersion,
                s.AnalyzedAtUtc
            })
            .ToListAsync(cancellationToken);

        List<EpisodeSupportingSourceSummary> supportingSources = supportingSourceEntities
            .Select(s => new EpisodeSupportingSourceSummary(
                s.Id,
                s.MediaAssetId,
                s.MediaAssetOriginalFileName,
                s.MediaAssetContentType,
                s.Summary,
                s.RelevanceRationale,
                JsonSerializer.Deserialize<string[]>(s.RelevantPointsJson ?? string.Empty, JsonOptions) ?? Array.Empty<string>(),
                JsonSerializer.Deserialize<string[]>(s.MatchedEvidenceRequirementsJson ?? string.Empty, JsonOptions) ?? Array.Empty<string>(),
                s.AnalyzerId,
                s.RelevancePromptVersion,
                s.AnalyzedAtUtc))
            .ToList();

        return new EpisodeDetails(
                    Id: episode.Id,
                    Title: episode.Title,
                    Description: episode.Description,
                    TargetAudience: episode.TargetAudience,
                    Objective: episode.Objective,
                    Tone: episode.Tone,
                    Language: episode.Language,
                    PlannedPublishDate: episode.PlannedPublishDate,
                    Status: episode.Status,
                    CreatedAtUtc: episode.CreatedAtUtc,
                    UpdatedAtUtc: episode.UpdatedAtUtc,
                    AcceptedPlan: acceptedPlan,
                    Transcript: transcript,
                    SupportingSources: supportingSources,
                    MediaAssets: orderedMediaAssets);
    }

    private EpisodePlanningResult? CreateAcceptedPlan(Episode episode)
    {
        if (string.IsNullOrWhiteSpace(episode.AcceptedPlanJson))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(episode.PlanPromptVersion) ||
            episode.PlanGeneratedAtUtc is null)
        {
            throw new InvalidOperationException(
                "The saved episode plan metadata is incomplete.");
        }

        EpisodePlan? plan = JsonSerializer.Deserialize<EpisodePlan>(episode.AcceptedPlanJson, JsonOptions);
        if (plan is null)
        {
            throw new InvalidOperationException(
                "The saved episode plan could not be read.");
        }

        return new EpisodePlanningResult(
            Plan: plan,
            PromptVersion:
                episode.PlanPromptVersion,
            GeneratedAtUtc:
                episode.PlanGeneratedAtUtc.Value,
            RepairAttempted:
                episode.PlanRepairAttempted,
            RepairPromptVersion:
                episode.PlanRepairPromptVersion,
            FormatPolicyVersion:
                episode.PlanFormatPolicyVersion);
    }

    public async Task SavePlanAsync(
    Guid episodeId,
    string ownerId,
    EpisodePlanningResult planningResult,
    CancellationToken cancellationToken = default)
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

        ArgumentNullException.ThrowIfNull(planningResult);

        await using VibeCastDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        Episode episode =
            await dbContext.Episodes
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == episodeId &&
                        item.OwnerId == ownerId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "The episode could not be found or is not available " +
                "to the current user.");

        string planJson =
            JsonSerializer.Serialize(
                planningResult.Plan,
                JsonOptions);

        episode.SaveAcceptedPlan(
            planJson,
            planningResult.PromptVersion,
            planningResult.GeneratedAtUtc,
            planningResult.RepairAttempted,
            planningResult.RepairPromptVersion,
            planningResult.FormatPolicyVersion);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
