using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Common;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Domain.Media;
using VibeCast.Infrastructure.Data;


namespace VibeCast.Infrastructure.Media;

public sealed class EfMediaAssetService(
    IDbContextFactory<VibeCastDbContext> dbContextFactory,
    IBlobStorage blobStorage,
    IKnowledgeSourceStorage knowledgeSourceStorage,
    MediaUploadValidator validator,
    ArtworkAnalysisValidator artworkValidator,
    ILogger<EfMediaAssetService> logger) : IMediaAssetService
{
    public async Task<MediaUploadResult> UploadAsync(
        MediaUploadRequest request,
        Stream content,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("An authenticated owner is required.", nameof(ownerId));
        }

        var validation = validator.Validate(request);
        if (!validation.IsValid)
        {
            return MediaUploadResult.Rejected(validation.Errors);
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (request.EpisodeId is Guid episodeId)
        {
            var ownsEpisode = await db.Episodes
                .AsNoTracking()
                .AnyAsync(e => e.Id == episodeId && e.OwnerId == ownerId, cancellationToken);

            if (!ownsEpisode)
            {
                return MediaUploadResult.Rejected(nameof(request.EpisodeId), "The selected episode was not found.");
            }
        }

        var displayName = MediaUploadValidator.GetDisplayName(request.FileName);
        var contentType = MediaUploadValidator.GetCanonicalContentType(displayName);
        StoredBlob? blob = null;

        try
        {
            blob = await blobStorage.SaveAsync(content, displayName, contentType, cancellationToken);

            await using var storedContent = await blobStorage.OpenReadAsync(blob.StorageKey, cancellationToken);
            var signatureOk = await validator.HasExpectedSignatureAsync(storedContent, displayName, cancellationToken);

            if (!signatureOk)
            {
                await blobStorage.DeleteAsync(blob.StorageKey, CancellationToken.None);
                return MediaUploadResult.Rejected(nameof(request.FileName), "The file contents do not match its extension.");
            }

            var asset = MediaAsset.Create(
                episodeId: request.EpisodeId,
                ownerId: ownerId,
                originalFileName: blob.OriginalFileName,
                storageKey: blob.StorageKey,
                contentType: contentType,
                sizeBytes: blob.SizeBytes);

            asset.MarkValidated();
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Validated media asset {MediaAssetId} for episode {EpisodeId}.",
                asset.Id,
                asset.EpisodeId);

            return MediaUploadResult.Accepted(ToSummary(asset));
        }
        catch
        {
            if (blob is not null)
            {
                await blobStorage.DeleteAsync(blob.StorageKey, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<MediaAssetSummary>> ListAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("An authenticated owner is required.", nameof(ownerId));
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var assets = await db.MediaAssets
            .AsNoTracking()
            .Where(a => a.OwnerId == ownerId)
            .Select(a => new MediaAssetSummary(
                a.Id,
                a.EpisodeId,
                a.OriginalFileName,
                a.ContentType,
                a.SizeBytes,
                a.Status,
                a.IsKnowledgeSource,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return assets
           .OrderByDescending(a => a.CreatedAtUtc)
           .ToList();
    }

    public async Task<IReadOnlyList<MediaAssetSummary>> ListSharedSourcesAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using var db =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<MediaAssetSummary> sources = await db.MediaAssets
            .AsNoTracking()
            .Where(asset =>
                asset.OwnerId == ownerId &&
                asset.EpisodeId == null &&
                (asset.ContentType == "application/pdf" ||
                 asset.ContentType == "text/plain"))
            .Select(asset => new MediaAssetSummary(
                asset.Id,
                asset.EpisodeId,
                asset.OriginalFileName,
                asset.ContentType,
                asset.SizeBytes,
                asset.Status,
                asset.IsKnowledgeSource,
                asset.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return sources
            .OrderByDescending(source => source.CreatedAtUtc)
            .ToList();
    }

    public async Task<ArtworkWorkspace?> GetArtworkAsync(Guid mediaAssetId, string ownerId, CancellationToken cancellationToken = default)
    {
        await using VibeCastDbContext db = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);

        MediaAsset? asset =
            await db.MediaAssets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == mediaAssetId &&
                        candidate.OwnerId == ownerId,
                    cancellationToken);

        if (asset is null || !MediaAssetHelpers.IsSupportedArtwork(asset.ContentType))
        {
            return null;
        }

        return await CreateArtworkWorkspaceAsync(
            db,
            asset,
            ownerId,
            cancellationToken);
    }

    public async Task<ArtworkContent?> OpenArtworkAsync(Guid mediaAssetId, string ownerId, CancellationToken cancellationToken = default)
    {
        await using VibeCastDbContext db = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);

        var asset =
            await db.MediaAssets
                .AsNoTracking()
                .Where(candidate =>
                    candidate.Id == mediaAssetId &&
                    candidate.OwnerId == ownerId)
                .Select(candidate => new
                {
                    candidate.StorageKey,
                    candidate.OriginalFileName,
                    candidate.ContentType,
                    candidate.Status
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (asset is null || !MediaAssetHelpers.IsSupportedArtwork(asset.ContentType))
        {
            return null;
        }

        if (asset.Status != MediaAssetStatus.Validated &&
            asset.Status != MediaAssetStatus.Ready)
        {
            throw new SafeApplicationException(
                "Only validated artwork can be opened.");
        }

        Stream content = await blobStorage.OpenReadAsync(
                asset.StorageKey,
                cancellationToken);

        return new ArtworkContent(
            Content: content,
            ContentType: asset.ContentType,
            OriginalFileName: asset.OriginalFileName);
    }

    public async Task<ArtworkWorkspace> SaveArtworkAnalysisAsync(Guid mediaAssetId, ArtworkAnalysisResult result, string ownerId, CancellationToken cancellationToken = default)
    {
        await using VibeCastDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        MediaAsset asset = await GetOwnedAssetAsync(
                db,
                mediaAssetId,
                ownerId,
                cancellationToken);

        asset.SaveArtworkProposal(
            altText:result.Analysis.AltText,
            visualSummary: result.Analysis.VisualSummary,
            visibleText: result.Analysis.VisibleText,
            promptVersion: result.PromptVersion);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Saved an artwork-analysis proposal for " +
            "media asset {MediaAssetId}. PromptVersion: " +
            "{PromptVersion}.",
            mediaAssetId,
            result.PromptVersion);

        return await CreateArtworkWorkspaceAsync(
            db,
            asset,
            ownerId,
            cancellationToken);
    }

    public async Task<ArtworkWorkspace> AcceptArtworkDescriptionAsync(Guid mediaAssetId, string acceptedAltText, string ownerId, CancellationToken cancellationToken = default)
    {
        ValidationResult validation = artworkValidator.ValidateAltText(acceptedAltText);

        if (!validation.IsValid)
        {
            throw new SafeApplicationException(
                string.Join(
                    " ",
                    validation.Errors.Select(
                        error => error.ErrorMessage)));
        }

        await using VibeCastDbContext db = await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        MediaAsset asset =
            await GetOwnedAssetAsync(
                db,
                mediaAssetId,
                ownerId,
                cancellationToken);

        asset.AcceptArtworkDescription(acceptedAltText);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Accepted the artwork description for " +
            "media asset {MediaAssetId}.",
            mediaAssetId);

        return await CreateArtworkWorkspaceAsync(
            db,
            asset,
            ownerId,
            cancellationToken);
    }

    private static async Task<MediaAsset> GetOwnedAssetAsync(
        VibeCastDbContext db,
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken)
    {
        MediaAsset? asset =
            await db.MediaAssets
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == mediaAssetId &&
                        candidate.OwnerId == ownerId,
                    cancellationToken);

        return asset ??
            throw new SafeApplicationException(
                "The selected media asset was not found.");
    }

    private static async Task<ArtworkWorkspace>CreateArtworkWorkspaceAsync(VibeCastDbContext db, MediaAsset asset, string ownerId, CancellationToken cancellationToken)
    {
        string episodeTitle = "Unassigned episode artwork";

        if (asset.EpisodeId is Guid episodeId)
        {
            episodeTitle =
                await db.Episodes
                    .AsNoTracking()
                    .Where(episode =>
                        episode.Id == episodeId &&
                        episode.OwnerId == ownerId)
                    .Select(episode => episode.Title)
                    .SingleOrDefaultAsync(
                        cancellationToken)
                ?? "Untitled episode";
        }

        return new ArtworkWorkspace(
            MediaAssetId: asset.Id,
            EpisodeId: asset.EpisodeId,
            EpisodeTitle: episodeTitle,
            OriginalFileName:
                asset.OriginalFileName,
            ContentType:
                asset.ContentType,
            SizeBytes:
                asset.SizeBytes,
            MediaStatus:
                asset.Status,
            ProposedAltText:
                asset.ProposedAltText,
            AcceptedAltText:
                asset.AcceptedAltText,
            VisualSummary:
                asset.ArtworkSummary,
            VisibleText:
                asset.ArtworkVisibleText,
            PromptVersion:
                asset.ArtworkPromptVersion,
            AnalyzedAtUtc:
                asset.ArtworkAnalyzedAtUtc,
            AcceptedAtUtc:
                asset.ArtworkAcceptedAtUtc);
    }

    private static MediaAssetSummary ToSummary(MediaAsset asset) =>
        new(
            asset.Id,
            asset.EpisodeId,
            asset.OriginalFileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.Status,
            asset.IsKnowledgeSource,
            asset.CreatedAtUtc);

    public async Task<MediaContent?> OpenMediaAsync(
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using VibeCastDbContext db =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var asset =
            await db.MediaAssets
                .AsNoTracking()
                .Where(candidate =>
                    candidate.Id == mediaAssetId &&
                    candidate.OwnerId == ownerId)
                .Select(candidate => new
                {
                    candidate.StorageKey,
                    candidate.OriginalFileName,
                    candidate.ContentType,
                    candidate.Status
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (asset is null)
        {
            return null;
        }

        if (asset.Status is not
            (MediaAssetStatus.Validated or MediaAssetStatus.Ready))
        {
            throw new SafeApplicationException(
                "Only validated media can be opened.");
        }

        Stream content = await blobStorage.OpenReadAsync(
                asset.StorageKey,
                cancellationToken);

        return new MediaContent(
            Content: content,
            ContentType: asset.ContentType,
            OriginalFileName: asset.OriginalFileName);
    }

    public async Task<IReadOnlyList<MediaAssetSummary>> ListKnowledgeSourcesAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using VibeCastDbContext db =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<MediaAssetSummary> sources =
            await db.MediaAssets
                .AsNoTracking()
                .Where(asset =>
                    asset.OwnerId == ownerId &&
                    asset.IsKnowledgeSource)
                .Select(asset => new MediaAssetSummary(
                    asset.Id,
                    asset.EpisodeId,
                    asset.OriginalFileName,
                    asset.ContentType,
                    asset.SizeBytes,
                    asset.Status,
                    asset.IsKnowledgeSource,
                    asset.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        return sources
            .OrderByDescending(source => source.CreatedAtUtc)
            .ToList();
    }

    public async Task SetKnowledgeSourceAsync(
        Guid mediaAssetId,
        bool isKnowledgeSource,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "A media asset identifier is required.",
                nameof(mediaAssetId));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using VibeCastDbContext db =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        MediaAsset? asset = await db.MediaAssets.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == mediaAssetId &&
                candidate.OwnerId == ownerId,
            cancellationToken);

        if (asset is null)
        {
            throw new SafeApplicationException(
                "The selected media asset was not found.");
        }

        if (!MediaAssetHelpers.IsSupportedDocumentType(asset.ContentType))
        {
            throw new SafeApplicationException(
                "Only PDF and TXT documents can currently be used as knowledge sources.");
        }

        if (isKnowledgeSource)
        {
            await PromoteKnowledgeSourceAsync(
                asset,
                db,
                cancellationToken);

            return;
        }

        await WithdrawKnowledgeSourceAsync(
            asset,
            db,
            cancellationToken);
    }

    private async Task PromoteKnowledgeSourceAsync(MediaAsset asset, VibeCastDbContext db, CancellationToken cancellationToken)
    {
        bool wasAlreadyKnowledgeSource = asset.IsKnowledgeSource;

        Stream sourceContent;

        try
        {
            sourceContent = await blobStorage.OpenReadAsync(
                    asset.StorageKey,
                    cancellationToken);
        }
        catch (Exception exception)
            when (exception is IOException or
                  UnauthorizedAccessException)
        {
            throw new SafeApplicationException(
                "The original source file could not be opened.",
                exception);
        }

        await using (sourceContent)
        {
            StoredKnowledgeSource stored =
                await knowledgeSourceStorage.SaveAsync(
                    mediaAssetId: asset.Id,
                    ownerId: asset.OwnerId,
                    originalFileName:
                        asset.OriginalFileName,
                    contentType:
                        asset.ContentType,
                    content:
                        sourceContent,
                    cancellationToken:
                        cancellationToken);

            try
            {
                asset.SetKnowledgeSource(true);

                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                if (!wasAlreadyKnowledgeSource)
                {
                    try
                    {
                        await knowledgeSourceStorage.DeleteAsync(
                            asset.Id,
                            asset.OwnerId,
                            asset.OriginalFileName,
                            CancellationToken.None);
                    }
                    catch (Exception cleanupException)
                    {
                        logger.LogError(
                            cleanupException,
                            "Failed to compensate knowledge-source Blob creation for media asset {MediaAssetId}.",
                            asset.Id);
                    }
                }

                throw;
            }

            logger.LogInformation(
                "Promoted media asset {MediaAssetId} to knowledge storage at {StorageKey}.",
                asset.Id,
                stored.StorageKey);
        }

    }

    private async Task WithdrawKnowledgeSourceAsync(
        MediaAsset asset,
        VibeCastDbContext db,
        CancellationToken cancellationToken)
    {
        await knowledgeSourceStorage.DeleteAsync(
            asset.Id,
            asset.OwnerId,
            asset.OriginalFileName,
            cancellationToken);

        asset.SetKnowledgeSource(false);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Withdrew media asset {MediaAssetId} from the knowledge store.",
            asset.Id);
    }

    public async Task<IReadOnlyList<MediaAssetSummary>> GetKnowledgeSourcesAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediaAssetIds);

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("An authenticated owner is required.", nameof(ownerId));
        }

        Guid[] requestedIds = mediaAssetIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

        if (requestedIds.Length == 0)
        {
            throw new SafeApplicationException("Select at least one knowledge source.");
        }

        await using VibeCastDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<MediaAssetSummary> sources = await db.MediaAssets
                .AsNoTracking()
                .Where(asset =>
                    asset.OwnerId == ownerId &&
                    asset.IsKnowledgeSource &&
                    requestedIds.Contains(asset.Id))
                .Select(asset =>
                    new MediaAssetSummary(
                        asset.Id,
                        asset.EpisodeId,
                        asset.OriginalFileName,
                        asset.ContentType,
                        asset.SizeBytes,
                        asset.Status,
                        asset.IsKnowledgeSource,
                        asset.CreatedAtUtc))
                .ToListAsync(cancellationToken);

        if (sources.Count != requestedIds.Length)
        {
            throw new SafeApplicationException("One or more selected knowledge sources are unavailable.");
        }

        return sources;
    }
}
