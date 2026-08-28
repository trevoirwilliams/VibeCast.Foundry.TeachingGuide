#pragma warning disable MEAI001

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Domain.Media;
using VibeCast.Infrastructure.Data;
using VibeCast.Infrastructure.Options;

namespace VibeCast.Infrastructure.AI;

public class FoundryEpisodeArtworkGenerationService(
    IImageGenerator imageGenerator,
    IEpisodeService episodeService,
    IDbContextFactory<VibeCastDbContext> dbContextFactory,
    IBlobStorage blobStorage,
    MediaUploadValidator mediaValidator,
    IOptions<FoundryOptions> foundryOptions,
    ILogger<FoundryEpisodeArtworkGenerationService> logger)
 : IEpisodeArtworkGenerationService
{
    private const string ContentType = "image/png";

    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromSeconds(90);

    public async Task<MediaAssetSummary> GenerateAsync(Guid episodeId, string ownerId, CancellationToken cancellationToken = default)
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

        EpisodeDetails episode =
            await episodeService.GetAsync(
                episodeId,
                ownerId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "The episode could not be found or is not " +
                "available to the current user.");

        string prompt = EpisodeArtworkPrompt.Build(episode);

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
        timeout.CancelAfter(GenerationTimeout);

        ImageGenerationResponse response =
            await imageGenerator.GenerateImagesAsync(
                prompt,
                new ImageGenerationOptions
                {
                    Count = 1,
                    ImageSize = new Size(1536, 1024),
                    MediaType = ContentType,
                },
                timeout.Token);

        DataContent generatedImage =
            response.Contents
                .OfType<DataContent>()
                .SingleOrDefault()
            ?? throw new InvalidOperationException(
                "Microsoft Foundry did not return " +
                "generated image data.");

        if (!string.Equals(
                generatedImage.MediaType,
                ContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Microsoft Foundry returned an unsupported " +
                "image content type.");
        }

        byte[] imageBytes = generatedImage.Data.ToArray();
        DateTimeOffset generatedAtUtc = DateTimeOffset.UtcNow;

        string fileName = $"episode-{episode.Id:N}-promo-{generatedAtUtc:yyyyMMddHHmmss}.png";
        MediaUploadRequest validationRequest =
           new(
               FileName: fileName,
               ContentType: ContentType,
               SizeBytes: imageBytes.LongLength,
               EpisodeId: episode.Id);
        ValidationResult validation = mediaValidator.Validate(validationRequest);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "The generated artwork did not satisfy " +
                "VibeCast media requirements.");
        }

        await using MemoryStream imageStream = new(imageBytes, writable: false);

        bool signatureIsValid = await mediaValidator.HasExpectedSignatureAsync(
                imageStream,
                fileName,
                cancellationToken);

        if (!signatureIsValid)
        {
            throw new InvalidOperationException(
                "The generated artwork did not contain " +
                "a valid PNG signature.");
        }

        imageStream.Position = 0;

        StoredBlob? storedBlob = null;

        try
        {
            storedBlob = await blobStorage.SaveAsync(
                    imageStream,
                    fileName,
                    ContentType,
                    cancellationToken);

            MediaAsset asset = MediaAsset.Create(
                    episodeId: episode.Id,
                    ownerId: ownerId,
                    originalFileName:
                        storedBlob.OriginalFileName,
                    storageKey:
                        storedBlob.StorageKey,
                    contentType:
                        storedBlob.ContentType,
                    sizeBytes:
                        storedBlob.SizeBytes);

            asset.MarkValidated();

            asset.RecordGeneration(
                modelDeployment:
                    foundryOptions.Value.ImageModelDeployment,
                promptVersion:
                    EpisodeArtworkPrompt.Version,
                generatedAtUtc:
                    generatedAtUtc);

            await using VibeCastDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
                    cancellationToken);

            dbContext.MediaAssets.Add(asset);

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Generated promotional artwork {MediaAssetId} " +
                "for episode {EpisodeId}. ModelDeployment: " +
                "{ModelDeployment}; PromptVersion: " +
                "{PromptVersion}.",
                asset.Id,
                episode.Id,
                foundryOptions.Value.ImageModelDeployment,
                EpisodeArtworkPrompt.Version);

            return new MediaAssetSummary(
                Id: asset.Id,
                EpisodeId: asset.EpisodeId,
                OriginalFileName:
                    asset.OriginalFileName,
                ContentType:
                    asset.ContentType,
                SizeBytes:
                    asset.SizeBytes,
                Status:
                    asset.Status,
                CreatedAtUtc:
                    asset.CreatedAtUtc);
        }
        catch
        {
            if (storedBlob is not null)
            {
                await blobStorage.DeleteAsync(
                    storedBlob.StorageKey,
                    CancellationToken.None);
            }

            throw;
        }
    }
}

#pragma warning restore MEAI001
