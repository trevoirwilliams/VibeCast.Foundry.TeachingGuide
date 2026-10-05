using System;
using System.Collections.Generic;
using System.Text;

namespace VibeCast.Application.Media;

public interface IEpisodeTranscriptionService
{
    Task<EpisodeTranscript> TranscribeAsync(
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken = default);
}

public sealed record EpisodeTranscript(
    Guid MediaAssetId,
    Guid EpisodeId,
    string SourceFileName,
    string Text,
    string Locale,
    DateTimeOffset TranscribedAtUtc);
