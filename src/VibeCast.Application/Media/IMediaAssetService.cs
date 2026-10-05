using VibeCast.Domain.Media;

namespace VibeCast.Application.Media;

public interface IMediaAssetService
{
    Task<IReadOnlyList<MediaAssetSummary>> ListAsync( string ownerId,
        CancellationToken cancellationToken = default);
    Task<MediaUploadResult> UploadAsync(MediaUploadRequest request, Stream content, string ownerId, CancellationToken cancellationToken = default);

    Task<ArtworkWorkspace?> GetArtworkAsync(
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<ArtworkContent?> OpenArtworkAsync(
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<ArtworkWorkspace> SaveArtworkAnalysisAsync(
        Guid mediaAssetId,
        ArtworkAnalysisResult result,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<ArtworkWorkspace> AcceptArtworkDescriptionAsync(
        Guid mediaAssetId,
        string acceptedAltText,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<MediaContent?> OpenMediaAsync(
        Guid mediaAssetId,
        string ownerId,
        CancellationToken cancellationToken = default);
}
