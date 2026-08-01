using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Media;
using VibeCast.Domain.Media;
using VibeCast.Infrastructure.Data;


namespace VibeCast.Infrastructure.Media;

public sealed class EfMediaAssetService(
    IDbContextFactory<VibeCastDbContext> dbContextFactory,
    IBlobStorage blobStorage,
    MediaUploadValidator validator,
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
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return assets
           .OrderByDescending(a => a.CreatedAtUtc)
           .ToList();
    }

    private static MediaAssetSummary ToSummary(MediaAsset asset) =>
        new(
            asset.Id,
            asset.EpisodeId,
            asset.OriginalFileName,
            asset.ContentType,
            asset.SizeBytes,
            asset.Status,
            asset.CreatedAtUtc);
}
