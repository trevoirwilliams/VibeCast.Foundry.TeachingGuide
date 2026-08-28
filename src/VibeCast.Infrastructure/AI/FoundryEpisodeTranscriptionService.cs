using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using Azure.AI.Speech.Transcription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Media;
using VibeCast.Domain.Media;
using VibeCast.Infrastructure.Data;

namespace VibeCast.Infrastructure.AI;

public class FoundryEpisodeTranscriptionService(
    TranscriptionClient transcriptionClient,
    IDbContextFactory<VibeCastDbContext> dbContextFactory,
    IBlobStorage blobStorage,
    ILogger<FoundryEpisodeTranscriptionService> logger) : IEpisodeTranscriptionService
{
    public async Task<EpisodeTranscript> TranscribeAsync(Guid mediaAssetId, string ownerId, CancellationToken cancellationToken = default)
    {
        if (mediaAssetId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid media asset identifier is required.",
                nameof(mediaAssetId));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "An authenticated owner is required.",
                nameof(ownerId));
        }

        await using VibeCastDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        MediaAsset asset =
            await dbContext.MediaAssets
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == mediaAssetId &&
                        candidate.OwnerId == ownerId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "The selected recording was not found.");

        if (asset.EpisodeId is not Guid episodeId)
        {
            throw new InvalidOperationException(
                "The recording must be attached to an episode.");
        }

        if (!MediaAssetHelpers.IsAudio(asset.ContentType))
        {
            throw new InvalidOperationException(
                "Only audio assets can be transcribed.");
        }

        string language =
            await dbContext.Episodes
                .Where(candidate =>
                    candidate.Id == episodeId &&
                    candidate.OwnerId == ownerId)
                .Select(candidate => candidate.Language)
                .SingleAsync(cancellationToken);

        string locale = EpisodeSpeechLocale.Resolve(language);

        await using Stream audio = await blobStorage.OpenReadAsync(
                asset.StorageKey,
                cancellationToken);
        TranscriptionOptions options = new(audio);
        options.Locales.Add(locale);

        ClientResult<TranscriptionResult> response = 
            await transcriptionClient.TranscribeAsync(
                options,
                cancellationToken);

        string transcriptText =
            string.Join(
                Environment.NewLine,
                response.Value.CombinedPhrases
                    .Select(phrase => phrase.Text)
                    .Where(text =>
                        !string.IsNullOrWhiteSpace(text)));

        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            throw new InvalidOperationException(
                "Speech transcription returned no usable text.");
        }

        DateTimeOffset transcribedAtUtc = DateTimeOffset.UtcNow;

        asset.SaveTranscript(transcriptText, locale, transcribedAtUtc);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Transcribed media asset {MediaAssetId} " +
            "for episode {EpisodeId}. Locale: {Locale}.",
            asset.Id,
            episodeId,
            locale);

        return new EpisodeTranscript(
            MediaAssetId: asset.Id,
            EpisodeId: episodeId,
            SourceFileName: asset.OriginalFileName,
            Text: transcriptText,
            Locale: locale,
            TranscribedAtUtc: transcribedAtUtc);
    }
}
